using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using M1ToolPlus.Properties;

namespace M1ToolPlus
{
    /// <summary>
    /// 检查更新对话框（v1.8.0 重做）：
    /// 打开后自动查询本仓库的最新 Release（Settings.GitHubR）——
    /// 旧版按钮只是打开原作者的 Releases 页面。发现新版本后**直接开始下载**
    /// 并显示进度条；下载完成后由用户选择「继续使用」或「重启更新」。
    /// 「重启更新」生成临时 .bat（等待本进程退出 → 解压覆盖 → 自动重启主程序），
    /// 启动脚本后返回 DialogResult.OK，主窗体随即退出进程，脚本完成余下工作。
    /// UI 全部代码构建（无 Designer/resx），文案取自三语资源更新_*。
    /// </summary>
    internal sealed class FormUpdate : Form
    {
        private Label _info;      // 顶部主提示：正在检查更新… / 发现新版本…
        private ProgressBar _bar; // 下载进度
        private Label _detail;    // 进度明细 / 失败原因
        private Label _hint;      // 「重启更新」行为说明
        private LinkLabel _lnk;   // 打开发布页（失败态逃逸门）
        private Button _btnRetry;
        private Button _btnLater;   // 继续使用（取消）
        private Button _btnRestart; // 重启更新

        private WebClient _wc;        // 下载中对象（用于取消/释放）
        private ReleaseInfo _rel;     // 最近一次查到的 Release 元数据
        private string _zipPath;      // 更新包缓存完整路径
        private bool _busyDownload;   // 是否正在下载（防重复/供取消判断）
        private Timer _autoClose;     // 「已是最新」时自动关闭

        // ---- 版面常量：动作按钮统一右对齐、等间距，随可见数量重排 ----
        private const int PadL = 14;            // 左右留白
        private const int ContentWidth = 448;   // 476 - 14*2
        private const int ButtonHeight = 28;
        private const int ButtonGap = 8;        // 按钮间距（原先是 4px，两个按钮几乎贴在一起）
        private const int ActionTop = 176;      // 动作按钮行的顶边

        public FormUpdate()
        {
            SuspendLayout();
            Font = UiTheme.UiFont;

            Text = Resources.更新_标题;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            ClientSize = new Size(476, 224);

            _info = new Label();
            // 固定宽度 + 省略号：提示里带版本号/tag，AutoSize 会把窗口顶出右缘后被硬裁
            _info.AutoSize = false;
            _info.AutoEllipsis = true;
            _info.Location = new Point(PadL, 13);
            _info.Size = new Size(ContentWidth, 20);
            _info.Text = Resources.更新_正在检查;
            Controls.Add(_info);

            _bar = new ProgressBar();
            _bar.Location = new Point(PadL, 44);
            _bar.Size = new Size(ContentWidth, 22);
            Controls.Add(_bar);

            _detail = new Label();
            // 失败原因（异常消息）长度不可控，同样用固定宽度 + 省略号而不是硬裁
            _detail.AutoSize = false;
            _detail.AutoEllipsis = true;
            _detail.Location = new Point(PadL, 76);
            _detail.Size = new Size(ContentWidth, 20);
            _detail.Text = "";
            Controls.Add(_detail);

            _hint = new Label();
            _hint.AutoSize = false;
            _hint.Size = new Size(ContentWidth, 34);
            _hint.Location = new Point(PadL, 100);
            _hint.Text = Resources.更新_重启说明;
            _hint.Visible = false;
            Controls.Add(_hint);

            _lnk = new LinkLabel();
            _lnk.AutoSize = true;
            _lnk.Location = new Point(PadL, 146);
            _lnk.Text = Resources.更新_打开发布页;
            _lnk.Hide();
            _lnk.LinkClicked += new LinkLabelLinkClickedEventHandler(OnOpenPage);
            Controls.Add(_lnk);

            _btnRetry = new Button();
            _btnRetry.Size = new Size(96, ButtonHeight);
            _btnRetry.Text = Resources.更新_重试;
            _btnRetry.Hide();
            _btnRetry.Click += new EventHandler(OnRetry);
            Controls.Add(_btnRetry);

            _btnLater = new Button();
            _btnLater.Size = new Size(110, ButtonHeight);
            _btnLater.Text = Resources.更新_继续使用;
            _btnLater.Enabled = false;
            _btnLater.Click += new EventHandler(OnLater);
            Controls.Add(_btnLater);

            _btnRestart = new Button();
            _btnRestart.Size = new Size(110, ButtonHeight);
            _btnRestart.Text = Resources.更新_重启更新;
            _btnRestart.Enabled = false;
            _btnRestart.Click += new EventHandler(OnRestart);
            Controls.Add(_btnRestart);

            // 初值按「继续使用 + 重启更新 都可见」算好（此刻窗体还没显示，
            // 子控件的 Visible 恒为 false，LayoutActions 拿不到真实可见性）
            _btnRestart.Left = ClientSize.Width - PadL - _btnRestart.Width;
            _btnLater.Left = _btnRestart.Left - ButtonGap - _btnLater.Width;
            _btnRetry.Left = _btnLater.Left - ButtonGap - _btnRetry.Width;
            foreach (Button b in new Button[] { _btnRetry, _btnLater, _btnRestart })
            {
                b.Top = ActionTop;
            }

            _autoClose = new Timer();
            _autoClose.Interval = 1600;
            _autoClose.Tick += new EventHandler(OnAutoClose);

            CancelButton = _btnLater;
            ResumeLayout(false);
            PerformLayout();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UiTheme.Apply(this);
            StartCheck();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // 窗体现在才真正可见，子控件的 Visible 从这一刻起才反映真实显隐
            LayoutActions();
        }

