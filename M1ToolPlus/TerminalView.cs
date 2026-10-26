using System;
using System.Windows.Forms;

namespace M1ToolPlus
{
    /// <summary>
    /// 终端输出视图：把「正式日志行」与「临时进度行」的渲染从各窗体里抽出来。
    ///
    /// 为什么抽：Form1 原本有一份带进度行的实现，FormHTool / FormDiff / FormMFF08
    /// 又各自复制了一份简化版（四处重复、行为还不完全一致）。终端是只读 RichTextBox，
    /// 删中间文本必须临时关掉 ReadOnly 否则被静默忽略——这种细节过去只在 Form1
    /// 里正确实现过一次，别处的副本都删不掉进度行。
    ///
    /// 语义与原 Form1 实现逐行对应：
    ///   Append = 若有临时进度行先删掉，再追加正式行并滚到底；
    ///   Live   = 把文本原地重写为临时进度行（nfc-mfclassic 用 printf(".") 逐块
    ///            输出且不换行，重写到同一行上就成了逐格增长的进度条）；
    ///   Clear / Replace = 清屏/整体替换，并复位进度行状态。
    /// </summary>
    internal sealed class TerminalView
    {
        private readonly RichTextBox _box;

        /// <summary>临时进度行的起始位置；-1 表示没有。</summary>
        private int _liveStart = -1;

        public TerminalView(RichTextBox box)
        {
            if (box == null) { throw new ArgumentNullException("box"); }
            _box = box;
        }

        /// <summary>底层控件（主题化、只读设置等仍直接操作控件本身）。</summary>
        public RichTextBox Box { get { return _box; } }

        /// <summary>追加一行正式日志（先清掉可能残留的临时进度行）。</summary>
        public void Append(string msg)
        {
            if (msg == null) { msg = ""; }
            if (_liveStart >= 0)
            {
                // 正式日志到达：先删掉临时进度行，避免留下半截进度条。
                // 注意：只读 RichTextBox 的 SelectedText = "" 会被系统静默忽略
                // （插入/替换却有效），必须临时关掉只读再删。
                int start = Math.Min(_liveStart, _box.TextLength);
                if (start < _box.TextLength)
                {
                    bool readOnly = _box.ReadOnly;
                    _box.ReadOnly = false;
                    try
                    {
                        _box.Select(start, _box.TextLength - start);
                        _box.SelectedText = "";
                    }
                    finally
                    {
                        _box.ReadOnly = readOnly;
                    }
                }
                _liveStart = -1;
            }
            _box.AppendText(msg + "\n");
            _box.SelectionStart = _box.TextLength;
            _box.ScrollToCaret();
        }

        /// <summary>原地刷新临时进度行（无换行的进度输出）。</summary>
        public void Live(string msg)
        {
            if (msg == null) { return; }
            if (_liveStart < 0 || _liveStart > _box.TextLength)
            {
                _liveStart = _box.TextLength;
            }
            _box.Select(_liveStart, _box.TextLength - _liveStart);
            _box.SelectedText = msg;
            _box.SelectionStart = _box.TextLength;
            _box.ScrollToCaret();
        }

        /// <summary>清屏并复位进度行状态。</summary>
        public void Clear()
        {
            _box.Clear();
            _liveStart = -1;
        }

        /// <summary>整体替换文本（差异比较、Hex 编辑器等整屏输出场景）。</summary>
        public void Replace(string text)
        {
            _box.Clear();
            _box.Text = text ?? "";
            _liveStart = -1;
        }
    }
}
