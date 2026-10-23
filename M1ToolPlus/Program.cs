using M1ToolPlus.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;

namespace M1ToolPlus
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        private static readonly IntPtr PerMonitorAwareV2 = new IntPtr(-4);

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // UI 按物理像素 1:1 渲染。优先声明 Per-Monitor V2 DPI 感知（Win10 1703+）：
            // 任何缩放比例（100%/125%/150%）、任何多显示器组合下，系统都不会再把窗口
            // 做 GDI 位图拉伸——文字和界面永远按原生像素锐利渲染，这是「界面发虚」
            // 的唯一几何根源。旧系统（Win10 1703 之前）回退到系统级 SetProcessDPIAware。
            // 配合 UiTheme 的「像素字号」（见 UiFontPx），界面在任何缩放比例下
            // 都渲染成同样的像素尺寸（1:1）。
            try
            {
                if (!SetProcessDpiAwarenessContext(PerMonitorAwareV2))
                {
                    try { SetProcessDPIAware(); }
                    catch (Exception) { }
                }
            }
            catch (Exception)
            {
                try { SetProcessDPIAware(); }
                catch (Exception) { }
            }

            if (Properties.Settings.Default.MultiMode)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            else
            {
                bool ret;
                System.Threading.Mutex mutex = new System.Threading.Mutex(true, Application.ProductName, out ret);
                if (ret)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Form1());
                    mutex.ReleaseMutex();
                }
                else
                {
                    if (MessageBox.Show(Resources.您已经运行了MifareOne_Tool_打开多个本程序可能, Resources.您正在试图重复运行, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        Application.Run(new Form1());
                        mutex.ReleaseMutex();
                    }
                    else
                    {
                        Application.Exit();
                    }
                }
            }
        }
    }
}