        /// <summary>
        /// 把当前可见的动作按钮排成「右对齐 + 等间距」的一行。
        /// 任一按钮显隐变化后都要调用——原先的固定坐标会让「重试」孤零零贴在左边，
        /// 只剩一个按钮时右边又会空出一大块，看起来像布局坏了。
        /// </summary>
        private void LayoutActions()
        {
            int right = ClientSize.Width - PadL;
            foreach (Button b in new Button[] { _btnRestart, _btnLater, _btnRetry })
            {
                if (!b.Visible) { continue; }
                b.Left = right - b.Width;
                b.Top = ActionTop;
                right = b.Left - ButtonGap;
            }
        }

        /// <summary>显隐动作按钮并立即重排（窗体尚未显示时重排是空操作，保留构造期初值）。</summary>
        private void ShowAction(Button b, bool on)
        {
            if (on) { b.Show(); } else { b.Hide(); }
            LayoutActions();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 下载中被直接关闭：取消下载、清理半截包
            if (_busyDownload && _wc != null)
            {
                try { _wc.CancelAsync(); }
                catch (Exception) { }
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_autoClose != null)
            {
                _autoClose.Stop();
                _autoClose.Dispose();
                _autoClose = null;
            }
            DisposeWorker();
            base.OnFormClosed(e);
        }

        private static string LocalTag()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            return "v" + v.ToString(3);
        }

        private static string PageUrl()
        {
            return "https://github.com/" + Settings.Default.GitHubR + "/releases/latest";
        }

        // ---------- 检查 ----------

