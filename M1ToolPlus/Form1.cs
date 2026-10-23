using M1ToolPlus.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Globalization;

namespace M1ToolPlus
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Properties.Settings.Default.Language);
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(Properties.Settings.Default.Language);
            InitializeComponent();
            // 主窗口背景重绘量较大；双缓冲只作用于窗体自身的背景绘制，减少闪烁。
            this.DoubleBuffered = true;
            InitializeThemeSelector();
            ApplyLanguageSelection();
        }

        /// <summary>构造期回显主题下拉框时置位，避免触发保存/刷新。</summary>
        private bool suppressThemeChanged;
        private ComboBox comboBoxTheme;

        /// <summary>
        /// 在「软件设置」页动态增加「外观 / 主题」组（跟随系统 / 深色 / 浅色）。
        /// 不改进 Designer 的绝对布局：新组放入现有的 flowLayoutPanel22，
        /// 由流式布局排在「语言和地区」等组右侧。
        /// </summary>
        private void InitializeThemeSelector()
        {
            try
            {
                GroupBox box = new GroupBox();
                box.Name = "groupBoxTheme";
                box.Text = Resources.外观;
                box.AutoSize = true;
                box.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                box.TabStop = false;
                box.Font = new Font(box.Font, FontStyle.Bold);

                FlowLayoutPanel fp = new FlowLayoutPanel();
                fp.Name = "flowLayoutPanelTheme";
                fp.FlowDirection = FlowDirection.TopDown;
                fp.WrapContents = false;
                fp.AutoSize = true;
                fp.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                fp.Location = new Point(3, 18);

                Label label = new Label();
                label.Name = "labelTheme";
                label.Text = Resources.主题;
                label.AutoSize = true;
                label.Font = new Font(label.Font, FontStyle.Bold);
                label.Margin = new Padding(3, 0, 3, 0);

                ComboBox combo = new ComboBox();
                combo.Name = "comboBoxTheme";
                combo.DropDownStyle = ComboBoxStyle.DropDownList;
                combo.Font = new Font(box.Font, FontStyle.Regular);
                combo.Width = 120;
                combo.Margin = new Padding(3, 2, 3, 0);
                // 顺序与 ComboBoxTheme_SelectedIndexChanged 的索引一致。
                combo.Items.AddRange(new object[] { Resources.跟随系统, Resources.深色, Resources.浅色 });
                combo.SelectedIndexChanged += ComboBoxTheme_SelectedIndexChanged;

                fp.Controls.Add(label);
                fp.Controls.Add(combo);
                box.Controls.Add(fp);
                flowLayoutPanel22.Controls.Add(box);

                comboBoxTheme = combo;
                ApplyThemeSelection();
            }
            catch (Exception) { }
        }

        /// <summary>把主题下拉框回显成当前设置（空 = 跟随系统）。</summary>
        private void ApplyThemeSelection()
        {
            if (comboBoxTheme == null) { return; }
            int index = 0;
            string theme = Properties.Settings.Default.Theme;
            if (string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase)) { index = 1; }
            else if (string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase)) { index = 2; }
            suppressThemeChanged = true;
            try { comboBoxTheme.SelectedIndex = index; }
            finally { suppressThemeChanged = false; }
        }

        private void ComboBoxTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressThemeChanged) { return; }
            string theme;
            switch (comboBoxTheme.SelectedIndex)
            {
                case 1: theme = "dark"; break;
                case 2: theme = "light"; break;
                default: theme = ""; break;   // 跟随系统
            }
            Properties.Settings.Default.Theme = theme;
            Properties.Settings.Default.Save();
            // 不重启：重新读取设置并刷新所有已打开窗体
            UiTheme.Refresh();
        }

        /// <summary>构造期回显语言下拉框时置位，避免触发 SelectedIndexChanged 里的「保存 + 重启」。</summary>
        private bool suppressLanguageChanged;

        /// <summary>
        /// 把「语言和地区」下拉框回显成当前设置的语言。
        /// 只设 SelectedIndex，不写设置；设置项的权威值始终是 Settings.Language。
        /// 下拉项文案是**各语言自己的写法**（简体中文 / Русский / English），
        /// 不随界面语言翻译，见 Form1.Designer.cs 的 comboBox1.Items。
        /// </summary>
        private void ApplyLanguageSelection()
        {
            try
            {
                int index = 0; // 0 = 简体中文（默认/空设置均视为中文）
                string lang = Properties.Settings.Default.Language;
                if (!string.IsNullOrEmpty(lang))
                {
                    string two = CultureInfo.GetCultureInfo(lang).TwoLetterISOLanguageName;
                    if (two == "ru") { index = 1; }
                    else if (two == "en") { index = 2; }
                }
                suppressLanguageChanged = true;
                try { comboBox1.SelectedIndex = index; }
                finally { suppressLanguageChanged = false; }
            }
            catch (Exception) { suppressLanguageChanged = false; }
        }

        private Process process = new Process();
        private bool lprocess = false;
        private bool running = false;

        private void buttonListDev_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            this.Text = Resources.MifareOne_Tool_运行中;
            if (Properties.Settings.Default.NewScan)
            { File.Delete("libnfc.conf"); }
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(list_dev);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync();
        }

        void writeConfig(string devstr, bool autoscan = true, bool intscan = false)
        {
            string cfg = "allow_autoscan = " + (autoscan ? "true" : "false") + "\n";
            cfg += "allow_intrusive_scan = " + (intscan ? "true" : "false") + "\n";
            cfg += "device.name = \"NFC-Device\"\n";
            cfg += "device.connstring = \"" + devstr + "\"";
            File.WriteAllText("libnfc.conf", cfg);
            curDevice.Text = Resources.设备串口 + devstr.Replace("pn532_uart:", "").Replace(":115200", "");
        }

        void default_rpt(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == ToolProcess.ProgressLiveLine)
            {
                // 未换行的进度（nfc-mfclassic 的 "|...x...|"）：原地刷新最后一行，
                // 不能走 logAppend，否则每个字符都会变成独立的一行。
                logLive((string)e.UserState);
                return;
            }
            if (e.ProgressPercentage == 100)
            {
                logAppend((string)e.UserState);
                logAppend("");
                Text = Resources.MifareOne_Tool_运行完毕;
            }
            else if (e.ProgressPercentage == 101)
            {
                logAppend((string)e.UserState);
                if (lastuid != "")
                {
                    if (File.Exists(omfd) && new FileInfo(omfd).Length > 1)
                    {
                        Directory.CreateDirectory("auto_keys");
                        string filename = "auto_keys\\" + lastuid + "_" + DateTime.Now.ToString().Replace("/", "-").Replace(" ", "_").Replace(":", "-") + ".mfd";
                        if (File.Exists(filename))
                        {
                            File.Delete(filename);
                        }
                        File.Move(omfd, filename);
                        logAppend(Resources._已自动保存 + filename + "##");
                    }
                    else
                    {
                        File.Delete(omfd);
                        logAppend(Resources._缓存文件异常);
                    }
                    lastuid = "";
                }
                else
                {
                    SaveFileDialog ofd = new SaveFileDialog();
                    ofd.AddExtension = true;
                    //ofd.DefaultExt = ".mfd";
                    ofd.Title = Resources.请选择MFD文件保存位置及文件名;
                    ofd.OverwritePrompt = true;
                    ofd.Filter = Resources.DUMP文件_dump_MFD文件_mfd;
                    if (File.Exists(omfd) && new FileInfo(omfd).Length > 1)
                    {
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            if (File.Exists(ofd.FileName))
                            {
                                File.Delete(ofd.FileName);
                            }
                            File.Move(omfd, ofd.FileName);
                            logAppend(Resources._已保存 + ofd.FileName + "##");
                        }
                        else
                        {
                            File.Delete(omfd);
                            logAppend(Resources._未保存);
                        }
                    }
                    else
                    {
                        File.Delete(omfd);
                        logAppend(Resources._缓存文件异常);
                    }
                }
                omfd = "";
                logAppend("");
                Text = Resources.MifareOne_Tool_运行完毕;
            }
            else if (e.ProgressPercentage == 102)
            {
                logAppend((string)e.UserState);
                logAppend(Resources._Nonce收集完毕);
                logAppend(Resources.您可以上传到云计算服务节点进行计算);
                logAppend("");

                Text = Resources.MifareOne_Tool_运行完毕;
            }
            else if (e.ProgressPercentage == 103)
            {
                logAppend(Resources.识别了以下设备);
                List<string> myReaders = (List<string>)(e.UserState);
                foreach (string reader in myReaders)
                {
                    logAppend(reader);
                }
                if (myReaders.Count > 0)
                {
                    logAppend(Resources.将自动选择首个设备 + myReaders.First());
                    writeConfig(myReaders.First());
                    SetDeviceCombo.Items.Clear();
                    SetDeviceCombo.Items.AddRange(myReaders.ToArray());
                    SetDeviceCombo.SelectedIndex = 0;
                }
                else
                {
                    logAppend(Resources.没有发现任何有效的NFC设备);
                    logAppend(Resources.请检查接线是否正确_驱动是否正常安装_设备电源是否已经打开_);
                }
            }
            else
            {
                logAppend((string)e.UserState);
            }
            Application.DoEvents();
        }

        /// <summary>终端里当前「临时进度行」的起始位置；-1 表示没有。</summary>
        private int liveLineStart = -1;

        private void logAppend(string msg)
        {
            if (liveLineStart >= 0)
            {
                // 正式日志到达：先删掉临时进度行，避免留下半截进度条。
                // 注意：终端是只读 RichTextBox，此时 SelectedText = "" 会被系统静默忽略
                // （插入/替换却有效），必须临时关掉只读再删，否则进度行会残留，
                // 正式行接在它后面形成「|...| |...|」重复。
                int start = Math.Min(liveLineStart, richTextBox1.TextLength);
                if (start < richTextBox1.TextLength)
                {
                    bool readOnly = richTextBox1.ReadOnly;
                    richTextBox1.ReadOnly = false;
                    try
                    {
                        richTextBox1.Select(start, richTextBox1.TextLength - start);
                        richTextBox1.SelectedText = "";
                    }
                    finally
                    {
                        richTextBox1.ReadOnly = readOnly;
                    }
                }
                liveLineStart = -1;
            }
            richTextBox1.AppendText(msg + "\n");
            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.ScrollToCaret();
        }

        /// <summary>
        /// 原地刷新终端最后一行（临时进度行）。
        /// nfc-mfclassic 用 printf(".") 逐块输出且不换行，只在扇区边界 fflush；
        /// 这里把每一小段重写到最后一行上，用户就能看到进度条逐格增长。
        /// </summary>
        private void logLive(string msg)
        {
            if (msg == null) { return; }
            if (liveLineStart < 0 || liveLineStart > richTextBox1.TextLength)
            {
                liveLineStart = richTextBox1.TextLength;
            }
            richTextBox1.Select(liveLineStart, richTextBox1.TextLength - liveLineStart);
            richTextBox1.SelectedText = msg;
            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.ScrollToCaret();
        }

        void list_dev(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/nfc-scan-device.exe");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行检测设备); running = true;
            List<string> myReader = new List<string>();
            // 该处需在读取输出的同时收集设备名（Issue #26/#9），故传入行处理器而非直接输出到终端区。
            // libnfc 输出为 UTF-8；.NET 4.0 无 StandardOutputEncoding，默认按 ANSI 解码会中文乱码。
            ToolProcess.PumpOutput(process, b, delegate(string line)
            {
                if (string.IsNullOrEmpty(line)) { return; }
                // 回显到终端区（上游行为，ToolProcess 重构时被漏掉）：
                // 不回显的话点「检测连接」后终端一片空白，无法判断卡在哪一步。
                b.ReportProgress(0, line);
                if (Properties.Settings.Default.NewScan)
                {
                    //Issue #26/#9: 原仅匹配 pn532_uart, ACR122U(acr122_pcsc) 等设备永远无法进入设备列表,
                    //这里放宽为常见 libnfc 连接串前缀
                    Match m = Regex.Match(line, "(?i)\\b(pn532_uart:COM\\d+:\\d+|acr122_pcsc:[^\\s\"']+|pn532_[a-z]+:[^\\s\"']+|openpcsc:[^\\s\"']+|arygon:[^\\s\"']+|freegd:[^\\s\"']+)\\b");
                    if (m.Success)
                    {
                        myReader.Add(m.Value);
                    }
                }
            });
            // Issue #39 修复：nfc-scan-device 在无设备 / 不兼容读卡器上可能长时间阻塞
            // （Win10 上有用户报告达 100s 以上），无超时等待会让界面永久停在运行中状态。
            // 这里设 60 秒上限，超时则终止子进程；先 WaitForExit(超时) 再无参 WaitForExit()，
            // 确保异步输出事件已全部处理完（否则可能丢失最后几行设备信息）。
            if (!process.WaitForExit(60000))
            {
                try { process.Kill(); } catch (Exception) { }
                process.WaitForExit(3000);
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._程序已被强制停止);
                b.ReportProgress(103, myReader);
                return;
            }
            process.WaitForExit();
            lprocess = false;
            running = false;
            b.ReportProgress(103, myReader);
            b.ReportProgress(100, Resources._运行完毕);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            logAppend(Resources._软件版本 + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            localVersionLabel.Text = Resources.本地版本 + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Directory.CreateDirectory("auto_keys");
            checkBoxAutoABN.Checked = Properties.Settings.Default.AutoABN;
            checkBoxWriteProtect.Checked = Properties.Settings.Default.WriteCheck;
            checkBoxAutoLoadKey.Checked = Properties.Settings.Default.AutoLoadUidKey;
            richTextBox1.ForeColor = Properties.Settings.Default.MainCLIColor;
            buttonCLIColor.ForeColor = Properties.Settings.Default.MainCLIColor;
            checkBoxDefIsAdv.Checked = Properties.Settings.Default.DefIsAdv;
            checkBoxHardLowCost.Checked = Properties.Settings.Default.HardLowCost;
            checkBoxNewScan.Checked = Properties.Settings.Default.NewScan;
            checkBoxCuidKeyOver.Checked = Properties.Settings.Default.CuidKeyOver;
            // 初始页签不在这里切：句柄创建阶段切换会被过期的 TCN_SELCHANGE 覆盖，
            // 统一延后到 Shown（见 ApplyInitialTabAfterShown）。
            //Issue #26: 恢复上次使用的设备，避免重复全量扫描
            //（勾选 NewScan 后每次全扫, Win10 下 libnfc 串口轮询可超 100 秒）
            //仅恢复显示与当前配置, 不重写 libnfc.conf（保护用户手写的 allow_autoscan=false 等）
            try
            {
                if (File.Exists("libnfc.conf"))
                {
                    Match lastDevMatch = Regex.Match(File.ReadAllText("libnfc.conf"), "device\\.connstring\\s*=\\s*\"([^\"]+)\"");
                    if (lastDevMatch.Success && !string.IsNullOrEmpty(lastDevMatch.Groups[1].Value))
                    {
                        string lastDev = lastDevMatch.Groups[1].Value;
                        SetDeviceCombo.SelectedIndexChanged -= SetDeviceCombo_SelectedIndexChanged;
                        SetDeviceCombo.Items.Add(lastDev);
                        SetDeviceCombo.SelectedIndex = SetDeviceCombo.Items.Count - 1;
                        SetDeviceCombo.SelectedIndexChanged += SetDeviceCombo_SelectedIndexChanged;
                        curDevice.Text = Resources.设备串口 + lastDev.Replace("pn532_uart:", "").Replace(":115200", "");
                        logAppend(Resources.已恢复上次使用的设备 + lastDev);
                    }
                }
            }
            catch (Exception) { /* libnfc.conf 损坏时忽略, 走正常手动检测流程 */ }
            //File.Delete("libnfc.conf");//用户COM口一般不常变化
            checkBoxMultiDev.Checked = Properties.Settings.Default.MultiMode;
            if (Properties.Settings.Default.MultiMode)
            {
                if (File.Exists("libnfc.conf"))
                {
                    string[] conf = File.ReadAllLines("libnfc.conf");
                    foreach (string line in conf)
                    {
                        if (line.StartsWith("device.connstring = \"pn532_uart:"))
                        {
                            curDevice.Text = Resources.设备串口 + line.Replace("device.connstring = \"pn532_uart:", "").Replace(":115200\"", "");
                        }
                    }
                }
            }
            // 统一界面外观（集中式主题，不改动布局参数）
            UiTheme.Apply(this);
            // 终端字体/字号：微软雅黑 + 设置页「文字大小」，Ctrl+滚轮也统一走这里
            richTextBox1.ZoomStep += delegate(int step) { StepTerminalFont(step); };
            ApplyTerminalFontSettings();
            // 初始页签的切换要等消息循环把**句柄创建时排下的那条 TCN_SELCHANGE**
            // 处理完之后再做（那条通知报告的是旧索引 0）。
            // 详见 SelectTab 的说明。
            this.Shown += delegate
            {
                try { BeginInvoke(new MethodInvoker(ApplyInitialTabAfterShown)); }
                catch (Exception) { ApplyInitialTabAfterShown(); }
            };
        }

        /// <summary>窗体显示后确定初始页签。</summary>
        private void ApplyInitialTabAfterShown()
        {
            try
            {
                if (Properties.Settings.Default.DefIsAdv) { SelectTab(1); }
                ApplyInitialTab();
                DumpUiInfoIfRequested();
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 诊断用：M1ToolPlus.exe --ui-info 启动时把窗体/DPI/关键控件尺寸写入
        /// %TEMP%\m1tool-ui-info.txt，用于核对 1:1 缩放与布局实际值。
        /// </summary>
        private void DumpUiInfoIfRequested()
        {
            try
            {
                bool want = false;
                foreach (string a in Environment.GetCommandLineArgs()) { if (a == "--ui-info") { want = true; break; } }
                if (!want) { return; }
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("ClientSize=" + ClientSize.Width + "x" + ClientSize.Height);
                sb.AppendLine("WindowSize=" + Size.Width + "x" + Size.Height);
                sb.AppendLine("Bounds=" + Bounds);
                sb.AppendLine("ClientOrigin=" + PointToScreen(Point.Empty));
                sb.AppendLine("AutoScaleMode=" + AutoScaleMode);
                sb.AppendLine("AutoScaleDimensions=" + AutoScaleDimensions);
                sb.AppendLine("CurrentAutoScaleDimensions=" + CurrentAutoScaleDimensions);
                sb.AppendLine("DeviceDpi=" + (int)CreateGraphics().DpiX);
                sb.AppendLine("Font=" + Font.Name + " " + Font.SizeInPoints.ToString("0.##") + "pt unit=" + Font.Unit + " height=" + Font.Height);
                sb.AppendLine("WorkingArea=" + Screen.FromControl(this).WorkingArea);
                sb.AppendLine("ScreenBounds=" + Screen.FromControl(this).Bounds);
                // 语言诊断：切换语言不生效时先看这三行（设置值 / 线程文化 / 实际渲染文字）
                sb.AppendLine("SettingLanguage=" + (Properties.Settings.Default.Language ?? "(null)"));
                sb.AppendLine("CurrentUICulture=" + System.Threading.Thread.CurrentThread.CurrentUICulture.Name);
                sb.AppendLine("CurrentCulture=" + System.Threading.Thread.CurrentThread.CurrentCulture.Name);
                sb.AppendLine("TabPage1Text=" + tabControl1.TabPages[0].Text);
                sb.AppendLine("ButtonListDevText=" + buttonListDev.Text);
                sb.AppendLine("LanguageCombo=" + comboBox1.SelectedIndex + ":" + comboBox1.Text);
                // 三个选项必须始终是各自语言的写法，不能随界面语言翻译
                string langItems = "";
                foreach (object item in comboBox1.Items)
                {
                    if (langItems.Length > 0) { langItems += "|"; }
                    langItems += item;
                }
                sb.AppendLine("LanguageItems=" + langItems);
                // 终端诊断：文本确实写进去了但被页签盖住时，TextLength > 0 而界面上看不到
                sb.AppendLine("TerminalTextLength=" + richTextBox1.TextLength);
                sb.AppendLine("TerminalFirstLine=" + (richTextBox1.Lines.Length > 0 ? richTextBox1.Lines[0] : "(empty)"));
                // 字号诊断：设置页显示值 / 实际字体名 / 像素高度 / 缩放因子必须互相一致
                sb.AppendLine("TerminalFontSize=" + numericCLIFontSize.Value + "pt");
                sb.AppendLine("TerminalFontFamily=" + richTextBox1.Font.FontFamily.Name);
                sb.AppendLine("TerminalFontPx=" + richTextBox1.Font.SizeInPoints.ToString("0.##") + "pt h=" + richTextBox1.Font.Height);
                sb.AppendLine("TerminalZoom=" + richTextBox1.ZoomFactor.ToString("0.##"));
                string[] names = new string[] { "tabControl1", "richTextBox1", "statusStrip1", "groupBox1", "buttonScanCard", "comboBox1" };
                foreach (string n in names)
                {
                    Control[] found = Controls.Find(n, true);
                    if (found != null && found.Length > 0)
                    {
                        Control c = found[0];
                        sb.AppendLine(n + "=" + c.Bounds + " font=" + c.Font.Name + " " + c.Font.SizeInPoints.ToString("0.##") + "pt h=" + c.Font.Height);
                        sb.AppendLine(n + ".Screen=" + c.PointToScreen(Point.Empty));
                    }
                }
                string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "m1tool-ui-info.txt");
                System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 支持以命令行参数指定初始标签页：M1ToolPlus.exe --tab 2
        /// 供自动化截图脚本（scripts\screenshot-tabs.ps1）切换页面用，不影响正常启动。
        /// </summary>
        private void ApplyInitialTab()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "--tab")
                    {
                        int idx;
                        if (int.TryParse(args[i + 1], out idx) && idx >= 0 && idx < tabControl1.TabPages.Count)
                        {
                            SelectTab(idx);
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 切换到指定页签（带下标校验）。
        ///
        /// 注意调用时机：**不能在 Load 里**直接切。句柄创建时原生控件会排一条
        /// TCN_SELCHANGE（报告旧索引 0），消息循环启动后处理它时会把 WinForms 的
        /// 内部索引写回 0，页签可见性随之按 0 还原——表现就是「标签条高亮第 1 页、
        /// 页面内容却是第 0 页」。这里统一由 Shown + BeginInvoke 延后执行。
        /// </summary>
        private void SelectTab(int idx)
        {
            try
            {
                if (idx < 0 || idx >= tabControl1.TabPages.Count) { return; }
                tabControl1.SelectedIndex = idx;
            }
            catch (Exception) { }
        }

        private void buttonScanCard_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(list_tag);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync();
        }

        void list_tag(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/nfc-list.exe");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行扫描卡片);
            running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            lprocess = false; running = false;
            b.ReportProgress(100, Resources._运行完毕);
        }

        string omfd = "";

        private string GetUID()
        {
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/nfc-list.exe");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            Process p;
            if (!ToolProcess.Start(psi, out p)) { return ""; }
            p.WaitForExit();
            // 与 ToolProcess 一致按 GBK(936) 解码：nfc-bin 的中文提示是 ANSI 字节，
            // 按 UTF-8 解码会得到"ʹ��"之类的乱码（UID 本身是 ASCII，不影响正则）。
            string rawStr;
            using (StreamReader sr = new StreamReader(p.StandardOutput.BaseStream, ToolProcess.ConsoleEncoding))
            {
                rawStr = sr.ReadToEnd();
            }
            string uid;
            string pattern = @"UID\s\(NFCID1\)\: ([0-9A-Fa-f]{2}\s\s[0-9A-Fa-f]{2}\s\s[0-9A-Fa-f]{2}\s\s[0-9A-Fa-f]{2})";
            if (Regex.IsMatch(rawStr, pattern))
            {
                uid = Regex.Match(rawStr, pattern).Captures[0].Value.Replace(" ", "").Replace("UID(NFCID1):", ""); ;
            }
            else
            {
                uid = "";
            }
            return uid;
        }
        private void LoadUidKey(string uid)
        {
            if (!Directory.Exists("auto_keys"))
            {
                Directory.CreateDirectory("auto_keys");
                return;
            }
            if (uid.Length < 8) { return; }
            logAppend(Resources.正在检索是否存在key_mfd);
            List<string> files = Directory.EnumerateFiles("auto_keys", "*.mfd").ToList<string>();
            files.Reverse();//保证拿到最新的
            for (int i = 0; i < files.Count; i++)
            {
                if (files[i].StartsWith("auto_keys\\" + uid))
                {
                    logAppend(Resources.已找到_K + files[i]);
                    keymfd = files[i];
                    buttonSelectKey.Text = "K=" + files[i];
                    return;
                }
            }
            return;
        }

        string lastuid = "";

        private void buttonMfRead_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            string rmfd = "MfRead.tmp";
            string kt = "A";
            string nn = "";
            string uid;
            if (checkBoxAutoLoadKey.Checked)
            {
                uid = GetUID();
                LoadUidKey(uid);
            }
            if (checkBoxAutoSave.Checked)
            {
                lastuid = GetUID();
            }
            if (checkBoxAutoABN.Checked && keymfd != "")
            {
                kt = "C";
                logAppend(Resources.正在使用智能KeyABN);
            }
            else
            {
                switch (MessageBox.Show(Resources.使用KeyA_是_或KeyB_否_还是不使用_用于全新白卡_, "KeyA/B/N", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information))
                {
                    case DialogResult.No:
                        kt = "B";
                        break;

                    case DialogResult.Cancel:
                        nn = "x";
                        break;
                }
            }
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(mf_read);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync(new string[] { rmfd, kt, nn });
            omfd = rmfd;

        }

        void mf_read(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/nfc-mfclassic.exe");
            string[] args = (string[])e.Argument;
            psi.Arguments = "r " + args[1] + " u \"" + args[0] + "\"";
            if (keymfd != "" && args[2] == "")
            {
                psi.Arguments += " \"" + keymfd + "\" f";
            }
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行读取卡片); running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            lprocess = false; running = false;
            if (process.ExitCode == 0)
            {
                b.ReportProgress(101, Resources._运行完毕);
            }
            else
            {
                b.ReportProgress(100, Resources._运行出错);
                File.Delete(args[0]);
            }
        }

        private void buttonCLI_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists("nfc-bin")) { logAppend(Resources.加载的文件不存在); return; }
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe");
            psi.WorkingDirectory = "nfc-bin";
            Process cli;
            ToolProcess.Start(psi, out cli);
        }

        private string keymfd = "";

        private void buttonSelectKey_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.CheckFileExists = true;
            ofd.Filter = Resources.MFD文件_mfd_DUMP文件_dump;
            ofd.Title = Resources.请选择一个包含目标卡密钥的MFD文件_通常是已经破解出的该卡;
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                keymfd = ofd.FileName;
                buttonSelectKey.Text = "K=" + ofd.SafeFileName;
            }
            else
            {
                keymfd = "";
                buttonSelectKey.Text = Resources.选择key_mfd;
            }
        }

        bool writecheck(string file)
        {
            if (checkBoxWriteProtect.Checked == false)
            { return true; }//如果禁用，直接假装检查成功
            S50 card = new S50();
            try
            {
                card.LoadFromMfd(file);
            }
            catch (IOException ioe)
            {
                MessageBox.Show(ioe.Message, Resources.打开出错, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (card.Verify()[16] == 0x00)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void buttonMfWrite_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            string rmfd = "";
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.CheckFileExists = true;
            ofd.Filter = Resources.MFD文件_mfd_dump;
            ofd.Title = Resources.请选择需要写入的MFD文件;
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                rmfd = ofd.FileName;
            }
            else
            {
                return;
            }
            if (!writecheck(rmfd)) { MessageBox.Show(Resources.将要写入的文件存在错误_请用高级模式中的Hex工具打开查看, Resources.错误, MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            string kt = "A";
            string nn = "";
            if (checkBoxAutoABN.Checked && keymfd != "")
            {
                kt = "C";
                logAppend(Resources.正在使用智能KeyABN);
            }
            else
            {
                switch (MessageBox.Show(Resources.使用KeyA_是_或KeyB_否_还是不使用_用于全新白卡_, Resources.KeyA_B_N, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information))
                {
                    case DialogResult.No:
                        kt = "B";
                        break;

                    case DialogResult.Cancel:
                        nn = "x";
                        break;
                }
            }
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(mf_write);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync(new string[] { rmfd, kt, nn });
        }

        void mf_write(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/nfc-mfclassic.exe");
            string[] args = (string[])e.Argument;
            psi.Arguments = "w " + args[1] + " u \"" + args[0] + "\"";
            if (keymfd != "" && args[2] == "")
            {
                psi.Arguments += " \"" + keymfd + "\" f";
            }
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行写入M1卡片); running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            lprocess = false; running = false;
            b.ReportProgress(100, Resources._运行完毕);
        }

        private void buttonMfoc_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            string rmfd = "Mfoc.tmp";
            string key = "";
            if (Control.ModifierKeys == Keys.Control)
            {
                string[] ks = Interaction.InputBox(Resources.请输入已知的Key_以英文半角逗号分隔, Resources.请输入已知Key, Properties.Settings.Default.LastTryKey, -1, -1).Trim().Split(',');
                if (ks.Length > 0)
                {
                    Properties.Settings.Default.LastTryKey = string.Join(",", ks);
                    foreach (string k in ks)
                    {
                        string pat = "[0-9A-Fa-f]{12}";
                        if (Regex.IsMatch(k, pat))
                        {
                            key += "-k " + k.Substring(0, 12) + " ";
                        }
                    }
                }
            }
            if (checkBoxAutoSave.Checked)
            {
                lastuid = GetUID();
            }
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(mfoc);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync(new string[] { rmfd, key });
            omfd = rmfd;
        }

        void mfoc(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/mfoc.exe");
            string[] args = (string[])e.Argument;
            psi.Arguments = args[1] + " -O \"" + args[0] + "\"";
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行MFOC解密); running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            lprocess = false; running = false;
            if (process.ExitCode == 0)
            {
                b.ReportProgress(101, Resources._运行完毕);
            }
            else
            {
                b.ReportProgress(100, Resources._运行出错);
                File.Delete(args[0]);
            }
        }

        private void buttonUidReset_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(reset_uid);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync();
        }

        public static string hex(byte[] bytes)
        {
            StringBuilder ret = new StringBuilder();
            foreach (byte b in bytes)
            {
                //{0:X2} 大写
                ret.AppendFormat("{0:x2}", b);
            }
            return ret.ToString();
        }

        void reset_uid(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/nfc-mfsetuid.exe");
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] uid = new byte[4];
            rng.GetNonZeroBytes(uid);
            psi.Arguments = hex(uid) + "2B0804006263646566676869";
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行重置UID卡片卡号); running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            lprocess = false; running = false;
            b.ReportProgress(100, Resources._运行完毕);
        }

        private void buttonUidFormat_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            if (MessageBox.Show(Resources.该操作将会清空UID卡内全部数据_清空后不可恢复_请确认是否, Resources.危险操作警告, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
            { return; }
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(format_uid);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync();
        }

        void format_uid(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/nfc-mfsetuid.exe");
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] uid = new byte[4];
            rng.GetNonZeroBytes(uid);
            psi.Arguments = "-f " + hex(uid) + "2B0804006263646566676869";
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行UID卡片全格); running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            lprocess = false; running = false;
            b.ReportProgress(100, Resources._运行完毕);
        }

        private void buttonBmfRead_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            string rmfd = "BmfRead.tmp";
            if (checkBoxAutoSave.Checked)
            {
                lastuid = GetUID();
            }
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(bmf_read);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync(new string[] { rmfd });
            omfd = rmfd;
        }

        void bmf_read(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/nfc-mfclassic.exe");
            string[] args = (string[])e.Argument;
            psi.Arguments = "R A u \"" + args[0] + "\"";
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行UID卡片读取); running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            lprocess = false; running = false;
            if (process.ExitCode == 0)
            {
                b.ReportProgress(101, Resources._运行完毕);
            }
            else
            {
                b.ReportProgress(100, Resources._运行出错);
                File.Delete(args[0]);
            }
        }

        private void buttonBmfWrite_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            string rmfd = "";
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.CheckFileExists = true;
            ofd.Filter = Resources.MFD文件_mfd_dump;
            ofd.Title = Resources.请选择需要写入的MFD文件;
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                rmfd = ofd.FileName;
            }
            else
            {
                return;
            }
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(bmf_write);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync(new string[] { rmfd });
        }

        void bmf_write(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/nfc-mfclassic.exe");
            string[] args = (string[])e.Argument;
            psi.Arguments = "W A u \"" + args[0] + "\"";
            if (keymfd != "")
            {
                psi.Arguments += " \"" + keymfd + "\" f";
            }
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行UID卡片写入); running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            lprocess = false; running = false;
            b.ReportProgress(100, Resources._运行完毕);
        }

        private void buttonConClr_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            liveLineStart = -1;
        }

        private void buttonConSave_Click(object sender, EventArgs e)
        {
            string logtext = richTextBox1.Text;
            File.WriteAllText("m1t.log", logtext, Encoding.UTF8);
            MessageBox.Show(Resources.终端内容已保存至m1t_log文件, Resources.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }



        private void buttonUidWrite_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            byte[] buid = new byte[4];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetNonZeroBytes(buid);
            string uid = Interaction.InputBox(Resources.请输入需要写入的UID卡号_共8位十六进制数_如E44A3B, Resources.请输入UID号, hex(buid), -1, -1).Trim();
            string pat = "[0-9A-Fa-f]{8}";
            if (!Regex.IsMatch(uid, pat))
            {
                MessageBox.Show(Resources.输入的UID号不合法, "InputError", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(set_uid);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync(uid);
        }

        void set_uid(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/nfc-mfsetuid.exe");
            psi.Arguments = "" + ((string)e.Argument).Substring(0, 8) + "2B0804006263646566676869";
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行UID卡片设定卡号); running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            lprocess = false; running = false;
            b.ReportProgress(100, Resources._运行完毕);
        }

        private void buttonMfcuk_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            Text = Resources.MifareOne_Tool_运行中;
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(Mfcuk);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync();
        }

        void Mfcuk(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            ProcessStartInfo psi = new ProcessStartInfo("cmd");
            psi.Arguments = "/k mfcuk.exe -v 3 -C -R -1 -s 250 -S 250";
            psi.WorkingDirectory = "nfc-bin";
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行全加密卡片爆破);
            process.WaitForExit();
            lprocess = false; running = false;
            b.ReportProgress(100, Resources._运行完毕);
        }

        bool cuidKeyOver = false;

        private void buttonCmfWrite_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            string rmfd = "";
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.CheckFileExists = true;
            ofd.Filter = Resources.MFD文件_mfd_dump;
            ofd.Title = Resources.请选择需要写入的MFD文件;
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                rmfd = ofd.FileName;
            }
            else
            {
                return;
            }
            if (!writecheck(rmfd)) { MessageBox.Show(Resources.将要写入的文件存在错误_请用高级模式中的Hex工具打开查看, Resources.错误, MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            if (keymfd == "" && Properties.Settings.Default.CuidKeyOver)
            {
                cuidKeyOver = true;
                string uid = GetUID();
                S50 empty = new S50(Utils.Hex2Block(uid, 4));
                empty.ExportToMfd("cuid_empty.kmf");
                keymfd = "cuid_empty.kmf";
                logAppend(Resources.已启用CUID空卡写入补丁);
            }
            string kt = "A";
            string nn = "";
            if (checkBoxAutoABN.Checked && keymfd != "")
            {
                kt = "C";
                logAppend(Resources.正在使用智能KeyABN);
            }
            else
            {
                switch (MessageBox.Show(Resources.使用KeyA_是_或KeyB_否_还是不使用_用于全新白卡_, "KeyA/B/N", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information))
                {
                    case DialogResult.No:
                        kt = "B";
                        break;

                    case DialogResult.Cancel:
                        nn = "x";
                        break;
                }
            }
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(cmf_write);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync(new string[] { rmfd, kt, nn });
        }

        void cmf_write(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/nfc-mfclassic.exe");
            string[] args = (string[])e.Argument;
            psi.Arguments = "c " + args[1] + " u \"" + args[0] + "\"";
            if (keymfd != "" && args[2] == "")
            {
                psi.Arguments += " \"" + keymfd + "\" f";
            }
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行CUID_FUID卡片写入); running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            lprocess = false; running = false;
            if (cuidKeyOver == true)
            {
                keymfd = "";
                cuidKeyOver = false;
                File.Delete("cuid_empty.kmf");
            }
            b.ReportProgress(100, Resources._运行完毕);
        }

        private void buttonKill_Click(object sender, EventArgs e)
        {
            if (lprocess)
            {
                if (process.HasExited == false)
                {
                    process.Kill();
                    this.Text = Resources.MifareOne_Tool_已终止;
                    logAppend(Resources._程序已被强制停止);
                }
            }
        }

        private void buttonLockUfuid_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            if (MessageBox.Show(Resources.该操作将会锁死UFUID卡片_锁死后不可恢复_无法再次更改0, Resources.危险操作警告, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
            { return; }
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(lock_ufuid);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync();
        }

        void lock_ufuid(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/nfc-mfsetuid.exe");
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] uid = new byte[4];
            rng.GetNonZeroBytes(uid);
            psi.Arguments = "-q -l";
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行UFUID卡片锁定); running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            lprocess = false; running = false;
            b.ReportProgress(100, Resources._运行完毕);
        }

        private void buttonMfFormat_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (keymfd == "") { MessageBox.Show(Resources.未选择有效key_mfd, Resources.无密钥, MessageBoxButtons.OK, MessageBoxIcon.Error); return; } this.Text = Resources.MifareOne_Tool_运行中;
            string rmfd = keymfd;
            string kt = "A";
            if (checkBoxAutoABN.Checked && keymfd != "")
            {
                kt = "C";
                logAppend(Resources.正在使用智能KeyABN);
            }
            else
            {
                switch (MessageBox.Show(Resources.使用KeyA_是_或KeyB_否, "KeyA/B", MessageBoxButtons.YesNo, MessageBoxIcon.Information))
                {
                    case DialogResult.No:
                        kt = "B";
                        break;
                }
            }
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(mf_format);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync(new string[] { rmfd, kt });
        }

        void mf_format(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/nfc-mfclassic.exe");
            string[] args = (string[])e.Argument;
            psi.Arguments = "f " + args[1] + " u \"" + args[0] + "\"";
            psi.Arguments += " \"" + keymfd + "\" f";
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行格式化M1卡片); running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            lprocess = false; running = false;
            b.ReportProgress(100, Resources._运行完毕);
        }

        private void buttonEMfoc_Click(object sender, EventArgs e)
        {
            buttonMfoc_Click(sender, e);
        }

        private void buttonEscan_Click(object sender, EventArgs e)
        {
            buttonListDev_Click(sender, e);
        }

        private void buttoEScanCard_Click(object sender, EventArgs e)
        {
            buttonScanCard_Click(sender, e);
        }

        private void buttonECmfoc_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            string rmfd = "Mfoc.tmp";
            string key = "";
            string[] ks = Interaction.InputBox(Resources.请输入已知的Key_以英文半角逗号分隔, Resources.请输入已知Key, "FFFFFFFFFFFF", -1, -1).Trim().Split(',');
            if (ks.Length > 0)
            {
                foreach (string k in ks)
                {
                    string pat = "[0-9A-Fa-f]{12}";
                    if (Regex.IsMatch(k, pat))
                    {
                        key += "-k " + k.Substring(0, 12) + " ";
                    }
                }
            }
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(mfoc);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync(new string[] { rmfd, key });
            omfd = rmfd;
        }

        private void buttonEUpdate_Click(object sender, EventArgs e)
        {
            toolStripCheckUpdate_ButtonClick(sender, e);
        }

        private void buttonESelectKey_Click(object sender, EventArgs e)
        {
            buttonSelectKey_Click(sender, e);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            buttonMfRead_Click(sender, e);
        }

        private void buttonEMfWrite_Click(object sender, EventArgs e)
        {
            buttonMfWrite_Click(sender, e);
        }

        private void buttonECuidWrite_Click(object sender, EventArgs e)
        {
            buttonCmfWrite_Click(sender, e);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            buttonLockUfuid_Click(sender, e);
        }

        private void buttonEAdv_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 1;
        }

        private void buttonEnAcr122u_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            if (MessageBox.Show(Resources.同时打开ACR122U支持可能会引起操作速度下降_请确认是否, Resources.提示信息, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
            { Text = "M1ToolPlus"; return; }
            // Issue #9/#31 修复：原代码检查 libnfc(PN532Only).dll / libnfc(ACR122U).dll，
            // 这两个文件名在发布包中并不存在（包内实际是 libnfc(532).dll / libnfc(ACR122Uslow).dll），
            // 导致该开关永远不生效。这里改为按包内实际文件名做可逆切换。
            const string curDll = "nfc-bin/libnfc.dll";
            const string compatDll = "nfc-bin/libnfc(ACR122Uslow).dll";
            const string pnOnlyDll = "nfc-bin/libnfc(532).dll";
            const string backupDll = "nfc-bin/libnfc.dll.acr122u.bak";
            lprocess = true;
            try
            {
                if (!File.Exists(curDll))
                {
                    logAppend(Resources._缓存文件异常);
                }
                else if (SameFileContent(curDll, compatDll))
                {
                    // 当前已是兼容（慢速）模式 → 切回默认驱动
                    if (File.Exists(backupDll)) { File.Copy(backupDll, curDll, true); File.Delete(backupDll); }
                    else if (File.Exists(pnOnlyDll)) { File.Copy(pnOnlyDll, curDll, true); }
                    else { logAppend(Resources._缓存文件异常); return; }
                    logAppend(Resources.ACR122U兼容模式_已切回);
                }
                else
                {
                    // 进入兼容（慢速）模式：先备份当前驱动，保证可切回
                    if (!File.Exists(compatDll)) { logAppend(Resources._缓存文件异常); return; }
                    File.Copy(curDll, backupDll, true);
                    File.Copy(compatDll, curDll, true);
                    logAppend(Resources.ACR122U兼容模式_已切换);
                    logAppend(Resources.默认驱动已含ACR122U支持);
                }
            }
            catch (Exception ex) { logAppend(Resources._运行出错 + " " + ex.Message); }
            lprocess = false; running = false;
            Text = Resources.MifareOne_Tool_运行完毕;
            logAppend(Resources._运行完毕);
        }

        // 比较两个文件内容是否一致（先比长度，再比字节），用于判断当前生效的 libnfc 驱动变体
        private bool SameFileContent(string pathA, string pathB)
        {
            try
            {
                FileInfo fa = new FileInfo(pathA);
                FileInfo fb = new FileInfo(pathB);
                if (!fa.Exists || !fb.Exists || fa.Length != fb.Length) { return false; }
                byte[] a = File.ReadAllBytes(pathA);
                byte[] b = File.ReadAllBytes(pathB);
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i]) { return false; }
                }
                return true;
            }
            catch (Exception) { return false; }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (running)
            {
                statusLabel.Text = Resources.运行中;
                if (process.HasExited == false)
                {
                    DateTime now = DateTime.Now;
                    TimeSpan runtime = now - process.StartTime;
                    runTimeLabel.Text = Resources.运行时间 + ((int)runtime.TotalSeconds).ToString() + Resources.秒;
                }
            }
            else { statusLabel.Text = Resources.空闲; }
        }

        private void buttonCheckEncrypt_Click(object sender, EventArgs e)
        {//其实这个mfdetect就是个mfoc阉割版。。只检测不破解而已，所以-f -k什么的可以加上，测试自己的key
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            string key = "";
            if (Control.ModifierKeys == Keys.Control)
            {
                string[] ks = Interaction.InputBox(Resources.请输入已知的Key_以英文半角逗号分隔, Resources.请输入已知Key, Properties.Settings.Default.LastTryKey, -1, -1).Trim().Split(',');
                if (ks.Length > 0)
                {
                    Properties.Settings.Default.LastTryKey = string.Join(",", ks);
                    foreach (string k in ks)
                    {
                        string pat = "[0-9A-Fa-f]{12}";
                        if (Regex.IsMatch(k, pat))
                        {
                            key += "-k " + k.Substring(0, 12) + " ";
                        }
                    }
                }
            }
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(MfDetect);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync(key);
        }

        void MfDetect(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/mfdetect.exe");
            psi.Arguments = (string)(e.Argument) + "-O dummy.tmp";
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行检测卡片加密); running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            File.Delete("dummy.tmp");
            lprocess = false; running = false;
            if (process.ExitCode == 0)
            {
                b.ReportProgress(100, Resources._运行完毕);
            }
            else
            {
                b.ReportProgress(100, Resources._运行出错);
            }
        }

        private void buttonHexTool_Click(object sender, EventArgs e)
        {
            FormHTool fht = new FormHTool();
            UiTheme.Apply(fht);
            fht.Show();
        }

        private void buttonECheckEncrypt_Click(object sender, EventArgs e)
        {
            buttonCheckEncrypt_Click(sender, e);
        }

        private void toolStripCheckUpdate_ButtonClick(object sender, EventArgs e)
        {
            Process.Start("https://github.com/xcicode/MifareOneTool/releases/latest");
        }

        private void buttonDiffTool_Click(object sender, EventArgs e)
        {
            FormDiff df = new FormDiff();
            UiTheme.Apply(df);
            df.Show();
        }

        private void buttonnKeysMfoc_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            string rmfd = "Mfoc.tmp";
            string key = "";
            string[] ks = Interaction.InputBox(Resources.请输入已知的Key_以英文半角逗号分隔, Resources.请输入已知Key, Properties.Settings.Default.LastTryKey, -1, -1).Trim().Split(',');
            if (ks.Length > 0)
            {
                Properties.Settings.Default.LastTryKey = string.Join(",", ks);
                foreach (string k in ks)
                {
                    string pat = "[0-9A-Fa-f]{12}";
                    if (Regex.IsMatch(k, pat))
                    {
                        key += "-k " + k.Substring(0, 12) + " ";
                    }
                }
            }
            if (checkBoxAutoSave.Checked)
            {
                lastuid = GetUID();
            }
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(mfoc);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync(new string[] { rmfd, key });
            omfd = rmfd;
        }

        private void buttonDictMfoc_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            string cmd_mode = "/k";
            //if (Control.ModifierKeys == Keys.Control)
            //{
            //    cmd_mode="/k";
            //}
            string filename = "";
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.CheckFileExists = true;
            ofd.Filter = Resources.密钥字典文件_dic;
            ofd.Title = Resources.请选择需要打开的密钥字典文件;
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                filename = ofd.FileName;
            }
            else
            {
                Text = Resources.MifareOne_Tool_已取消;
                return;
            }
            string rmfd = "Mfoc.tmp";
            string key = "-f \"" + filename + "\" ";
            if (checkBoxAutoSave.Checked)
            {
                lastuid = GetUID();
            }
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(mfocCMD);
            bgw.WorkerReportsProgress = true;
            bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
            bgw.RunWorkerAsync(new string[] { rmfd, key, cmd_mode });
            omfd = rmfd;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowText")]
        public static extern int SetWindowText(IntPtr hwnd, string lpString);
        [DllImport("user32", SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        void mfocCMD(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/mfoc.exe");
            string[] args = (string[])e.Argument;
            psi.WorkingDirectory = "./";
            // 原实现经 cmd.exe 拼接命令（Issue #35/#42 风险点）：字典文件名带引号/空格时易出错，
            // 且存在命令注入面。这里改为直接调用 mfoc.exe，输出重定向到界面终端区。
            psi.Arguments = args[1] + "-O \"" + args[0] + "\"";
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行字典模式MFOC解密); running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            process.WaitForExit();
            lprocess = false; running = false;
            if (process.ExitCode == 0)    // 直调 mfoc 后以其自身退出码为准（原判断的 -1073741510 是 cmd.exe 的返回码）
            {
                b.ReportProgress(101, Resources._运行完毕);
            }
            else
            {
                b.ReportProgress(100, Resources._运行出错);
                File.Delete(args[0]);
            }
        }

        private void checkBoxAutoABN_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AutoABN = checkBoxAutoABN.Checked;
        }

        private void buttonEUIDWrite_Click(object sender, EventArgs e)
        {
            buttonBmfWrite_Click(sender, e);
        }

        private void buttonEReadUID_Click(object sender, EventArgs e)
        {
            buttonBmfRead_Click(sender, e);
        }

        private void checkBoxWriteProtect_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.WriteCheck = checkBoxWriteProtect.Checked;
        }

        private void buttonHardNested_Click(object sender, EventArgs e)
        {
            if (lprocess) { MessageBox.Show(Resources.有任务运行中_不可执行, Resources.设备忙, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } this.Text = Resources.MifareOne_Tool_运行中;
            FormHardNes fhn = new FormHardNes();
            UiTheme.Apply(fhn);
            if (fhn.ShowDialog() == DialogResult.Yes)
            {
                string hardargs = fhn.GetArg();
                BackgroundWorker bgw = new BackgroundWorker();
                if (fhn.collectOnly())
                {
                    //lastuid = "0x" + GetUID() + fhn.GetFileAfter();
                    bgw.DoWork += new DoWorkEventHandler(CollectNonce);
                }
                else
                {
                    bgw.DoWork += new DoWorkEventHandler(Hardnest);
                }
                bgw.WorkerReportsProgress = true;
                bgw.ProgressChanged += new ProgressChangedEventHandler(default_rpt);
                bgw.RunWorkerAsync(hardargs);
            }
            else
            {
                Text = Resources.MifareOne_Tool_已取消;
            }
        }
        void Hardnest(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo("nfc-bin/libnfc_hardnested.exe");
            if (Properties.Settings.Default.HardLowCost)
            {
                psi.FileName = "nfc-bin/libnfc_hardnestedlc.exe";
            }
            psi.Arguments = (string)e.Argument;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行HardNested解密强化卡); running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            lprocess = false; running = false;
            if (process.ExitCode == 0)
            {
                b.ReportProgress(100, Resources._运行完毕);
            }
            else
            {
                b.ReportProgress(100, Resources._运行出错);
            }
        }

        void CollectNonce(object sender, DoWorkEventArgs e)
        {
            if (lprocess) { return; }
            ProcessStartInfo psi = new ProcessStartInfo(Resources.nfc_bin_collect_exe);
            psi.Arguments = (string)e.Argument;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            lprocess = true;
            BackgroundWorker b = (BackgroundWorker)sender;
            if (!ToolProcess.Start(psi, out process))
            {
                lprocess = false;
                running = false;
                b.ReportProgress(100, Resources._运行出错);
                logAppend(Resources.无法找到MFF08程序文件_操作终止);
                return;
            }
            b.ReportProgress(0, Resources.开始执行HardNested收集数据); running = true;
            ToolProcess.PumpOutput(process, b);
            process.WaitForExit();
            lprocess = false; running = false;
            if (process.ExitCode == 0)
            {
                b.ReportProgress(102, Resources._运行完毕);
            }
            else
            {
                b.ReportProgress(100, Resources._运行出错);
            }
        }

        private void checkBoxAutoLoadKey_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AutoLoadUidKey = checkBoxAutoLoadKey.Checked;
        }

        private void buttonCLIColor_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            cd.AllowFullOpen = true;
            cd.AnyColor = true;
            if (cd.ShowDialog() == DialogResult.OK)
            {
                richTextBox1.ForeColor = cd.Color;
                buttonCLIColor.ForeColor = cd.Color;
                Properties.Settings.Default.MainCLIColor = cd.Color;
            }
        }

        /// <summary>终端默认字号（磅）：比界面基准 9pt 大两号。</summary>
        private const float DefaultTerminalFontPt = 12F;

        /// <summary>已应用的终端字号（磅），用于避免同一次字号被重复建字体。</summary>
        private float _terminalFontPt = -1F;

        /// <summary>
        /// 启动时按设置应用终端字体。
        /// 旧版本的 MainCLIFontSize 只在设置页改值时使用、启动时从未生效，
        /// 因此这里做一次性迁移：老配置统一提升到新的默认字号。
        /// </summary>
        private void ApplyTerminalFontSettings()
        {
            if (Properties.Settings.Default.TerminalFontVersion < 1)
            {
                Properties.Settings.Default.MainCLIFontSize = DefaultTerminalFontPt;
                Properties.Settings.Default.TerminalFontVersion = 1;
            }
            float pt = Properties.Settings.Default.MainCLIFontSize;
            if (pt < (float)numericCLIFontSize.Minimum || pt > (float)numericCLIFontSize.Maximum)
            {
                pt = DefaultTerminalFontPt;
            }
            // 同步到设置页：值没变时不会触发 ValueChanged，所以下面再显式应用一次
            numericCLIFontSize.Value = (decimal)pt;
            ApplyTerminalFont(pt);
        }

        /// <summary>Ctrl+滚轮：以「磅」为单位步进，并夹在设置页允许的范围内。</summary>
        private void StepTerminalFont(int step)
        {
            decimal v = numericCLIFontSize.Value + step;
            if (v < numericCLIFontSize.Minimum) { v = numericCLIFontSize.Minimum; }
            if (v > numericCLIFontSize.Maximum) { v = numericCLIFontSize.Maximum; }
            if (v != numericCLIFontSize.Value) { numericCLIFontSize.Value = v; }
        }

        /// <summary>
        /// 应用终端字体：固定微软雅黑（Consolas 无中文字形，中文靠回退渲染、
        /// 中英混排时高度不一致），字号按像素建字体，保证任何系统缩放下 1:1。
        /// </summary>
        private void ApplyTerminalFont(float pt)
        {
            if (Math.Abs(_terminalFontPt - pt) < 0.001F) { return; }
            _terminalFontPt = pt;
            float px = pt * 96F / 72F;
            richTextBox1.Font = new Font(UiTheme.UiFont.FontFamily, px, FontStyle.Regular, GraphicsUnit.Pixel);
            // 缩放统一走字号：ZoomFactor 叠加在字号上会让「文字大小」与实际显示对不上
            richTextBox1.ZoomFactor = 1F;
        }

        private void numericCLIFontSize_ValueChanged(object sender, EventArgs e)
        {
            // 设置项沿用「磅」的用户语义（默认 10.0），但按**像素**建字体（见 ApplyTerminalFont）
            float pt = (float)numericCLIFontSize.Value;
            ApplyTerminalFont(pt);
            Properties.Settings.Default.MainCLIFontSize = pt;
        }

        private void checkBoxDefIsAdv_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.DefIsAdv = checkBoxDefIsAdv.Checked;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void checkBoxAutoSave_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AutoSave = checkBoxAutoSave.Checked;
        }

        private void checkBoxHardLowCost_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.HardLowCost = checkBoxHardLowCost.Checked;
        }

        private void tabPage3_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.NewScan = checkBoxNewScan.Checked;
        }

        private void buttonEStop_Click(object sender, EventArgs e)
        {
            buttonKill_Click(sender, e);
        }

        private void SetDeviceCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SetDeviceCombo.SelectedIndex >= 0)
            {
                writeConfig(SetDeviceCombo.SelectedItem.ToString());
                logAppend(Resources.已指定使用该NFC设备 + SetDeviceCombo.SelectedItem.ToString());
            }
        }

        private void checkBoxMultiDev_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.MultiMode = checkBoxMultiDev.Checked;
        }

        private void checkBoxCuidKeyOver_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.CuidKeyOver = checkBoxCuidKeyOver.Checked;
        }

        private void buttonMFF08_Click(object sender, EventArgs e)
        {
            FormMFF08 mff08 = new FormMFF08();
            UiTheme.Apply(mff08);
            mff08.ShowDialog();
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 构造期回显（ApplyLanguageSelection）也会走到这里，必须屏蔽，
            // 否则一启动就「保存设置 + 重启」，形成死循环。
            if (suppressLanguageChanged) { return; }
            // 按索引切换语言（0=中文 1=俄语 2=英语），
            // 不再用文案比对匹配——各语言资源的下拉文案不同，索引式对新增语言更稳
            string culture;
            switch (comboBox1.SelectedIndex)
            {
                case 1:
                    culture = "ru-RU";
                    break;
                case 2:
                    culture = "en-US";
                    break;
                case 0:
                    culture = "zh-cn";
                    break;
                default:
                    return;
            }
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            Properties.Settings.Default.Language = culture;
            Properties.Settings.Default.Save();
            Application.Restart();
        }
    }
}
