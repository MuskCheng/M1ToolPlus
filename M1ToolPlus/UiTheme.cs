using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace M1ToolPlus
{
    /// <summary>
    /// 运行时 UI 主题（Fluent 风格化）。
    ///
    /// 定位说明：本项目界面是 VS2010 时代用绝对定位搭的（FixedSingle 窗体 + 固定坐标），
    /// 改 Designer/resx 布局会导致控件错位，因此**不改任何布局参数**，只在运行时统一外观。
    ///
    /// 风格参考 Windows 11 Fluent Design（Mica / 圆角 / Segoe UI），并按能力自适应降级：
    ///   · Win11 22H2+ 自动启用 Mica 背景与窗口圆角，标题栏随深浅色切换；
    ///   · Win10 或不支持时自动回退为纯色深/浅主题，功能与可读性不受影响；
    ///   · 主题跟随系统（HKCU\...\Themes\Personalize\AppsUseLightTheme），系统切换时即时响应，
    ///     也可在「界面设置」里手动指定。
    ///
    /// 注：Mica 需要窗口透明合成，与 WinForms 的不透明渲染存在冲突，因此只对顶层窗体尝试，
    ///     失败即静默回退，不影响任何功能。
    /// </summary>
    internal static class UiTheme
    {
        // ================= 配色（深色） =================
        internal static readonly Color DBack = Color.FromArgb(0x20, 0x20, 0x20);       // 窗口底色
        internal static readonly Color DSurface = Color.FromArgb(0x2B, 0x2B, 0x2B);    // 面板
        internal static readonly Color DCard = Color.FromArgb(0x33, 0x33, 0x33);       // 卡片/控件
        internal static readonly Color DCardHover = Color.FromArgb(0x3D, 0x3D, 0x3D);
        internal static readonly Color DBorder = Color.FromArgb(0x45, 0x45, 0x45);
        internal static readonly Color DText = Color.FromArgb(0xE6, 0xE6, 0xE6);
        internal static readonly Color DTextDim = Color.FromArgb(0xA6, 0xA6, 0xA6);
        private static readonly Color DangerAccent = Color.FromArgb(0xC5, 0x30, 0x2B);   // 停止/清卡等危险操作
        private static readonly Color DangerFill = Color.FromArgb(0x8E, 0x2A, 0x24);

        // ================= 配色（浅色） =================
        internal static readonly Color LBack = Color.White;
        internal static readonly Color LSurface = Color.FromArgb(0xF5, 0xF7, 0xFA);
        internal static readonly Color LCard = Color.White;
        internal static readonly Color LCardHover = Color.FromArgb(0xE8, 0xF0, 0xFB);
        internal static readonly Color LBorder = Color.FromArgb(0xE0, 0xE4, 0xE8);
        internal static readonly Color LText = Color.FromArgb(0x21, 0x21, 0x21);
        internal static readonly Color LTextDim = Color.FromArgb(0x61, 0x61, 0x61);

        // Windows 11 默认强调色（Segoe Fluent Blue），可被系统强调色覆盖
        private static readonly Color DefaultAccent = Color.FromArgb(0x00, 0x78, 0xD4);

        private static bool _dark;
        private static Color _accent = DefaultAccent;
        private static Font _font;
        private static Form _host;

        /// <summary>是否处于深色模式。</summary>
        internal static bool IsDark { get { return _dark; } }

        /// <summary>统一界面字体：Segoe UI 优先（与 Fluent 一致），中文回退雅黑。</summary>
        internal static Font UiFont
        {
            get
            {
                if (_font == null)
                {
                    string family = "Segoe UI";
                    try { using (Font probe = new Font(family, 8.25F)) { } _font = new Font(family, 8.25F, FontStyle.Regular, GraphicsUnit.Point); }
                    catch (Exception)
                    {
                        try { _font = new Font("Microsoft YaHei UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point); }
                        catch (Exception) { _font = SystemFonts.MessageBoxFont; }
                    }
                }
                return _font;
            }
        }

        // ================= 对外入口 =================

        /// <summary>应用主题（跟随系统深浅色）。窗体句柄需已创建才能设置 Mica/圆角。</summary>
        internal static void Apply(Form form)
        {
            if (form == null) { return; }
            _host = form;
            RefreshSystemTheme();
            ApplyPalette(form);
            WatchSystemTheme(form);
        }

        /// <summary>重新读取系统主题并刷新界面（系统切换深浅色或用户在设置里手动指定时调用）。</summary>
        internal static void Refresh()
        {
            if (_host == null || _host.IsDisposed) { return; }
            RefreshSystemTheme();
            ApplyPalette(_host);
        }

        private static void RefreshSystemTheme()
        {
            _dark = !SystemIsLight();
            Color sys = SystemAccent();
            _accent = sys.IsEmpty ? DefaultAccent : sys;
        }

        private static void ApplyPalette(Form form)
        {
            form.Font = UiFont;
            form.BackColor = _dark ? DBack : LBack;
            foreach (Control c in form.Controls) { Prepare(c); }
            StyleChildren(form);
            ApplyAdaptiveLayout(form);
            try { ApplyTitleBar(form); } catch (Exception) { }
        }

        /// <summary>
        /// 自适应布局修复。
        ///
        /// 原窗体是 FixedSingle（不可缩放）+ 绝对定位，内容宽度写死 726px。
        /// 一旦文案变长（如按钮按文本加宽），超出部分就会被裁掉——这是文字显示不完整的根因。
        /// 这里把窗体改为可缩放，并把三个顶层区域（状态栏 / 功能区 / 终端）改为 Dock 布局：
        ///   · 窗口放大时，终端区自动铺满剩余空间；
        ///   · 窗口缩小时，由 MinimumSize 保证功能区仍有足够宽度；
        ///   · 标签页内部的绝对定位保持不变（改动风险最小）。
        /// </summary>
        private static void ApplyAdaptiveLayout(Form form)
        {
            try
            {
                // 1) 允许缩放，并给出足够容纳功能区的最小尺寸
                if (form.FormBorderStyle == FormBorderStyle.FixedSingle)
                {
                    form.FormBorderStyle = FormBorderStyle.Sizable;
                }
                form.MaximizeBox = true;
                form.MinimumSize = new Size(900, 640);
                if (form.StartPosition == FormStartPosition.WindowsDefaultLocation)
                {
                    form.StartPosition = FormStartPosition.CenterScreen;
                }

                // 2) 顶层三区改为 Dock：状态栏贴底、功能区贴顶、终端区填满剩余
                foreach (Control c in form.Controls)
                {
                    if (c is StatusStrip) { c.Dock = DockStyle.Bottom; }
                    else if (c is TabControl) { c.Dock = DockStyle.Top; }
                    else if (c is RichTextBox) { c.Dock = DockStyle.Fill; }
                }

                TabControl tabs = FindTabControl(form);
                if (tabs != null)
                {
                    // 3) 宽度变化时重排功能区（TabControl 内的绝对定位控件默认不会重排）
                    tabs.Resize += delegate { LayoutFlowPanels(tabs); };
                    LayoutFlowPanels(tabs);

                    // 4) 功能区实测所需宽度：窗口初始宽度不足时自动扩展，
                    //    避免按钮加宽后把右侧控件（提示文字、停止按钮）挤出可视区。
                    int needed = MeasureRequiredWidth(tabs);
                    if (needed > tabs.Width)
                    {
                        form.ClientSize = new Size(needed + 12, form.ClientSize.Height);
                    }
                }
            }
            catch (Exception) { }
        }

        /// <summary>测量功能区实际需要的最宽一行（含绝对定位控件的右边缘）。</summary>
        private static int MeasureRequiredWidth(Control root)
        {
            int max = 0;
            try
            {
                foreach (Control c in root.Controls)
                {
                    int right = c.Left + c.Width;
                    if (right > max) { max = right; }
                    int nested = MeasureRequiredWidth(c);
                    if (nested > max) { max = nested; }
                }
            }
            catch (Exception) { }
            return max;
        }

        /// <summary>让所有流式容器在宽度变化后重新计算内边距与换行。</summary>
        private static void LayoutFlowPanels(Control root)
        {
            foreach (Control c in root.Controls)
            {
                FlowLayoutPanel flp = c as FlowLayoutPanel;
                if (flp != null)
                {
                    try
                    {
                        flp.PerformLayout();
                        foreach (Control child in flp.Controls)
                        {
                            FitButtonWidth(child as Button);
                            FitGroupWidth(child as GroupBox);
                        }
                    }
                    catch (Exception) { }
                }
                LayoutFlowPanels(c);
            }
        }

        private static TabControl FindTabControl(Control root)
        {
            foreach (Control c in root.Controls)
            {
                TabControl t = c as TabControl;
                if (t != null) { return t; }
                TabControl nested = FindTabControl(c);
                if (nested != null) { return nested; }
            }
            return null;
        }

        private static void WatchSystemTheme(Form form)
        {
            if (form.Tag is string) { return; }   // 防止重复挂钩
            form.Tag = "ui-theme";
            new NativeWindowHook(form, delegate { OnSystemThemeChanged(); });
        }

        private static void OnSystemThemeChanged()
        {
            try
            {
                if (_host != null && !_host.IsDisposed) { Refresh(); }
            }
            catch (Exception) { }
        }

        private static void Prepare(Control c)
        {
            if (c is TextBoxBase) { ((TextBoxBase)c).BorderStyle = BorderStyle.FixedSingle; }
            else if (c is ComboBox) { ((ComboBox)c).FlatStyle = FlatStyle.Flat; }
        }

        private static void StyleChildren(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                StyleControl(c);
                StyleChildren(c);
            }
        }

        private static void StyleControl(Control c)
        {
            if (c is Button) StyleButton((Button)c);
            else if (c is GroupBox) StyleGroupBox((GroupBox)c);
            else if (c is Label) StyleLabel((Label)c);
            else if (c is TabControl) StyleTabs((TabControl)c);
            else if (c is TabPage) c.BackColor = _dark ? DSurface : LBack;
            else if (c is StatusStrip) StyleStatusStrip((StatusStrip)c);
            else if (c is ToolStrip) StyleToolStrip((ToolStrip)c);
            else if (c is PictureBox) ((PictureBox)c).SizeMode = PictureBoxSizeMode.Zoom;
            else if (c is RichTextBox) StyleTerminal((RichTextBox)c);
            else if (c is TextBoxBase) StyleInput((TextBoxBase)c);
            else if (c is ComboBox) StyleInput((ComboBox)c);
            else if (c is NumericUpDown) StyleInput((NumericUpDown)c);
            else if (c is CheckBox) StyleCheckBox((CheckBox)c);
            else if (c is Panel || c is FlowLayoutPanel || c is TableLayoutPanel)
            {
                if (c is GroupBox) { /* handled */ }
                else if (c.BackColor == SystemColors.Control || c.BackColor == Color.Transparent) { c.BackColor = Color.Transparent; }
            }
        }

        // ---------------- 按钮 ----------------
        private static void StyleButton(Button b)
        {
            string n = b.Name ?? string.Empty;
            bool danger = Has(n, "Kill") || Has(n, "EStop") || Has(n, "Format")
                       || Has(n, "LockUfuid") || Has(n, "ConClr");
            bool neutral = Has(n, "CLI") || Has(n, "Diff") || Has(n, "Hex") || Has(n, "SelectKey")
                        || Has(n, "Save") || Has(n, "Color") || Has(n, "Update");

            Color accent = danger ? (neutral ? DTextDim : DangerAccent) : (neutral ? DTextDim : _accent);
            Color bg = _dark ? (neutral ? DCard : accent) : accent;
            Color hover = _dark ? (neutral ? DCardHover : Lighten(accent, 0.14F)) : Lighten(accent, 0.12F);
            Color fg = _dark
                ? (neutral ? DText : ContrastOn(accent))
                : ContrastOn(accent);

            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = _dark ? 1 : 0;
            b.FlatAppearance.BorderColor = _dark ? (danger || neutral ? DBorder : accent) : accent;
            b.FlatAppearance.MouseOverBackColor = hover;
            b.FlatAppearance.MouseDownBackColor = _dark ? DCardHover : Lighten(accent, 0.24F);
            b.BackColor = bg;
            b.ForeColor = fg;
            b.Cursor = Cursors.Hand;

            // 绝对定位布局下按钮宽度写死，中文文案常被裁掉；仅在流式容器内按文本加宽。
            if (b.Parent is FlowLayoutPanel) { FitButtonWidth(b); }
            if (b.Parent is FlowLayoutPanel) { FitHeight(b); }

            b.MouseEnter += delegate { b.BackColor = hover; };
            b.MouseLeave += delegate { b.BackColor = bg; };
        }

        /// <summary>
        /// 按钮文字溢出修复：按实际文本测量宽度（含内边距与边框），
        /// 不足则加宽。仅在流式容器内进行——FlowLayoutPanel 会自动重排，
        /// 若在 TableLayoutPanel 固定列里加宽反而会挤压其它控件。
        /// </summary>
        private static void FitButtonWidth(Button b)
        {
            if (b == null) { return; }
            try
            {
                if (!(b.Parent is FlowLayoutPanel)) { return; }
                int need = MeasureTextWidth(b.Text, b.Font) + 24;   // 22px 内边距 + 2px 余量
                if (b.Width < need) { b.Width = need; }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 测量单行文本宽度。用 TextRenderer（GDI，与控件实际绘制一致）避免 Graphics 依赖控件句柄。
        /// </summary>
        private static int MeasureTextWidth(string text, Font font)
        {
            if (string.IsNullOrEmpty(text)) { return 0; }
            try
            {
                Size s = TextRenderer.MeasureText(text, font, new Size(10000, 10000),
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                return s.Width;
            }
            catch (Exception) { return 0; }
        }

        private static int MeasureTextHeight(string text, Font font, int width)
        {
            if (string.IsNullOrEmpty(text) || width <= 0) { return 0; }
            try
            {
                return TextRenderer.MeasureText(text, font, new Size(width, 10000),
                    TextFormatFlags.WordBreak).Height;
            }
            catch (Exception) { return 0; }
        }

        /// <summary>深色下把按钮高度留出一点余量，避免文字贴边显得拥挤。</summary>
        private static void FitHeight(Button b)
        {
            try
            {
                int need = b.Font.Height + 12;
                if (b.Height < need && need - b.Height <= 12) { b.Height = need; }
            }
            catch (Exception) { }
        }

        private static Color Lighten(Color c, float amount)
        {
            int r = (int)Math.Min(255, c.R + 255 * amount);
            int g = (int)Math.Min(255, c.G + 255 * amount);
            int b = (int)Math.Min(255, c.B + 255 * amount);
            return Color.FromArgb(r, g, b);
        }

        /// <summary>按底色亮度自动选黑/白文字，避免强调色偏亮时出现白字看不清。</summary>
        private static Color ContrastOn(Color bg)
        {
            return Luma(bg) > 150 ? Color.FromArgb(0x1A, 0x1A, 0x1A) : Color.White;
        }

        // ---------------- 分组框 ----------------
        private static void StyleGroupBox(GroupBox g)
        {
            g.ForeColor = _dark ? DText : Color.FromArgb(0x37, 0x47, 0x4F);
            g.BackColor = Color.Transparent;
            g.Padding = new Padding(6, 4, 6, 6);
            if (g.Parent is FlowLayoutPanel) { FitGroupWidth(g); }
            PaintGroupBorder(g);
        }

        /// <summary>
        /// 深色下 GroupBox 的系统边框仍是浅灰，在深底上非常突兀；
        /// 这里自绘一条与主题一致的圆角边框并置为透明背景。
        /// </summary>
        private static void PaintGroupBorder(GroupBox g)
        {
            if (!_dark) { return; }
            try
            {
                Color border = DBorder;
                g.Paint += delegate(object sender, PaintEventArgs e)
                {
                    GroupBox box = (GroupBox)sender;
                    Rectangle r = new Rectangle(0, 7, box.Width - 1, box.Height - 14);
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(border))
                    using (SolidBrush fill = new SolidBrush(box.BackColor))
                    {
                        e.Graphics.FillRectangle(fill, r);
                        e.Graphics.DrawRectangle(pen, r);
                    }
                };
            }
            catch (Exception) { }
        }

        /// <summary>分组框标题溢出修复：标题过长时 WinForms 会折成两行（竖排），按需放宽。</summary>
        private static void FitGroupWidth(GroupBox g)
        {
            if (g == null) { return; }
            try
            {
                if (!(g.Parent is FlowLayoutPanel)) { return; }
                int need = MeasureTextWidth(g.Text, g.Font) + g.Padding.Horizontal + 28;
                if (g.Width < need) { g.Width = need; }
            }
            catch (Exception) { }
        }

        // ---------------- 标签 ----------------
        /// <summary>
        /// 标签溢出修复。原作的标签多为固定尺寸 + 绝对定位，换字体或换语言后文字会被裁，
        /// 这里按实测需求调整尺寸：
        ///   · 单行放不下 → 优先加宽（若父容器允许），否则改自动尺寸；
        ///   · 多行被裁 → 按实测高度增高；
        ///   · 宽度已足够但仍竖排 → 放宽所在列（仅固定像素列）。
        /// </summary>
        private static void StyleLabel(Label l)
        {
            if (l == null) { return; }

            // SAK 支持说明是原作者刻意用蓝色强调的重要提示（原本靠父容器继承），
            // 分组框配色会覆盖继承色，这里显式补回。
            if (l.Name == "label13") { l.ForeColor = _dark ? Color.FromArgb(0x8A, 0xC0, 0xFF) : Color.FromArgb(0x0D, 0x47, 0xA1); }
            else if (l.ForeColor == SystemColors.ControlText) { l.ForeColor = _dark ? DText : LText; }

            if (l.AutoSize || string.IsNullOrEmpty(l.Text)) { return; }

            try
            {
                bool hasBreak = l.Text.IndexOf('\n') >= 0;
                if (hasBreak)
                {
                    // 文案自带换行：只需保证高度足够
                    int need = MeasureTextHeight(l.Text, l.Font, l.Width);
                    if (need > l.Height && need - l.Height <= 60) { l.Height = need; }
                    return;
                }

                int textW = MeasureTextWidth(l.Text, l.Font);
                int needW = textW + 8;
                if (needW <= l.Width) { return; }   // 放得下，不动

                // 单行放不下
                if (l.Parent is FlowLayoutPanel || l.Parent is TableLayoutPanel)
                {
                    if (l.Parent is FlowLayoutPanel) { l.Width = needW; }
                    else { GrowParentColumn(l, needW); if (l.Width < needW) { l.Width = needW; } }
                    return;
                }

                // 父容器是固定尺寸面板：让标签自动适应宽度，交给父容器决定是否滚动/裁剪
                if (needW - l.Width <= 200) { l.AutoSize = true; }
            }
            catch (Exception) { }
        }

        /// <summary>把标签所在容器的固定像素列放宽到能容下文本。</summary>
        private static void GrowParentColumn(Control c, int neededWidth)
        {
            TableLayoutPanel tlp = c.Parent as TableLayoutPanel;
            if (tlp == null) { return; }
            try
            {
                int col = tlp.GetColumn(c);
                if (col < 0 || col >= tlp.ColumnStyles.Count) { return; }
                ColumnStyle cs = tlp.ColumnStyles[col];
                if (cs.SizeType != System.Windows.Forms.SizeType.Absolute) { return; }
                int add = neededWidth - c.Width;
                if (add > 0 && add <= 200)
                {
                    tlp.ColumnStyles[col] = new ColumnStyle(System.Windows.Forms.SizeType.Absolute, cs.Width + add);
                }
            }
            catch (Exception) { }
        }

        // ---------------- 选项卡 ----------------
        private static void StyleTabs(TabControl t)
        {
            t.DrawMode = TabDrawMode.OwnerDrawFixed;
            t.Padding = new Point(18, 6);
            t.DrawItem += delegate(object sender, DrawItemEventArgs e)
            {
                TabControl tab = (TabControl)sender;
                if (e.Index < 0 || e.Index >= tab.TabPages.Count) { return; }
                Graphics g = e.Graphics;
                bool sel = (e.Index == tab.SelectedIndex);
                Rectangle r = e.Bounds;

                Color bg = sel ? (_dark ? _accent : Color.FromArgb(0xE9, 0xF2, 0xFD)) : (_dark ? DSurface : LSurface);
                Color fg = sel ? (_dark ? DText : Color.FromArgb(0x0B, 0x57, 0xA4)) : (_dark ? DTextDim : LTextDim);
                Color line = sel ? (_dark ? _accent : Color.FromArgb(0x0B, 0x57, 0xA4)) : (_dark ? DBorder : LBorder);

                using (SolidBrush b1 = new SolidBrush(bg)) { g.FillRectangle(b1, r); }
                using (Pen pen = new Pen(line)) { g.DrawLine(pen, r.Left, r.Bottom - 1, r.Right - 1, r.Bottom - 1); }
                TextRenderer.DrawText(g, tab.TabPages[e.Index].Text, tab.Font, r, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        // ---------------- 状态栏 / 工具栏 ----------------
        private static void StyleStatusStrip(StatusStrip ss)
        {
            ss.BackColor = _dark ? DSurface : LSurface;
            ss.ForeColor = _dark ? DTextDim : LTextDim;
            ss.RenderMode = ToolStripRenderMode.System;
            ss.Padding = new Padding(8, 2, 8, 2);
        }

        private static void StyleToolStrip(ToolStrip ts)
        {
            ts.BackColor = _dark ? DBack : Color.White;
            ts.ForeColor = _dark ? DText : LText;
            ts.ImageScalingSize = new Size(16, 16);
            ts.Padding = new Padding(4, 2, 4, 2);
        }

        // ---------------- 输入控件 ----------------
        private static void StyleInput(TextBoxBase tb)
        {
            if (tb.ReadOnly && !tb.BackColor.Equals(Color.Transparent))
            {
                // 只读框保留可辨识的浅灰底，深色下用更暗的卡片色
                tb.BackColor = _dark ? Color.FromArgb(0x2A, 0x2A, 0x2A) : Color.FromArgb(0xF0, 0xF0, 0xF0);
                return;
            }
            tb.BackColor = _dark ? DCard : Color.White;
            tb.ForeColor = _dark ? DText : LText;
        }

        private static void StyleInput(ComboBox cb)
        {
            cb.BackColor = _dark ? DCard : Color.White;
            cb.ForeColor = _dark ? DText : LText;
        }

        private static void StyleInput(NumericUpDown nud)
        {
            nud.BackColor = _dark ? DCard : Color.White;
            nud.ForeColor = _dark ? DText : LText;
        }

        private static void StyleCheckBox(CheckBox cb)
        {
            cb.ForeColor = _dark ? DText : LText;
            cb.BackColor = Color.Transparent;
        }

        /// <summary>
        /// 终端输出区：深色主题下用深底浅字；浅色主题下沿用用户自定义颜色，
        /// 仅在对比度不足时兜底为深色（历史默认值是浅青字配浅蓝底，几乎不可读）。
        /// </summary>
        private static void StyleTerminal(RichTextBox rt)
        {
            if (_dark)
            {
                rt.BackColor = Color.FromArgb(0x18, 0x18, 0x18);
                rt.ForeColor = Color.FromArgb(0xD4, 0xD4, 0xD4);
            }
            else if (Luma(rt.ForeColor) - Luma(rt.BackColor) < 70)
            {
                rt.ForeColor = Color.FromArgb(0x21, 0x21, 0x21);
            }
        }

        private static double Luma(Color c) { return 0.299 * c.R + 0.587 * c.G + 0.114 * c.B; }

        // ================= 系统主题探测 =================

        private static bool SystemIsLight()
        {
            // 调试/验证用：设置环境变量 M1TOOL_THEME=light 或 dark 可强制指定主题，
            // 未设置（或为 system）时跟随 Windows 的深浅色设置。
            try
            {
                string forced = Environment.GetEnvironmentVariable("M1TOOL_THEME");
                if (!string.IsNullOrEmpty(forced))
                {
                    if (forced.Equals("dark", StringComparison.OrdinalIgnoreCase)) { return false; }
                    if (forced.Equals("light", StringComparison.OrdinalIgnoreCase)) { return true; }
                }
            }
            catch (Exception) { }

            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser
                    .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object v = key.GetValue("AppsUseLightTheme");
                        if (v != null) { return Convert.ToInt32(v) != 0; }
                    }
                }
            }
            catch (Exception) { }
            return true;   // 探测不到按浅色（与历史行为一致）
        }

        /// <summary>读取系统强调色（DWM ColorizationColor，格式为 0xAABBGGRR）。</summary>
        private static Color SystemAccent()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser
                    .OpenSubKey(@"Software\Microsoft\Windows\DWM"))
                {
                    object v = key == null ? null : key.GetValue("ColorizationColor");
                    if (v != null)
                    {
                        uint n = Convert.ToUInt32(v);
                        int r = (int)(n & 0xFF);
                        int g = (int)((n >> 8) & 0xFF);
                        int b = (int)((n >> 16) & 0xFF);
                        // 仅采纳蓝色系强调色，避免系统主题色（如橙/绿）破坏界面配色一致性
                        if (r + g + b > 60 && r + g + b < 720 && b >= g && b >= r) { return Color.FromArgb(r, g, b); }
                    }
                }
            }
            catch (Exception) { }
            return Color.Empty;   // Color 是值类型，用 IsEmpty 表示「无系统强调色」
        }

        // ================= 标题栏 / Mica / 圆角 =================

        private static void ApplyTitleBar(Form form)
        {
            if (Environment.OSVersion.Version.Major < 6) { return; }

            int dark = _dark ? 1 : 0;
            SafeDwm(form, 20, ref dark);                              // DWMWA_USE_IMMERSIVE_DARK_MODE

            int caption = ToRgb(_dark ? DSurface : Color.White);
            int text = ToRgb(_dark ? DText : Color.FromArgb(0x1A, 0x1A, 0x1A));
            SafeDwm(form, 35, ref caption);                           // DWMWA_CAPTION_COLOR
            SafeDwm(form, 36, ref text);                              // DWMWA_TEXT_COLOR

            // 圆角：Windows 11 21H2+（属性 33，取值 2 = 小圆角）
            int corner = 2;
            if (SafeDwm(form, 33, ref corner) == 0) { /* 成功即已应用 */ }

            // Mica：Windows 11 22H2+（属性 38，取值 2 = 主窗口 Mica，1 = 侧边栏 Acrylic）
            // 需要窗口透明合成，与 WinForms 不透明渲染有冲突，故仅尝试一次，失败即回退纯色。
            if (_dark)
            {
                int backdrop = 2;
                SafeDwm(form, 38, ref backdrop);
            }
        }

        private static int SafeDwm(Form form, int attr, ref int value)
        {
            try { return DwmSetWindowAttribute(form.Handle, attr, ref value, sizeof(int)); }
            catch (Exception) { return -1; }
        }

        private static int ToRgb(Color c) { return c.R | (c.G << 8) | (c.B << 16); }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        // ================= 系统主题变更消息钩子 =================

        /// <summary>监听 WM_SETTINGCHANGE：用户在系统里切换深浅色时界面即时刷新。</summary>
        private sealed class NativeWindowHook : NativeWindow, IDisposable
        {
            private const int WM_SETTINGCHANGE = 0x001A;
            private const int WM_SYSCOLORCHANGE = 0x0015;
            private readonly Action _onChanged;
            private readonly Form _form;

            internal NativeWindowHook(Form form, Action onChanged)
            {
                _form = form;
                _onChanged = onChanged;
                try
                {
                    AssignHandle(form.Handle);
                    form.Disposed += delegate { Dispose(); };
                }
                catch (Exception) { }
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                try
                {
                    if (m.Msg == WM_SETTINGCHANGE || m.Msg == WM_SYSCOLORCHANGE)
                    {
                        // 系统主题切换后控件句柄需重建，稍作延迟再重绘
                        Timer timer = new Timer();
                        timer.Interval = 120;
                        timer.Tick += delegate(object s, EventArgs e)
                        {
                            ((Timer)s).Stop();
                            if (_onChanged != null) { _onChanged(); }
                        };
                        timer.Start();
                    }
                }
                catch (Exception) { }
            }

            public void Dispose() { try { ReleaseHandle(); } catch (Exception) { } }
        }

        private static bool Has(string name, string keyword)
        {
            return name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}