        private void StartCheck()
        {
            _btnRestart.Enabled = false;
            _btnLater.Enabled = false;
            ShowAction(_btnRetry, false);
            _informationSet(Resources.更新_正在检查);
            _detail.Text = "";
            _bar.Value = 0;
            _hint.Visible = false;

            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(delegate(object s, DoWorkEventArgs e)
            {
                string err;
                ReleaseInfo rel = GitHubUpdate.FetchLatest(out err);
                e.Result = new object[] { rel, err };
            });
            bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnCheckDone);
            bgw.RunWorkerAsync();
        }

        private void OnCheckDone(object sender, RunWorkerCompletedEventArgs e)
        {
            if (IsDisposed || Disposing) { return; }
            if (e.Error != null)
            {
                Failed(e.Error.GetBaseException().Message);
                return;
            }
            object[] r = (object[])e.Result;
            ReleaseInfo rel = (ReleaseInfo)r[0];
            string err = (string)r[1];
            if (rel == null)
            {
                if (err == "404")
                {
                    // 仓库还没有任何已发布版本（首次发布前点「检查更新」的正常状态）
                    _info.Text = Resources.更新_暂无发布;
                    _btnRestart.Hide();
                    _btnLater.Text = Resources.更新_关闭;
                    _btnLater.Enabled = true;
                    _lnk.Show();
                    return;
                }
                Failed(err);
                return;
            }
            _rel = rel;
            Version rv = rel.Remote;
            Version lv = Assembly.GetExecutingAssembly().GetName().Version;
            if (rel.AssetUrl == null || rel.AssetName == null)
            {
                _info.Text = Resources.更新_无更新包;
                ShowAction(_btnRestart, false);
                _btnLater.Text = Resources.更新_关闭;
                _btnLater.Enabled = true;
                _lnk.Show();
                return;
            }
            if (rv == null || rv <= lv)
            {
                UpToDate();
                return;
            }
            _info.Text = string.Format(Resources.更新_发现新版本, rel.Tag, LocalTag());
            _hint.Visible = true;
            StartDownload();
        }

        private void UpToDate()
        {
            _info.Text = string.Format(Resources.更新_已是最新, LocalTag());
            ShowAction(_btnRestart, false);
            _btnLater.Text = Resources.更新_关闭;
            _btnLater.Enabled = true;
            _autoClose.Start();
        }

        // ---------- 下载 ----------

        private void StartDownload()
        {
            ShowAction(_btnRetry, false);
            _bar.Value = 0;
            string dir = Path.Combine(Path.GetTempPath(), "M1ToolPlusUpdate");
            try { Directory.CreateDirectory(dir); }
            catch (Exception) { }
            _zipPath = Path.Combine(dir, "M1ToolPlus-" + _rel.Tag + ".zip");

            // 已下载过（大小一致且 SHA256 校验通过）则直接进入选择阶段，避免重复下载；
            // 大小对但摘要不对（截断/损坏/被篡改）→ 删掉重新下载。
            FileInfo cached = null;
            try { cached = new FileInfo(_zipPath); }
            catch (Exception) { }
            if (cached != null && cached.Exists && cached.Length == _rel.AssetSize)
            {
                string cachedHash;
                if (GitHubUpdate.VerifyPackage(_zipPath, _rel.AssetDigest, out cachedHash))
                {
                    Ready();
                    return;
                }
                try { File.Delete(_zipPath); }
                catch (Exception) { }
            }

            string workerErr = null;
            try
            {
                _busyDownload = true;
                _wc = new WebClient();
                _wc.Headers[HttpRequestHeader.UserAgent] =
                    "M1ToolPlus/" + Assembly.GetExecutingAssembly().GetName().Version;
                GitHubUpdate.EnableTls12();
                _wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(OnProgress);
                _wc.DownloadFileCompleted += new AsyncCompletedEventHandler(OnDownloadDone);
                _detail.Text = string.Format(Resources.更新_下载进度, _rel.AssetName, 0, "0.0 MB");
                _wc.DownloadFileAsync(new Uri(_rel.AssetUrl), _zipPath);
            }
            catch (Exception ex)
            {
                workerErr = ex.Message;
            }
            if (workerErr != null)
            {
                _busyDownload = false;
                DisposeWorker();
                Failed(workerErr);
            }
        }

        private void OnProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            if (IsDisposed || Disposing) { return; }
            try { _bar.Value = Math.Max(_bar.Minimum, Math.Min(_bar.Maximum, e.ProgressPercentage)); }
            catch (Exception) { }
            string mb;
            if (e.TotalBytesToReceive > 0)
            {
                mb = string.Format("{0:N1}/{1:N1} MB", e.BytesReceived / 1048576.0, e.TotalBytesToReceive / 1048576.0);
            }
            else
            {
                mb = string.Format("{0:N1} MB", e.BytesReceived / 1048576.0);
            }
            _detail.Text = string.Format(Resources.更新_下载进度,
                (_rel == null) ? "" : _rel.AssetName, e.ProgressPercentage, mb);
        }

        private void OnDownloadDone(object sender, AsyncCompletedEventArgs e)
        {
            _busyDownload = false;
            if (IsDisposed || Disposing)
            {
                return;
            }
            if (e.Cancelled)
            {
                CleanupPartial();
                return;
            }
            if (e.Error != null)
            {
                Failed(e.Error.GetBaseException().Message);
                return;
            }
            // 下载完成 ≠ 可用：用 GitHub 提供的 digest 校验 SHA256，
            // 不匹配（传输损坏或被篡改）则删除文件并提示，绝不开「重启更新」的门。
            string actual;
            if (!GitHubUpdate.VerifyPackage(_zipPath, _rel.AssetDigest, out actual))
            {
                string expected = (_rel.AssetDigest == null) ? "" : _rel.AssetDigest;
                try { if (File.Exists(_zipPath)) { File.Delete(_zipPath); } }
                catch (Exception) { }
                _busyDownload = false;
                FailedRaw(string.Format(Resources.更新_校验失败, ShortHash(expected), ShortHash(actual)));
                return;
            }
            Ready();
        }

        /// <summary>摘要截断为前 16 位十六进制，供失败提示展示（完整值无意义地占屏）。</summary>
        private static string ShortHash(string h)
        {
            if (string.IsNullOrEmpty(h)) { return "—"; }
            int i = h.IndexOf(':');
            if (i >= 0) { h = h.Substring(i + 1); }
            return (h.Length <= 16) ? h : h.Substring(0, 16) + "…";
        }

        private void Ready()
        {
            _bar.Value = _bar.Maximum;
            _detail.Text = Resources.更新_下载完成;
            ShowAction(_btnRetry, false);
            _btnLater.Text = Resources.更新_继续使用;
            _btnLater.Enabled = true;
            _btnRestart.Enabled = true;
        }

        private void Failed(string reason)
        {
            FailedRaw(string.Format(Resources.更新_下载失败, reason));
        }

        private void FailedRaw(string text)
        {
            _busyDownload = false;
            _bar.Value = 0;
            _detail.Text = text;
            // 失败后除了「重试」还要留一个明确的退出路径：
            // 原先「继续使用」保持 disabled，用户只能靠标题栏的 × 关闭对话框
            _btnLater.Text = Resources.更新_关闭;
            _btnLater.Enabled = true;
            ShowAction(_btnRetry, true);
            _lnk.Show();
        }

        private void CleanupPartial()
        {
            try
            {
                if (_zipPath != null && File.Exists(_zipPath)) { File.Delete(_zipPath); }
                if (_detail != null) { _detail.Text = ""; }
            }
            catch (Exception) { }
        }

        private void DisposeWorker()
        {
            if (_wc != null)
            {
                try { _wc.Dispose(); }
                catch (Exception) { }
                _wc = null;
            }
            _busyDownload = false;
        }

        private void _informationSet(string text)
        {
            _info.Text = text;
        }

        // ---------- 按钮行为 ----------

        private void OnRetry(object sender, EventArgs e)
        {
            StartCheck();
        }

        private void OnLater(object sender, EventArgs e)
        {
            // 继续使用：更新包保留在临时目录，下次检查到同一版本且大小一致时不再重复下载
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnAutoClose(object sender, EventArgs e)
        {
            Close();
        }

        private void OnOpenPage(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try { Process.Start(PageUrl()); }
            catch (Exception) { }
        }

        private void OnRestart(object sender, EventArgs e)
        {
            string workerErr = null;
            try
            {
                LaunchUpdateScript();
            }
            catch (Exception ex)
            {
                workerErr = ex.Message;
            }
            if (workerErr != null)
            {
                Failed(workerErr);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// 生成并启动自更新脚本：
        ///   1) 等待当前进程（按 PID）退出；
        ///   2) 用 .NET ZipFile（PS5+/.NET4.5+）解压覆盖安装目录，失败时退回
        ///      Shell.Application 的 CopyHere（XP 起可用）；
        ///   3) 重新启动 M1ToolPlus.exe。
        /// 脚本内容全 ASCII（cmd 对多字节编码敏感）；路径全部加双引号，
        /// 调用的 PowerShell 命令内部用单引号字面量，避免 cmd/PS 引号嵌套问题。
        /// </summary>
        private void LaunchUpdateScript()
        {
            string dest = Application.StartupPath;
            string zip = _zipPath;
            string pid = Process.GetCurrentProcess().Id.ToString();
            string dirTmp = Path.Combine(Path.GetTempPath(), "M1ToolPlusUpdate");
            Directory.CreateDirectory(dirTmp);
            string batPath = Path.Combine(dirTmp, "M1ToolPlus-update-" + pid + ".bat");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("rem M1ToolPlus self-update launcher (auto-generated)");
            sb.AppendLine("set \"EXE=" + dest + "\\M1ToolPlus.exe\"");
            sb.AppendLine("set \"ZIP=" + zip + "\"");
            sb.AppendLine("set \"DIR=" + dest + "\"");
            sb.AppendLine("rem wait until the running instance (PID " + pid + ") exits");
            sb.AppendLine(":wait");
            sb.AppendLine("tasklist /FI \"PID eq " + pid + "\" 2>nul | find /I \"M1ToolPlus.exe\" >nul 2>nul");
            sb.AppendLine("if not errorlevel 1 (");
            sb.AppendLine("    ping -n 2 127.0.0.1 >nul");
            sb.AppendLine("    goto wait");
            sb.AppendLine(")");
            sb.AppendLine("rem extract: PS5/.NET45+ .NET ZipFile first, Shell.Application fallback");
            sb.AppendLine("powershell -NoProfile -ExecutionPolicy Bypass -Command \"try { Add-Type -AssemblyName System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::ExtractToDirectory('%ZIP%', '%DIR%', $true) } catch { exit 1 }\"");
            sb.AppendLine("if errorlevel 1 (");
            sb.AppendLine("    powershell -NoProfile -Command \"$sh=(New-Object -ComObject Shell.Application); $z=$sh.NameSpace('%ZIP%'); $d=$sh.NameSpace('%DIR%'); if (($z -ne $null) -and ($d -ne $null)) { $d.CopyHere($z.Items(), 16) }\"");
            sb.AppendLine("    ping -n 6 127.0.0.1 >nul");
            sb.AppendLine(")");
            sb.AppendLine("start \"\" \"%EXE%\"");
            sb.AppendLine("exit");

            File.WriteAllLines(batPath, new string[] { sb.ToString() }, Encoding.Default);

            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c \"" + batPath + "\"");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(psi);
        }
    }
}
