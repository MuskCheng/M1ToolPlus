using System;
using System.ComponentModel;
using System.Diagnostics;
using M1ToolPlus.Properties;

namespace M1ToolPlus
{
    /// <summary>
    /// 读写卡流程的宿主能力：把窗体自己的忙碌标志、终端输出暴露给执行骨架，
    /// 让「启动工具 → 泵输出 → 等待 → 复位标志」这段流程可以脱离具体窗体复用。
    /// Form1 以显式实现接上（lprocess / running / process / logAppend）。
    /// </summary>
    internal interface IToolHost
    {
        /// <summary>是否有工具正在运行（对应 Form1.lprocess，用于按钮互斥）。</summary>
        void SetBusy(bool busy);

        /// <summary>是否处于「运行中」状态（对应 Form1.running，用于状态显示）。</summary>
        void SetRunning(bool isRunning);

        /// <summary>往终端写一行（缺文件、错误等无法走进度通道的信息）。</summary>
        void Log(string line);

        /// <summary>当前子进程（对应 Form1.process，供「停运行」终止）。</summary>
        Process CurrentProcess { get; set; }
    }

    /// <summary>
    /// 读写卡流程的公共骨架（v1.8.0 从 Form1 抽出）。
    ///
    /// 此前 nfc-mfclassic / mfoc / mfsetuid 等 18 处 DoWork 处理器各自复制同一段
    /// 「建 ProcessStartInfo → 启动 → 泵输出 → 等待 → 翻转忙碌标志」的代码，
    /// 漏改一处就会出现标志没复位、缺文件不提示之类的不一致。先抽取读/写 M1
    /// 这条主干（其余流程后续按同一接口迁移），参数拼装同时变成可单测的纯函数。
    /// </summary>
    internal static class CardToolFlow
    {
        /// <summary>
        /// 构建无窗口、重定向 stdout/stderr 的工具进程参数（所有 nfc-bin 工具通用）。
        /// </summary>
        public static ProcessStartInfo BuildPsi(string exe, string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo(exe);
            psi.Arguments = arguments;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            return psi;
        }

        /// <summary>
        /// nfc-mfclassic 的参数拼装（读 r / 写 w / 清 d 共用一套格式）：
        ///   &lt;mode&gt; &lt;A|B|C&gt; u "&lt;dump&gt;"[ "&lt;key.mfd&gt;" f]
        /// keyFile 为空、或 noKey（用户在对话框里选了「不使用 Key」）时不附加密钥段。
        /// </summary>
        public static string BuildClassicArgs(string mode, string keyMode, string dumpFile, string keyFile, bool noKey)
        {
            string args = mode + " " + keyMode + " u \"" + dumpFile + "\"";
            if (!string.IsNullOrEmpty(keyFile) && !noKey)
            {
                args += " \"" + keyFile + "\" f";
            }
            return args;
        }

        /// <summary>
        /// 执行骨架：置忙碌 → 启动（失败按通用文案汇报并返回 -1）→ 汇报开始 →
        /// 泵输出到终端 → 等待退出 → 复位忙碌标志 → 返回退出码。
        /// 进度汇报（ReportProgress）直接走 worker，不需要宿主参与。
        /// </summary>
        public static int Run(IToolHost host, BackgroundWorker worker, ProcessStartInfo psi, string startMessage)
        {
            if (host == null) { throw new ArgumentNullException("host"); }
            if (worker == null) { throw new ArgumentNullException("worker"); }

            host.SetBusy(true);
            Process p;
            if (!ToolProcess.Start(psi, out p))
            {
                host.SetBusy(false);
                host.SetRunning(false);
                worker.ReportProgress(100, Resources._运行出错);
                host.Log(string.Format(Resources.无法找到程序文件_操作终止, psi.FileName));
                return -1;
            }
            host.CurrentProcess = p;
            worker.ReportProgress(0, startMessage);
            host.SetRunning(true);
            ToolProcess.PumpOutput(p, worker);
            p.WaitForExit();
            host.SetBusy(false);
            host.SetRunning(false);
            return p.ExitCode;
        }
    }
}
