using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace M1ToolPlus
{
    /// <summary>
    /// 统一封装 nfc-bin 子进程的启动与输出读取。
    /// 两点改进（对应 Issue #28/#34/#35 的终端乱码与崩溃风险）：
    /// 1) 启动前检查目标文件是否存在，并捕获启动异常，避免 nfc-bin 不完整时直接崩溃；
    /// 2) 用 GBK(936) 显式解码子进程输出——随包的 libnfc 系工具是中文定制版，
    ///    提示文字按系统 ANSI 代码页（简体中文 Windows 为 936）写成字节，
    ///    例："使用libnfc" = CA B9 D3 C3、"未发现卡" = CE B4 B7 A2 CF D6 BF A8。
    ///    在英文/俄文 Windows 上默认 ANSI 解码会把这些字节解成乱码；
    ///    而按 UTF-8 解码同样错（CA B9 恰好是合法的 UTF-8 两字节序列 → "ʹ"）。
    ///    固定用 936 解码才能在任何系统区域设置下都得到正确中文。
    /// </summary>
    internal static class ToolProcess
    {
        private static Encoding _consoleEncoding;

        /// <summary>
        /// nfc-bin 工具输出使用的编码：GBK(936)。
        /// 取不到 936 时退回系统 ANSI（Encoding.Default），保证不因编码初始化失败而崩溃。
        /// </summary>
        internal static Encoding ConsoleEncoding
        {
            get
            {
                if (_consoleEncoding == null)
                {
                    try { _consoleEncoding = Encoding.GetEncoding(936); }
                    catch (Exception) { _consoleEncoding = Encoding.Default; }
                }
                return _consoleEncoding;
            }
        }

        /// <summary>启动子进程；目标文件缺失或启动失败时返回 false。</summary>
        public static bool Start(ProcessStartInfo psi, out Process process)
        {
            process = null;
            try
            {
                if (psi == null || string.IsNullOrEmpty(psi.FileName) || !File.Exists(psi.FileName))
                {
                    return false;
                }
                process = Process.Start(psi);
                return process != null;
            }
            catch (Exception)
            {
                process = null;
                return false;
            }
        }

        /// <summary>
        /// 未换行的临时输出（进度条）以该 ProgressPercentage 上报。
        /// Form1.default_rpt 据此在终端最后一行原地刷新；收到完整行（0）时再定稿。
        /// </summary>
        internal const int ProgressLiveLine = -1;

        /// <summary>
        /// 按 GBK(936) 读取子进程输出。stdout 交给 lineHandler 处理（为 null 时直接输出到终端区），
        /// stderr 始终输出到终端区。读取在独立线程上进行，不阻塞调用方。
        ///
        /// 为什么不能再用 ReadLine：nfc-mfclassic 的写卡进度是「|」+ 每个块一个字符，
        /// 只在扇区边界 fflush，整条进度直到最后的 "|\n" 才有换行。ReadLine 会一直
        /// 等到进程结束才返回，于是写卡过程一条日志都没有、最后瞬间全部出现。
        /// 按字符读后：遇到 \n 定稿一行；没有换行的内容作为「临时行」上报，
        /// 由终端原地刷新，进度条就能边写边长。
        /// </summary>
        public static void PumpOutput(Process process, System.ComponentModel.BackgroundWorker worker, Action<string> lineHandler)
        {
            if (process == null || worker == null) { return; }
            try
            {
                if (process.StartInfo.RedirectStandardOutput)
                {
                    Pump(process.StandardOutput.BaseStream, worker, lineHandler, true);
                }
                if (process.StartInfo.RedirectStandardError)
                {
                    Pump(process.StandardError.BaseStream, worker, null, false);
                }
            }
            catch (Exception)
            {
                // 读取失败不影响主流程
            }
        }

        /// <summary>简化入口：不关心单行内容时使用。</summary>
        public static void PumpOutput(Process process, System.ComponentModel.BackgroundWorker worker)
        {
            PumpOutput(process, worker, null);
        }

        private static void Pump(Stream baseStream, System.ComponentModel.BackgroundWorker worker,
            Action<string> lineHandler, bool liveProgress)
        {
            if (baseStream == null) { return; }
            // 每个流各占一个线程池线程，避免只读单流时的管道写满死锁
            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                Stream s = (Stream)state;
                try
                {
                    using (StreamReader reader = new StreamReader(s, ConsoleEncoding))
                    {
                        char[] buf = new char[512];
                        StringBuilder cur = new StringBuilder();
                        DateTime lineStart = DateTime.UtcNow;
                        DateTime lastLive = DateTime.MinValue;
                        char prev = '\0';
                        int n;
                        while ((n = reader.Read(buf, 0, buf.Length)) > 0)
                        {
                            for (int i = 0; i < n; i++)
                            {
                                char ch = buf[i];

                                if (ch == '\r' && i + 1 < n && buf[i + 1] == '\n')
                                {
                                    // CRLF：跳过 \r，交给后面的 \n 收尾，避免多出一个空行
                                    continue;
                                }

                                if (ch == '\n')
                                {
                                    string line = cur.ToString();
                                    cur.Length = 0;
                                    Emit(worker, lineHandler, line);
                                    prev = ch;
                                    lineStart = DateTime.UtcNow;
                                    continue;
                                }

                                if (ch == '\r')
                                {
                                    // 可能是进度条原地重写（\r 后不是 \n），等下一个字符再判断
                                    prev = ch;
                                    continue;
                                }

                                if (prev == '\r')
                                {
                                    // 上一段以 \r 结尾：先把它作为临时行播出，再从新行开始累积
                                    if (liveProgress && cur.Length > 0)
                                    {
                                        worker.ReportProgress(ProgressLiveLine, cur.ToString());
                                    }
                                    cur.Length = 0;
                                    lineStart = DateTime.UtcNow;
                                    lastLive = DateTime.UtcNow;
                                }

                                cur.Append(ch);
                                prev = ch;

                                if (liveProgress && cur.Length > 0)
                                {
                                    DateTime now = DateTime.UtcNow;
                                    // 250ms 内读完的整行不刷临时行（避免普通日志闪烁）；
                                    // 进度条这类长时间不换行的输出约每 120ms 刷新一次。
                                    if ((now - lineStart).TotalMilliseconds >= 250
                                        && (now - lastLive).TotalMilliseconds >= 120)
                                    {
                                        worker.ReportProgress(ProgressLiveLine, cur.ToString());
                                        lastLive = now;
                                    }
                                }
                            }
                        }

                        // 流结束：把没有换行收尾的残余内容定稿，避免进度条丢失
                        if (cur.Length > 0) { Emit(worker, lineHandler, cur.ToString()); }
                    }
                }
                catch (Exception)
                {
                    // 进程被强杀时读流会抛异常，忽略即可
                }
            }, baseStream);
        }

        private static void Emit(System.ComponentModel.BackgroundWorker worker, Action<string> lineHandler, string line)
        {
            if (lineHandler != null) { lineHandler(line); }
            else { worker.ReportProgress(0, line); }
        }
    }
}
