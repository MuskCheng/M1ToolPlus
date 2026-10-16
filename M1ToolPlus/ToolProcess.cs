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
    /// 2) 以 UTF-8 显式解码子进程输出。libnfc 系工具输出为 UTF-8，而 .NET Framework 4.0
    ///    没有 ProcessStartInfo.StandardOutputEncoding（4.5+ 才有），默认按系统 ANSI
    ///    代码页解码，在中文 Windows 上会把 libnfc 的中文提示显示成乱码。
    /// </summary>
    internal static class ToolProcess
    {
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
        /// 按 UTF-8 逐行读取子进程输出。stdout 交给 lineHandler 处理（为 null 时直接输出到终端区），
        /// stderr 始终输出到终端区。读取在独立线程上进行，不阻塞调用方。
        /// </summary>
        public static void PumpOutput(Process process, System.ComponentModel.BackgroundWorker worker, Action<string> lineHandler)
        {
            if (process == null || worker == null) { return; }
            try
            {
                if (process.StartInfo.RedirectStandardOutput)
                {
                    Pump(process.StandardOutput.BaseStream, worker, lineHandler);
                }
                if (process.StartInfo.RedirectStandardError)
                {
                    Pump(process.StandardError.BaseStream, worker, null);
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

        private static void Pump(Stream baseStream, System.ComponentModel.BackgroundWorker worker, Action<string> lineHandler)
        {
            if (baseStream == null) { return; }
            // 每个流各占一个线程池线程，避免只读单流时的管道写满死锁
            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                Stream s = (Stream)state;
                try
                {
                    using (StreamReader reader = new StreamReader(s, Encoding.UTF8))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (lineHandler != null) { lineHandler(line); }
                            else { worker.ReportProgress(0, line); }
                        }
                    }
                }
                catch (Exception)
                {
                    // 进程被强杀时读流会抛异常，忽略即可
                }
            }, baseStream);
        }
    }
}