using System;
using System.Windows.Forms;

namespace M1ToolPlus
{
    /// <summary>
    /// 主窗口终端输出控件。
    ///
    /// 为什么不用 RichTextBox 自带的 Ctrl+滚轮缩放：它改的是 ZoomFactor（显示放大比例），
    /// 字号 Font 本身不变——设置页的「文字大小」既不会跟着变，也无法反映真实字号；
    /// 而且 ZoomFactor 会叠加在字号上，之后再改设置值，实际大小就对不上了。
    ///
    /// 这里直接把 Ctrl+滚轮翻译成「字号步进」：由 Form1 改 numericCLIFontSize.Value，
    /// 统一走 ValueChanged → 改字体 + 存设置，保证「设置里显示多少，终端就是多少」。
    /// </summary>
    internal sealed class TerminalBox : RichTextBox
    {
        private const int WM_MOUSEWHEEL = 0x020A;

        /// <summary>Ctrl+滚轮：+1 放大一号，-1 缩小一号（单位与设置页一致：磅）。</summary>
        public event Action<int> ZoomStep;

        protected override void WndProc(ref Message m)
        {
            // 用消息自带的 MK_CONTROL（LOWORD 的 0x0008 位）判断，
            // 而不是 ModifierKeys：后者读的是当前键盘状态，
            // 触摸板/自动化发送的滚轮消息可能没有实际按键按下。
            bool ctrl = ((long)m.WParam & 0x0008L) != 0;
            if (m.Msg == WM_MOUSEWHEEL && ctrl)
            {
                int delta = (short)((long)m.WParam >> 16);
                if (delta != 0 && ZoomStep != null) { ZoomStep(delta > 0 ? 1 : -1); }
                return; // 吃掉消息：不让 RichEdit 再改 ZoomFactor，避免双重缩放
            }
            base.WndProc(ref m);
        }
    }
}
