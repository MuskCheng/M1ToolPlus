using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace M1ToolPlus
{
    /// <summary>
    /// 用户缩放支持（Form1 partial）。
    ///
    /// 背景（实测定位）：本程序是 x86 + DPI-unaware，在 125% 缩放的屏幕上
    /// 会被系统做 GDI 位图缩放，窗口边框被撑成 16px（32 位下 GetWindowRect
    /// 与 GetClientRect 差 16px），但系统的非客户区命中测试仍按未缩放的边框
    /// 计算——于是窗口最外侧 8px 被判为 HTBORDER（不可缩放的普通边框），
    /// 真正可缩放的 HTRIGHT 区域被挤到内侧 10~16px，而那一段已属客户区、
    /// 被终端框等子控件抢走。结果就是：按住窗口最外沿拖拽毫无反应。
    /// （同样测试下 64 位标准窗体返回 HTRIGHT，可见这是位深/DPI 组合引起的。）
    ///
    /// 对策：在窗体层面接管 WM_NCHITTEST——
    ///   · 收到 HTBORDER 时，按命中点相对四条边的位置翻译成对应的缩放码
    ///     （HTRIGHT/HTLEFT/HTTOP/HTBOTTOM 及四个角），把整条边框变成可拖拽区；
    ///   · 客户区最外圈再补 8px 热区（子控件覆盖处不会走到本方法，行为不变）。
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>客户区缩放热区宽度（逻辑像素）。</summary>
        private const int ResizeZonePx = 8;

        // HT* 命中码
        private const int HTCLIENT = 0x0001;
        private const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13,
                  HTTOPRIGHT = 14, HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17,
                  HTBORDER = 18;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll", EntryPoint = "GetWindowRect")] private static extern bool NativeGetWindowRect(IntPtr h, out NativeRect r);
        [DllImport("user32.dll", EntryPoint = "GetClientRect")] private static extern bool NativeGetClientRect(IntPtr h, out NativeRect r);

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;

            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                int hit = (int)(long)m.Result;

                if (hit == HTBORDER)
                {
                    // 非客户区边框：系统判为「普通边框」，这里按位置翻译成缩放码。
                    // 最大化窗口不参与：最大化态由系统接管边框行为。
                    if (WindowState == FormWindowState.Maximized) { return; }
                    int code = EdgeCodeOf(m.LParam, true);
                    if (code != HTCLIENT) { m.Result = (IntPtr)code; }
                    return;
                }

                if (hit == HTCLIENT && WindowState != FormWindowState.Maximized)
                {
                    // 客户区最外圈：额外提供一圈缩放热区。
                    int code = EdgeCodeOf(m.LParam, false);
                    if (code != HTCLIENT) { m.Result = (IntPtr)code; }
                    return;
                }
                return;
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// 按命中点相对窗口四条边的位置计算缩放命中码。
        /// </summary>
        /// <param name="lParam">WM_NCHITTEST 的屏幕坐标（本进程虚拟坐标）。</param>
        /// <param name="useFrame">true=按窗口边框厚度判定（非客户区）；false=按客户区热区宽度判定。</param>
        private int EdgeCodeOf(IntPtr lParam, bool useFrame)
        {
            try
            {
                long lp = lParam.ToInt64();
                int sx = (int)(short)(lp & 0xFFFF);
                int sy = (int)(short)((lp >> 16) & 0xFFFF);

                NativeRect win;
                if (!NativeGetWindowRect(Handle, out win)) { return HTCLIENT; }

                int left = sx - win.Left;
                int top = sy - win.Top;
                int right = win.Right - sx;
                int bottom = win.Bottom - sy;
                if (left < 0 || top < 0 || right < 0 || bottom < 0) { return HTCLIENT; }

                int fw;
                if (useFrame)
                {
                    // 边框厚度 = (窗口尺寸 - 客户区尺寸) / 2（左右边等宽）
                    NativeRect client;
                    fw = 8;
                    if (NativeGetClientRect(Handle, out client))
                    {
                        int dx = ((win.Right - win.Left) - (client.Right - client.Left)) / 2;
                        if (dx > 0) { fw = dx; }
                    }
                }
                else
                {
                    fw = ResizeZonePx;
                }

                // 四角优先
                if (left <= fw && top <= fw) { return HTTOPLEFT; }
                if (right <= fw && top <= fw) { return HTTOPRIGHT; }
                if (left <= fw && bottom <= fw) { return HTBOTTOMLEFT; }
                if (right <= fw && bottom <= fw) { return HTBOTTOMRIGHT; }
                if (left <= fw) { return HTLEFT; }
                if (right <= fw) { return HTRIGHT; }
                if (top <= fw) { return HTTOP; }
                if (bottom <= fw) { return HTBOTTOM; }
                return HTCLIENT;
            }
            catch (Exception) { return HTCLIENT; }
        }
    }
}
