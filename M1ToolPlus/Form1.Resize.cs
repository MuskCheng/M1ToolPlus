using System;
using System.Drawing;
using System.Windows.Forms;

namespace M1ToolPlus
{
    /// <summary>
    /// 用户缩放支持（Form1 partial）。
    /// 背景：非客户区边框在部分桌面环境下会被置顶覆盖窗口挡住，且原生 8px 边框
    /// 本身偏窄难抓。这里把窗体自身表面（客户区）最外圈 8px 映射为缩放命中区，
    /// 光标移入即显示对应方向的缩放箭头，按下即可拖拽——与原生边框双保险。
    /// 只影响落在窗体自身表面上的命中；子控件（页签、终端等）覆盖处的消息
    /// 不会进入本方法，行为不变。
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>客户区缩放热区宽度（逻辑像素）。</summary>
        private const int ResizeZonePx = 8;

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTCLIENT = 0x0001;
            const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13,
                      HTTOPRIGHT = 14, HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                if ((int)(long)m.Result == HTCLIENT && WindowState != FormWindowState.Maximized)
                {
                    try
                    {
                        // lParam = 屏幕坐标（本进程虚拟坐标，DPI-unaware 与逻辑像素一致）
                        long lp = m.LParam.ToInt64();
                        int sx = (int)(short)(lp & 0xFFFF);
                        int sy = (int)(short)((lp >> 16) & 0xFFFF);
                        Point cp = PointToClient(new Point(sx, sy));
                        Rectangle cr = ClientRectangle;
                        if (cr.Width <= 0 || cr.Height <= 0) { base.WndProc(ref m); return; }

                        bool left = cp.X <= ResizeZonePx;
                        bool right = cp.X >= cr.Width - 1 - ResizeZonePx;
                        bool top = cp.Y <= ResizeZonePx;
                        bool bottom = cp.Y >= cr.Height - 1 - ResizeZonePx;

                        int code = HTCLIENT;
                        if (left && top) { code = HTTOPLEFT; }
                        else if (right && top) { code = HTTOPRIGHT; }
                        else if (left && bottom) { code = HTBOTTOMLEFT; }
                        else if (right && bottom) { code = HTBOTTOMRIGHT; }
                        else if (left) { code = HTLEFT; }
                        else if (right) { code = HTRIGHT; }
                        else if (top) { code = HTTOP; }
                        else if (bottom) { code = HTBOTTOM; }

                        if (code != HTCLIENT)
                        {
                            m.Result = (IntPtr)code;
                            return;
                        }
                    }
                    catch (Exception) { }
                }
                return;
            }

            base.WndProc(ref m);
        }
    }
}
