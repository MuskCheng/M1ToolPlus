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

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // UI 按物理像素 1:1 渲染。
            // 不声明 DPI 感知时：125%/150% 缩放下系统会把整个窗口做 GDI 位图拉伸——
            // 界面发虚，且窗口被整体放大 25%/50%（1080p@125% 下几乎占满屏幕）。
            // 声明后系统不再拉伸；配合 UiTheme 的「像素字号」（见 UiFontPx），
            // 界面在任何缩放比例下都渲染成同样的像素尺寸（1:1）。
            try { SetProcessDPIAware(); }
            catch (Exception) { }

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
