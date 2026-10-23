using System;
using System.Collections;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using M1ToolPlus.Properties;

namespace M1ToolPlus
{
    /// <summary>
    /// 运行时 UI 主题（Fluent 风格化）。
    ///
    /// 定位说明：本项目界面是 VS2010 时代用绝对定位搭的（FixedSingle 窗体 + 固定坐标），
    /// 改 Designer/resx 布局会导致控件错位，因此**不改任何布局参数**，只在运行时统一外观。
    ///
    /// 风格参考 Windows 11 Fluent Design（Mica / 圆角 / 雅黑字体），并按能力自适应降级：
    ///   · Win11 22H2+ 自动启用 Mica 背景与窗口圆角，标题栏随深浅色切换；
    ///   · Win10 或不支持时自动回退为纯色深/浅主题，功能与可读性不受影响；
    ///   · 主题跟随系统（HKCU\...\Themes\Personalize\AppsUseLightTheme），系统切换时即时响应，
    ///     也可在「界面设置」里手动指定。
    ///
    /// 注：Mica 需要窗口透明合成，与 WinForms 的不透明渲染存在冲突，因此只对顶层窗体尝试，
    ///     失败即静默回退，不影响任何功能。
    /// </summary>
    internal static partial class UiTheme
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
        private static Font _terminalFont;
        private static Hashtable _fontCache;
        private static Form _host;
        private static NativeWindowHook _themeHook;

        // 主题刷新/缩放时避免重复挂事件、重复排队
        private static readonly Hashtable _adaptiveHooked = new Hashtable();
        private static readonly Hashtable _tabsHooked = new Hashtable();
        private static readonly Hashtable _bandPending = new Hashtable();
        private static readonly Hashtable _bandHandleHooked = new Hashtable();

        // 用户拖动窗口边缘缩放期间：临时取消功能区 Dock，避免逐帧重排/重绘；
        // 松手后恢复并一次性刷新，避免每个 WM_SIZE 都重绘上百个控件（实测单步约 75ms）。
        private static bool _inUserResize;
        private static bool _resizeFrozen;
        private static Size _resizeStartSize;
        private static DockStyle _resizeTabsDock = DockStyle.Top;

        /// <summary>是否处于深色模式。</summary>
        internal static bool IsDark { get { return _dark; } }

        /// <summary>
        /// 界面字号（**像素**）。等价于 96 DPI 下的 9pt 微软雅黑（em 12px、行高 16px）。
        /// 原作 8.25pt 在屏幕上偏小、久看吃力，提到这个尺寸。
        /// 不再往大调：按钮列宽是 84~97px 的固定值（绝对定位布局的既成事实），
        /// 字号再大「从UID卡读回」「写(UF)UID卡」等文案就会溢出。
        ///
        /// 为什么用像素而不是磅：进程已声明 DPI 感知（Program.Main 里的 SetProcessDPIAware），
        /// 若字号仍用 Point，125%/150% 缩放下文字会跟着放大 25%/50%，而绝对定位的
        /// 控件坐标不缩放——结果是文字被裁、间距失衡。像素字号在任何 DPI 下
        /// 都渲染出同样的物理像素高度，界面才能真正 1:1。
        /// </summary>
        private const float UiFontPx = 12F;

        /// <summary>
        /// 按钮左右内边距（像素）。列宽是 84~97px 的固定值，
        /// 内边距取 18px 是在「观感留白」与「文案能放下」之间的折中。
        /// </summary>
        private const int ButtonPaddingX = 18;

        /// <summary>
        /// 界面字体。
        ///
        /// 为什么不用 Segoe UI：它是**拉丁字体，不含中文字形**，中文全靠系统回退渲染，
        /// 中英混排时字重与基线不一致——这正是「难以辨识」的根因。
        /// 微软雅黑是 Windows 中文环境清晰度最高的常规字体：笔画均匀、
        /// 有 Light/Regular/Bold 分级、屏幕小字号下辨识度好。
        /// 逐级回退保证任何环境都不会出现方框字。
        /// </summary>
        internal static Font UiFont
        {
            get
            {
                if (_font == null)
                {
                    string[] candidates = new string[] { "Microsoft YaHei UI", "Microsoft YaHei", "微软雅黑" };
                    for (int i = 0; i < candidates.Length; i++)
                    {
                        try
                        {
                            using (Font probe = new Font(candidates[i], UiFontPx, FontStyle.Regular, GraphicsUnit.Pixel)) { }
                            _font = new Font(candidates[i], UiFontPx, FontStyle.Regular, GraphicsUnit.Pixel);
                            break;
                        }
                        catch (Exception) { }
                    }
                    if (_font == null)
                    {
                        try { _font = new Font(SystemFonts.MessageBoxFont.FontFamily, UiFontPx, FontStyle.Regular, GraphicsUnit.Pixel); }
                        catch (Exception) { _font = SystemFonts.MessageBoxFont; }
                    }
                }
                return _font;
            }
        }

        /// <summary>
        /// 终端输出区字体：等宽字体让 libnfc 输出的十六进制、扇区号逐行对齐，便于核对。
        /// Consolas 无中文字形，中文会回退到雅黑（等宽回退恰为全角，视觉上仍对齐）。
        /// </summary>
        internal static Font TerminalFont
        {
            get
            {
                if (_terminalFont == null)
                {
                    string[] candidates = new string[] { "Consolas", "Cascadia Mono", "Courier New" };
                    for (int i = 0; i < candidates.Length; i++)
                    {
                        try
                        {
                            using (Font probe = new Font(candidates[i], UiFontPx, FontStyle.Regular, GraphicsUnit.Pixel)) { }
                            _terminalFont = new Font(candidates[i], UiFontPx, FontStyle.Regular, GraphicsUnit.Pixel);
                            break;
                        }
                        catch (Exception) { }
                    }
                    if (_terminalFont == null) { _terminalFont = UiFont; }
                }
                return _terminalFont;
            }
        }

        // ================= 对外入口 =================

        /// <summary>应用主题（跟随系统深浅色，或设置页手动指定）。窗体句柄需已创建才能设置标题栏。</summary>
        internal static void Apply(Form form)
        {
            if (form == null) { return; }
            // 只把第一个应用主题的窗体当作主窗体。工具窗/对话框也会调用 Apply，
            // 不能让它抢走 _host，否则系统主题变化时主窗口不再刷新。
            bool firstHost = (_host == null || _host.IsDisposed);
            if (firstHost) { _host = form; }
            RefreshSystemTheme();
            ApplyPalette(form);
            if (firstHost) { WatchSystemTheme(form); }
        }

        /// <summary>重新读取主题设置并刷新所有已打开窗体（系统切换深浅色或用户在设置里手动指定时调用）。</summary>
        internal static void Refresh()
        {
            RefreshSystemTheme();
            // 复制一份：样式刷新过程中可能有窗体关闭，直接遍历 OpenForms 会抛异常。
            ArrayList forms = new ArrayList();
            foreach (Form open in Application.OpenForms) { forms.Add(open); }
            foreach (Form form in forms)
            {
                if (form == null || form.IsDisposed) { continue; }
                try { ApplyPalette(form); }
                catch (Exception) { }
            }
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
                // 0) 只有主窗口（原 FixedSingle）需要这套自适应。
                //    工具窗/对话框是 FixedDialog，尺寸由 Designer 按内容定好
                //    （如 DiffTool 370x456）；若也套用下面的 900x640 下限，
                //    窗体被强行撑大、内容却仍挤在左上角，右侧/下方全是空白。
                if (form.FormBorderStyle != FormBorderStyle.FixedSingle)
                {
                    return;
                }

                // 1) 允许缩放。最小尺寸不能写死：原下限 (900,640) 是功能区的「舒适」
                //    下限，但在高缩放比例屏幕（如 1080p@200% 的逻辑高度只有 ~540）
                //    上比整个工作区还大——窗口装不进屏幕、又缩不下去，表现为
                //    「初始窗口超出桌面、拖拽调整无效」。两者取小 + 极小下限兜底。
                if (form.FormBorderStyle == FormBorderStyle.FixedSingle)
                {
                    form.FormBorderStyle = FormBorderStyle.Sizable;
                }
                form.MaximizeBox = true;
                Rectangle waFit = Screen.FromControl(form).WorkingArea;
                form.MinimumSize = new Size(
                    Math.Min(900, Math.Max(420, waFit.Width - 16)),
                    Math.Min(640, Math.Max(360, waFit.Height - 16)));
                if (form.StartPosition == FormStartPosition.WindowsDefaultLocation)
                {
                    form.StartPosition = FormStartPosition.CenterScreen;
                }

                // 2) 顶层三区改为 Dock：状态栏贴底、功能区贴顶、终端区放在功能区下方
                StatusStrip status = null;
                RichTextBox terminal = null;
                TabControl tabs = FindTabControl(form);
                foreach (Control c in form.Controls)
                {
                    if (c is StatusStrip) { status = (StatusStrip)c; c.Dock = DockStyle.Bottom; }
                    else if (c is TabControl) { c.Dock = DockStyle.Top; }
                    else if (c is RichTextBox) { terminal = (RichTextBox)c; }
                }
                // 终端区不能 Dock.Fill 铺满整窗：TabControl 在 z 序上层，
                // Fill 会把 RichTextBox 拉成 (0,0,整窗)，日志从顶部开始写，
                // 前几百像素被页签盖住——用户看到的就是一片空白（Issue：检测连接无输出）。
                // 这里显式放到功能区下方、状态栏上方，并四边锚定跟随窗口缩放。
                if (terminal != null && terminal.Parent == form)
                {
                    form.PerformLayout();
                    int termTop = (tabs != null) ? tabs.Bottom : 0;
                    int termBottom = (status != null) ? status.Top : form.ClientSize.Height;
                    terminal.Dock = DockStyle.None;
                    terminal.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                    terminal.Bounds = new Rectangle(0, termTop, form.ClientSize.Width, Math.Max(0, termBottom - termTop));
                }

                if (tabs != null)
                {
                    // 3) 宽度变化时重排功能区（TabControl 内的绝对定位控件默认不会重排）
                    HookResizeReflow(form, tabs);
                    LayoutFlowPanels(tabs);

                    // 4) 功能区实测所需宽度：窗口初始宽度不足时自动扩展，
                    //    避免按钮加宽后把右侧控件（提示文字、停止按钮）挤出可视区。
                    //    撑宽同样受工作区约束——不检查的话，把窗口直接撑出屏幕。
                    int needed = MeasureRequiredWidth(tabs);
                    if (needed > tabs.Width)
                    {
                        int want = needed + 12;
                        Rectangle waGrow = Screen.FromControl(form).WorkingArea;
                        if (want > waGrow.Width - 16) { want = waGrow.Width - 16; }
                        form.ClientSize = new Size(want, form.ClientSize.Height);
                    }
                }

                // 5) 初始位置钳制：CenterScreen 只居中、不查边界，窗口一旦比工作区
                //    大就会两侧溢出（标题栏沉出屏幕顶、状态栏压进任务栏）。
                ClampToWorkArea(form);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 主窗口缩放事件只挂一次。用户拖动边缘缩放时：
        ///   · 临时取消功能区的 Dock，让 TabControl 保持原尺寸、内容不重绘；
        ///   · 新增区域由窗体背景填充，终端/状态栏仍实时跟随；
        ///   · 松手后恢复 Dock，一次性重排并刷新功能区。
        /// 程序化缩放（最大化/恢复）不走 ResizeBegin，仍然实时重排。
        /// </summary>
        private static void HookResizeReflow(Form form, TabControl tabs)
        {
            if (form == null || tabs == null || _adaptiveHooked.ContainsKey(form)) { return; }
            _adaptiveHooked[form] = true;

            tabs.Resize += delegate
            {
                if (!_inUserResize) { LayoutFlowPanels(tabs); }
            };

            form.ResizeBegin += delegate
            {
                // 拖动标题栏移动窗口时也会触发 ResizeBegin/ResizeEnd，但尺寸不变；
                // 取消 Dock 再恢复对内容没有影响，松手后也不会做昂贵重排。
                _resizeFrozen = true;
                _resizeStartSize = form.Size;
                _inUserResize = true;
                _resizeTabsDock = tabs.Dock;
                tabs.Dock = DockStyle.None;
            };

            form.ResizeEnd += delegate
            {
                bool frozen = _resizeFrozen;
                bool sizeChanged = (form.Size != _resizeStartSize);
                _resizeFrozen = false;
                _inUserResize = false;
                if (!frozen) { return; }
                try
                {
                    tabs.Dock = _resizeTabsDock;
                    if (sizeChanged)
                    {
                        LayoutFlowPanels(tabs);
                        LayoutBandStrips(tabs);
                        tabs.Invalidate(true);
                        tabs.Update();
                    }
                    else
                    {
                        tabs.Invalidate(true);
                    }
                }
                catch (Exception) { }
            };
        }

        /// <summary>
        /// 把窗口钳制到所在显示器工作区内。初始定位（CenterScreen）只居中、
        /// 不检查边界：窗口比工作区大时会两侧溢出——标题栏沉出屏幕顶、
        /// 状态栏压进任务栏。按「先缩到放得下、再挪回可视区」处理。
        /// 前提：MinimumSize 已先按工作区放宽，否则缩小赋值会被 MinimumSize 忽略。
        /// 仅在 Normal 状态执行（最大化的窗口由系统管）。
        /// </summary>
        private static void ClampToWorkArea(Form form)
        {
            try
            {
                if (form.WindowState != FormWindowState.Normal) { return; }
                Rectangle wa = Screen.FromControl(form).WorkingArea;
                if (form.Height > wa.Height) { form.Height = wa.Height; }
                if (form.Width > wa.Width) { form.Width = wa.Width; }
                int left = form.Left;
                int top = form.Top;
                if (form.Right > wa.Right) { left = wa.Right - form.Width; }
                if (form.Bottom > wa.Bottom) { top = wa.Bottom - form.Height; }
                if (left < wa.Left) { left = wa.Left; }
                if (top < wa.Top) { top = wa.Top; }
                if (left != form.Left || top != form.Top) { form.Location = new Point(left, top); }
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
                        // 不再显式 flp.PerformLayout()：WinForms 在尺寸变化时已经会
                        // 对流式容器做布局，这里再强制一次会让 24 个嵌套 FlowLayoutPanel
                        // 反复级联布局（实测单次约 100ms，拖动窗口直接卡死）。
                        // 按钮是 AutoSize=True，宽度由 WinForms 按字体算；这里再赋 Width
                        // 会被 AutoSize 立刻拉回，反而触发整棵树反复重排（实测约 90ms）。
                        // 只保留分组框标题的「放宽下限」，它是幂等的。
                        foreach (Control child in flp.Controls)
                        {
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
            // 必须持有引用：NativeWindow 一旦被 GC 回收，finalizer 会 ReleaseHandle，
            // 把窗口过程还原、主题变更监听随之失效。存到静态字段里保活。
            _themeHook = new NativeWindowHook(form, delegate { OnSystemThemeChanged(); });
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
            // 终端区（RichTextBox）保持 BorderStyle.None：它靠圆角 Region 自绘外观，
            // 系统 FixedSingle 边框会在深色底上画出一条白边（右/下各 1px）。
            if (c is RichTextBox) { return; }
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
            // 统一字体（必须在最前面）：
            // Form1.resx 里有 81 个控件带**显式字体**，且清一色是 "Microsoft Sans Serif"——
            // 该字体只有西文字形，中文全靠 FontLink 回退到宋体，小字号下笔画粘连、
            // 正是「难以辨识」的另一半根因。这些控件不继承窗体字体，
            // 所以只设 form.Font 覆盖不到，必须逐个控件显式赋值。
            ApplyFont(c);

            if (c is Button) StyleButton((Button)c);
            else if (c is GroupBox) StyleGroupBox((GroupBox)c);
            else if (c is Label) StyleLabel((Label)c);
            else if (c is TabControl) StyleTabs((TabControl)c);
            else if (c is TabPage) c.BackColor = _dark ? DBack : LSurface;
            else if (c is StatusStrip) StyleStatusStrip((StatusStrip)c);
            else if (c is ToolStrip) StyleToolStrip((ToolStrip)c);
            else if (c is PictureBox) StylePicture((PictureBox)c);
            else if (c is RichTextBox) StyleTerminal((RichTextBox)c);
            else if (c is TextBoxBase) StyleInput((TextBoxBase)c);
            else if (c is ComboBox) StyleInput((ComboBox)c);
            else if (c is NumericUpDown) StyleInput((NumericUpDown)c);
            else if (c is DataGridView) StyleGrid((DataGridView)c);
            else if (c is CheckBox) StyleCheckBox((ButtonBase)c);
            else if (c is RadioButton) StyleCheckBox((ButtonBase)c); // RadioButton 与 CheckBox 是兄弟类型，都继承 ButtonBase，不能互转
            else if (c is Panel || c is FlowLayoutPanel || c is TableLayoutPanel)
            {
                if (c is GroupBox) { /* handled */ }
                else
                {
                    // 运行时实测：这些容器在 resx 里带**显式的 Color.White（FFFFFFFF）**，
                    // 既不是 SystemColors.Control 也不是 Transparent——旧的 `|| Transparent`
                    // 守卫因此从不命中，深色主题下就留下一块块白底（设置页尤其明显）。
                    //
                    // 现在统一改为「跟随父容器底色」：父容器已被主题化（页签页=面板色、
                    // 分组框=卡片色），子容器直接继承即可，既不会出现白底，
                    // 也不会出现「卡片里嵌着一块深色方块」的高低错位。
                    c.BackColor = ParentBackOf(c);
                }
            }
        }

        /// <summary>取父容器底色；父容器不存在/透明时回退到窗口底色。</summary>
        private static Color ParentBackOf(Control c)
        {
            try
            {
                if (c != null && c.Parent != null && !c.Parent.BackColor.IsEmpty)
                {
                    return c.Parent.BackColor;
                }
            }
            catch (Exception) { }
            return _dark ? DBack : LBack;
        }

        /// <summary>
        /// 给控件套用界面字体，**保留原有的字形样式**（Bold/Italic/Strikeout）。
        ///
        /// 为什么要保留：resx 里有些控件（如分组框标题、说明性标签）刻意设了 Bold，
        /// 一刀切成 Regular 会丢掉标题与正文的层次。
        ///
        /// 字体对象按「样式」缓存共享：WinForms 不会因为多个控件共用同一个 Font
        /// 实例而去销毁它，这里也不 Dispose，随进程结束由 GC 回收。
        /// </summary>
        private static void ApplyFont(Control c)
        {
            if (c == null) { return; }
            try
            {
                // 终端输出区用等宽字体，其余控件用界面字体
                bool mono = (c is RichTextBox);
                Font want = mono ? TerminalFont : UiFont;
                float px = UiFontPx;                     // 字号是像素值，不是磅值

                Font cur = c.Font;
                if (cur != null && cur.Unit == GraphicsUnit.Pixel
                    && cur.FontFamily.Name == want.FontFamily.Name
                    && Math.Abs(cur.Size - want.Size) < 0.01F && cur.Style == want.Style)
                {
                    return;   // 已经是对的，别重复赋值（赋值会触发一次重排）
                }

                FontStyle style = cur != null ? cur.Style : FontStyle.Regular;
                if (mono) { style = FontStyle.Regular; }   // 等宽字体不做粗体，十六进制更好认

                // cur.Unit 判断不可省：设计器里若已有同族同号字体，但那是**磅**值字体，
                // 在 125%/150% 缩放下会被放大——必须换成像素字体，否则 1:1 破功。
                c.Font = GetFont(want.FontFamily, px, style);
            }
            catch (Exception) { }
        }

        /// <summary>按「字体 + 字号(像素) + 样式」缓存 Font 实例，避免给每个控件都 new 一个。</summary>
        private static Font GetFont(FontFamily family, float pixelSize, FontStyle style)
        {
            string key = family.Name + "|" + pixelSize.ToString("R") + "|" + style.ToString();
            if (_fontCache == null) { _fontCache = new Hashtable(); }

            Font cached = _fontCache[key] as Font;
            if (cached != null) { return cached; }

            Font created;
            try { created = new Font(family, pixelSize, style, GraphicsUnit.Pixel); }
            catch (Exception) { created = UiFont; }

            _fontCache[key] = created;
            return created;
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
            // 边框改由自绘描边：FlatAppearance 画的是直角矩形，圆角处会露断头线
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = hover;
            b.FlatAppearance.MouseDownBackColor = _dark ? DCardHover : Lighten(accent, 0.24F);
            b.BackColor = bg;
            b.ForeColor = fg;
            b.Cursor = Cursors.Hand;

            // 圆角：Region 裁剪 + 自绘填充/描边（见 UiTheme.Round.cs）
            RegisterButtonKind(b, danger ? (neutral ? "muted" : "danger") : (neutral ? "muted" : "primary"), bg);
            HookRoundButton(b);
            ApplyRoundRegion(b, RadiusButton);

            // 字号变大后按钮高度写死的 25px 会挤字，按字体实测高度留白
            if (b.Parent is FlowLayoutPanel) { FitButtonWidth(b); FitHeight(b); }
        }

        /// <summary>
        /// 按钮文字溢出修复：按实际文本测量宽度（含内边距与边框），不足则加宽。
        ///
        /// 为什么不再动 AutoSize：resx 里绝大多数按钮是 AutoSize=True，
        /// 直接置 AutoSize=False 实测会让流式容器内的整片按钮消失（AutoSize=False +
        /// Dock=Fill 在 FlowLayoutPanel 里的交互会把尺寸算成 0）。
        /// 事实上换了雅黑 9pt 之后 AutoSize 会按新字体自动重算宽度，裁切已消失，
        /// 这里只是给流式容器内的按钮做一次兜底。
        /// </summary>
        private static void FitButtonWidth(Button b)
        {
            if (b == null) { return; }
            try
            {
                if (!(b.Parent is FlowLayoutPanel)) { return; }
                int need = MeasureTextWidth(b.Text, b.Font) + ButtonPaddingX;   // 左右内边距
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

        /// <summary>
        /// 深色下把按钮高度留出一点余量，避免文字贴边显得拥挤。
        /// </summary>
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
            // GroupBox 内部同样要实色底：若用 Transparent，里面的复选框/单选框等
            // 浅色文字控件会回退到默认白底，深色主题下就会「白底 + 白字」看不见。
            // 卡片色比所在页签面略亮一档，形成「浮起的圆角卡片」层次。
            g.BackColor = _dark ? DCard : Color.White;
            g.Padding = new Padding(6, 4, 6, 6);

            // 圆角卡片：自绘圆角底 + 圆角描边 + 标题（盖掉系统画的直角边框与标题）
            HookRoundGroupBox(g);

            // 分组框只是视觉容器，不需要参与 Tab 键焦点序列。
            // 默认 TabStop=True 时启动时它会拿到焦点，WinForms 随即画一个虚线焦点框，
            // 在深色主题下是一条刺眼的亮色虚线，还正好压在标题文字上（看着像字被划掉）。
            // 分组框内容里的按钮/输入框照样能被 Tab 到，所以关掉它的 TabStop 没有副作用。
            if (g.TabStop) { g.TabStop = false; }

            // AutoSize 改成「只涨不缩」：resx 里分组框是 AutoSize=True + GrowAndShrink，
            // 我们按标题实测宽度手工放宽的 Width 会被它下一轮布局拉回去。
            // GrowAndShrink -> GrowOnly 后，手工设的更大尺寸能被保留，
            // 而高度的自动撑开（依赖内部流式容器算高度）也照常工作。
            //
            // 注意：绝不能直接置 AutoSize=False——实测那样会让分组框整体塌成一条，
            // 内部 FlowLayoutPanel 随之换行，整组按钮消失。
            if (g.AutoSize && g.AutoSizeMode == AutoSizeMode.GrowAndShrink)
            {
                g.AutoSizeMode = AutoSizeMode.GrowOnly;
            }

            FitGroupWidth(g);
        }
        // 注：GroupBox 的标题由系统绘制（顶边留缺口），一旦自绘就会与系统标题重影。
        // 深色下边框呈浅灰属于系统观感，强行改色需要 owner-draw GroupBox（.NET 4.0 无现成实现），
        // 故此处只调 ForeColor，不自绘边框。

        /// <summary>
        /// 分组框标题溢出修复：标题过长时 WinForms 会折成两行（显示成竖排）。
        ///
        /// **绝对不能关掉 GroupBox.AutoSize**：resx 里所有 GroupBox 都是 AutoSize=True，
        /// 其高度正是靠 AutoSize 撑出来的；关掉后分组框会塌成一条，
        /// 里面的流式容器随之换行，按钮整片消失（实测踩过）。
        /// 所以这里只保留「父容器是流式面板时按实测宽度放宽」这一条安全路径。
        /// </summary>
        private static void FitGroupWidth(GroupBox g)
        {
            if (g == null) { return; }
            try
            {
                int need = MeasureTextWidth(g.Text, g.Font) + g.Padding.Horizontal + 34;
                if (g.Width >= need) { return; }

                if (g.Parent is FlowLayoutPanel)
                {
                    // 不能只设 Width：AutoSize 引擎会按「内容首选宽度」把手设值拉回去。
                    // 首选宽度以内部控件为准、**不含标题**，于是「窄内容 + 长标题」的
                    // 分组框（如 C/FUID卡操作）标题总被边框裁掉（实测线条直接压住文字）。
                    // MinimumSize 是 AutoSize 布局尊重的下限约束，不会被拉回，
                    // 也和引擎不存在抢占回环——这里用 Height=0 表示高度不设下限。
                    if (need > g.MinimumSize.Width) { g.MinimumSize = new Size(need, 0); }
                    g.Width = need;
                    return;
                }

                TableLayoutPanel tlp = g.Parent as TableLayoutPanel;
                if (tlp != null)
                {
                    GrowParentColumn(g, need);
                    if (g.Width < need) { g.Width = need; }
                    GrowAncestors(tlp, need - g.Width + 34);
                }
            }
            catch (Exception) { }
        }

        /// <summary>把表格容器及其各级父容器同步放宽 delta 像素。</summary>
        private static void GrowAncestors(Control c, int delta)
        {
            if (c == null || delta <= 0) { return; }
            try
            {
                Control parent = c.Parent;
                if (parent == null || parent is Form || parent is TabPage) { return; }

                // 表格/流式容器自身宽度受限：先把自身撑开
                int target = c.Width + delta;
                if (target > c.Width) { c.Width = target; }

                // 父容器若是表格，同样按增量放宽它所在的列
                TableLayoutPanel pt = parent as TableLayoutPanel;
                if (pt != null)
                {
                    GrowParentColumn(pt, pt.Width + delta);
                }
                GrowAncestors(parent, delta);
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

            // 链接标签（终端文字颜色/大小、语言选择等）默认是深蓝色系统色，
            // 在深色底上几乎读不清，这里按主题给一组对比度足够的链接色。
            LinkLabel ll = l as LinkLabel;
            if (ll != null)
            {
                ll.LinkColor = _dark ? Color.FromArgb(0x6C, 0xB6, 0xFF) : Color.FromArgb(0x0D, 0x47, 0xA1);
                ll.ActiveLinkColor = _dark ? Color.FromArgb(0x9A, 0xD0, 0xFF) : Color.FromArgb(0x1A, 0x6F, 0xD6);
                ll.VisitedLinkColor = ll.LinkColor;
                ll.DisabledLinkColor = _dark ? Color.FromArgb(0x6A, 0x6A, 0x6A) : Color.FromArgb(0xA0, 0xA0, 0xA0);
            }

            // SAK 支持说明是原作者刻意用蓝色强调的重要提示（原本靠父容器继承），
            // 分组框配色会覆盖继承色，这里显式补回。
            if (l.Name == "label13") { l.ForeColor = _dark ? Color.FromArgb(0x8A, 0xC0, 0xFF) : Color.FromArgb(0x0D, 0x47, 0xA1); }
            // 设计器里用 HotTrack（深蓝）强调的提示标签（如「请放原卡/新卡」），
            // 深色底上对比度极低，换成同色系的浅蓝。
            else if (l.ForeColor == SystemColors.HotTrack) { l.ForeColor = _dark ? Color.FromArgb(0x6C, 0xB6, 0xFF) : SystemColors.HotTrack; }
            else if (l.ForeColor == SystemColors.ControlText) { l.ForeColor = _dark ? DText : LText; }
            // 其它自定义的深色文字（红/绿/蓝等）在深色底上同样偏暗：向白色混合提亮，保留色相。
            else if (_dark && Luma(l.ForeColor) < 120) { l.ForeColor = BlendColors(l.ForeColor, Color.White, 0.45F); }

            if (string.IsNullOrEmpty(l.Text)) { return; }

            try
            {
                bool hasBreak = l.Text.IndexOf('\n') >= 0;

                // Dock=Left/Right 的多行标签：高度被表格行高钳死（行高只按单行算），
                // 第二行文字底部会被裁掉。取消 Dock 交回 AutoSize，行高会按实际文本撑开。
                if (hasBreak && l.AutoSize && (l.Dock == DockStyle.Left || l.Dock == DockStyle.Right))
                {
                    l.Dock = DockStyle.None;
                }

                if (l.AutoSize) { return; }

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

        // ---------------- 图标（PictureBox） ----------------

        private static readonly Hashtable _picOriginal = new Hashtable();   // PictureBox -> 原始图像
        private static readonly Hashtable _picInverted = new Hashtable();   // PictureBox -> 反色图像（由我们生成，需释放）

        /// <summary>
        /// 深色主题下把「黑色图标」反色成浅色。
        ///
        /// 原项目的图标资源（箭头、对勾、状态栏小图标）都是**黑色 PNG + 透明底**，
        /// 在深色背景上等于隐形。这里按图像亮度判定：偏暗的单色图标才反色，
        /// 彩色/浅色图片（截图、照片）保持原样，避免把正常图片弄花。
        /// </summary>
        private static void StylePicture(PictureBox p)
        {
            if (p == null) { return; }
            try
            {
                p.SizeMode = PictureBoxSizeMode.Zoom;
                Image img = ThemedImage(p, p.Image);
                if (img != null && !ReferenceEquals(p.Image, img)) { p.Image = img; }
            }
            catch (Exception) { }
        }

        /// <summary>工具栏/状态栏里的图标同样需要反色（ToolStripItem 的 Image）。</summary>
        private static void StyleToolStripImages(ToolStrip ts)
        {
            if (ts == null) { return; }
            try
            {
                foreach (ToolStripItem it in ts.Items)
                {
                    if (it == null || it.Image == null) { continue; }
                    Image img = ThemedImage(it, it.Image);
                    if (img != null && !ReferenceEquals(it.Image, img)) { it.Image = img; }
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 按主题返回该图像应显示的版本：深色主题下「整体偏暗」的单色图标返回反色副本，
        /// 其余情况返回原图。原件与反色件都缓存，避免反复重建/反复反色。
        /// </summary>
        private static Image ThemedImage(object key, Image current)
        {
            if (key == null || current == null) { return current; }
            try
            {
                Image orig;
                if (_picOriginal.ContainsKey(key)) { orig = (Image)_picOriginal[key]; }
                else
                {
                    // 首次见到的可能已是反色图（主题刷新时会重新走一遍），
                    // 但此时一定有缓存，故这里只可能是原图。
                    orig = current;
                    _picOriginal[key] = orig;
                }

                if (!_dark || !IsMostlyDarkImage(orig)) { return orig; }

                if (!_picInverted.ContainsKey(key)) { _picInverted[key] = InvertImage(orig); }
                return (Image)_picInverted[key];
            }
            catch (Exception) { return current; }
        }

        /// <summary>采样判断图像是否「整体偏暗」（只统计不透明像素）。</summary>
        private static bool IsMostlyDarkImage(Image img)
        {
            try
            {
                Bitmap bmp = img as Bitmap;
                if (bmp == null) { return false; }
                int stepX = Math.Max(1, bmp.Width / 24);
                int stepY = Math.Max(1, bmp.Height / 24);
                long sum = 0;
                int n = 0;
                for (int y = 0; y < bmp.Height; y += stepY)
                {
                    for (int x = 0; x < bmp.Width; x += stepX)
                    {
                        Color c = bmp.GetPixel(x, y);
                        if (c.A < 40) { continue; }
                        sum += (c.R * 299 + c.G * 587 + c.B * 114) / 1000;
                        n++;
                        if (n >= 600) { break; }
                    }
                    if (n >= 600) { break; }
                }
                if (n == 0) { return false; }
                return (sum / n) < 110;
            }
            catch (Exception) { return false; }
        }

        /// <summary>生成反色副本（保留 alpha）。</summary>
        private static Image InvertImage(Image src)
        {
            Bitmap bmp = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            using (System.Drawing.Imaging.ImageAttributes ia = new System.Drawing.Imaging.ImageAttributes())
            {
                System.Drawing.Imaging.ColorMatrix cm = new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
                    new float[] { -1,  0,  0, 0, 0 },
                    new float[] {  0, -1,  0, 0, 0 },
                    new float[] {  0,  0, -1, 0, 0 },
                    new float[] {  0,  0,  0, 1, 0 },
                    new float[] {  1,  1,  1, 0, 1 }
                });
                ia.SetColorMatrix(cm);
                g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height),
                    0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
            }
            return bmp;
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
            // 标签按钮本体由 OwnerDraw 上色；但**标签条本身的余白**（左右两侧、
            // 按钮上方/下方的空白）由系统按原生（浅色）主题绘制：
            // · 设 BackColor —— 无效，余白不走控件背景色；
            // · 挂 Paint 事件 —— 收不到这块的原生绘制区；
            // · 设 Appearance=FlatButtons —— 余白仍是浅色（三种都实测失败）。
            // 唯一可行的是用主题色小面板（子控件）盖住余白，
            // 并在 TabControl 尺寸变化时跟随重排。深浅两主题通用。
            t.DrawMode = TabDrawMode.OwnerDrawFixed;
            t.Padding = new Point(18, 6);
            t.Appearance = TabAppearance.FlatButtons;

            // 换主题会再次走 StyleTabs；事件只能挂一次，否则每切一次主题就多一层
            // DrawItem/Resize 处理器，越切越卡。
            if (!_tabsHooked.ContainsKey(t))
            {
                _tabsHooked[t] = true;
                StripNativeTabTheme(t);
                t.Resize += delegate(object sender, EventArgs e)
                {
                    if (!_inUserResize) { LayoutBandStrips((TabControl)sender); }
                };
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

            // 每次刷新都重算颜色/边界（DrawItem 读取的是静态 _dark/_accent，不重挂）
            LayoutBandStrips(t);
        }

        /// <summary>
        /// 去掉 TabControl 的原生主题（SetWindowTheme 空主题串）。
        /// 原生主题下标签头余白/按钮间隙/选中框描边都是从系统浅色主题绘制，
        /// WinForms 无法覆盖；去掉主题后这些区域改按 WinForms 自绘路径渲染，
        /// 浅灰白才有机会统一成主题深色。须等句柄创建后调用。
        /// </summary>
        private static void StripNativeTabTheme(TabControl t)
        {
            if (t == null || t.IsDisposed) { return; }
            try
            {
                if (!t.IsHandleCreated)
                {
                    t.HandleCreated += delegate(object s, EventArgs e) { StripNativeTabTheme((TabControl)s); };
                    return;
                }
                SetWindowTheme(t.Handle, "", "");
            }
            catch (Exception) { }
        }

        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string subAppName, string subIdList);

        /// <summary>
        /// 把标签头左右余白用面板盖住（面板挂在 TabControl 的父容器里并压在它上层，
        /// 因为 TabControl 自身的原生绘制会盖过它的子控件）。
        /// 套主题发生在 StyleChildren 的集合遍历里，此刻直接往 t.Parent.Controls
        /// 加控件会触发「集合已修改」异常——因此统一 BeginInvoke 延迟到消息循环。
        /// </summary>
        private static void LayoutBandStrips(TabControl t)
        {
            try
            {
                if (t == null || t.IsDisposed || t.TabPages.Count == 0 || t.Parent == null) { return; }
                if (!t.IsHandleCreated)
                {
                    // 刷新主题时可能反复走到这里，HandleCreated 只能挂一次
                    if (!_bandHandleHooked.ContainsKey(t))
                    {
                        _bandHandleHooked[t] = true;
                        t.HandleCreated += delegate(object s, EventArgs e)
                        {
                            _bandHandleHooked.Remove(t);
                            LayoutBandStrips((TabControl)s);
                        };
                    }
                    return;
                }
                // 连续 Resize 会排很多 BeginInvoke；合并成一次，避免消息队列堆积。
                if (_bandPending.ContainsKey(t)) { return; }
                _bandPending[t] = true;
                t.BeginInvoke(new MethodInvoker(delegate
                {
                    _bandPending.Remove(t);
                    // 用户拖动缩放期间保持冻结，松手后由 ResizeEnd 统一重排
                    if (!_inUserResize) { ApplyBandStrips(t); }
                }));
            }
            catch (Exception) { if (t != null) { _bandPending.Remove(t); } }
        }

        private static void ApplyBandStrips(TabControl t)
        {
            try
            {
                if (t.IsDisposed || t.Disposing || t.Parent == null) { return; }
                int bandH = Math.Max(1, t.DisplayRectangle.Y);
                Control host = t.Parent;
                UiTheme.TabBandPanel p = null;
                foreach (Control c in host.Controls)
                {
                    if (c.Tag is TabControl && c is UiTheme.TabBandPanel) { p = (UiTheme.TabBandPanel)c; break; }
                }
                if (p == null)
                {
                    p = new UiTheme.TabBandPanel { Tab = t, Tag = t, TabStop = false };
                    p.MouseClick += delegate(object s, MouseEventArgs e) { HitSelect(t, e.Location); };
                    p.MouseMove += delegate(object s, MouseEventArgs e) { HoverBand(p, e.Location); };
                    p.MouseLeave += delegate(object s, EventArgs e) { HoverBand(p, new Point(-999, -999)); };
                    // 切换标签时必须重绘自绘面板：原生重绘发生在面板下面，
                    // 覆盖其上的本面板不同步的话，选中高亮会滞留在旧页签上。
                    t.SelectedIndexChanged += delegate(object s, EventArgs e)
                    {
                        try
                        {
                            TabControl tc = (TabControl)s;
                            foreach (Control pp in tc.Parent.Controls)
                            {
                                if (pp is UiTheme.TabBandPanel) { pp.Invalidate(); }
                            }
                        }
                        catch (Exception) { }
                    };
                    host.Controls.Add(p);
                    host.Controls.SetChildIndex(p, 0);   // 压到最上层，盖住原生标签头
                }
                p.ThemeColors(new UiTheme.TabBandColors
                {
                    Band = _dark ? DSurface : LBack,
                    BtnBg = _dark ? DSurface : LSurface,
                    BtnFg = _dark ? DTextDim : LTextDim,
                    SelBg = _dark ? _accent : Color.FromArgb(0xE9, 0xF2, 0xFD),
                    SelFg = _dark ? DText : Color.FromArgb(0x0B, 0x57, 0xA4),
                    SelLine = _dark ? _accent : Color.FromArgb(0x0B, 0x57, 0xA4),
                    HoverBg = _dark ? DCardHover : LCardHover,
                    Sep = _dark ? DBorder : LBorder
                });
                p.Bounds = new Rectangle(t.Left, t.Top, t.Width, bandH);
                p.Invalidate();

                // 外框覆盖层：盖掉 TabControl 自己画的那圈系统浅色边框
                UiTheme.TabFramePanel f = EnsureFramePanel(t, host);
                if (f != null && host.Controls.Count > 1)
                {
                    // z 序（0 = 最前）：标签条 > 外框 > TabControl
                    host.Controls.SetChildIndex(f, 1);
                    host.Controls.SetChildIndex(p, 0);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// TabControl 去掉原生主题后，页面左/右/下会被画上一圈约 4px 的系统边框
        /// （颜色 = SystemColors.Control #F0F0F0）。深色主题里它与 1px 卡片描边
        /// 并排，就是一条粗细不均的白线。这里用与 TabControl 同尺寸的面板覆盖，
        /// Region 挖掉页面区域、只留那圈边框：填页面底色并在底边画 1px 分隔线。
        /// 挖空后页面区域的鼠标事件照常穿透，不影响操作。
        /// </summary>
        private static UiTheme.TabFramePanel EnsureFramePanel(TabControl t, Control host)
        {
            try
            {
                UiTheme.TabFramePanel f = null;
                foreach (Control c in host.Controls)
                {
                    if (c.Tag is TabControl && c is UiTheme.TabFramePanel) { f = (UiTheme.TabFramePanel)c; break; }
                }
                if (f == null)
                {
                    f = new UiTheme.TabFramePanel { Tag = t, TabStop = false };
                    host.Controls.Add(f);
                }
                f.ThemeColors(_dark ? DBack : LSurface, _dark ? DBorder : LBorder);
                f.Bounds = new Rectangle(t.Left, t.Top, t.Width, t.Height);

                Rectangle page = t.DisplayRectangle;
                bool hasRing = page.Left > 0 || page.Top > 0 || page.Right < f.Width || page.Bottom < f.Height;
                if (hasRing && page.Width > 2 && page.Height > 2 && f.Width > 0 && f.Height > 0)
                {
                    Region ring = new Region(new Rectangle(0, 0, f.Width, f.Height));
                    ring.Exclude(page);
                    Region old = f.Region;
                    f.Region = ring;   // 控件持有的是引用，新 Region 绝不能 Dispose
                    if (old != null) { old.Dispose(); }
                }
                f.Invalidate();
                return f;
            }
            catch (Exception) { return null; }
        }

        private static void HitSelect(TabControl t, Point pt)
        {
            try
            {
                for (int i = 0; i < t.TabCount; i++)
                {
                    if (t.GetTabRect(i).Contains(pt)) { t.SelectedIndex = i; return; }
                }
            }
            catch (Exception) { }
        }

        private static void HoverBand(UiTheme.TabBandPanel p, Point pt)
        {
            try
            {
                int idx = -1;
                TabControl t = p.Tab;
                if (t != null && !t.IsDisposed)
                {
                    for (int i = 0; i < t.TabCount; i++)
                    {
                        if (t.GetTabRect(i).Contains(pt)) { idx = i; break; }
                    }
                }
                if (p.HoverIndex != idx)
                {
                    p.HoverIndex = idx;
                    // 手型光标只在按钮上出现，避免整条横带看起来都“可点”——那是误导。
                    p.Cursor = idx >= 0 ? Cursors.Hand : Cursors.Default;
                    p.Invalidate();
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 整条标签头的自绘面板：盖住原生浅色标签条（余白/按钮间隙/上下沿线），并按
        /// GetTabRect 复绘标签按钮。为什么不继续用零散补丁条：原生绘制在按钮四周
        /// （间隙、选中框描边、按钮上下沿）都会冒出浅色，逐块补不如整体重绘可控。
        /// 坐标基准 = TabControl 客户区（面板恰好覆盖其标签头全高），所以
        /// GetTabRect 的坐标可直接绘制。点击/悬停命中沿用同一套矩形。
        /// </summary>
        internal sealed class TabBandPanel : Panel
        {
            internal TabControl Tab;
            internal TabBandColors C;
            internal int HoverIndex = -1;

            public TabBandPanel()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);
                TabStop = false;
                BackColor = Color.Transparent;  // 由 ThemeColors 随即覆盖
            }

            internal void ThemeColors(TabBandColors c) { C = c; }

            protected override void OnPaint(PaintEventArgs e)
            {
                TabControl t = Tab;
                if (t == null || t.IsDisposed || C == null) { return; }
                try
                {
                    using (SolidBrush band = new SolidBrush(C.Band)) { e.Graphics.FillRectangle(band, ClientRectangle); }
                    for (int i = 0; i < t.TabPages.Count; i++)
                    {
                        if (i >= t.TabPages.Count) { break; }
                        Rectangle r = t.GetTabRect(i);
                        bool sel = (i == t.SelectedIndex);
                        bool hover = (i == HoverIndex);
                        Color bg = sel ? C.SelBg : (hover && !sel ? C.HoverBg : C.BtnBg);
                        Color fg = sel ? C.SelFg : C.BtnFg;

                        // 圆角药丸：内缩一圈，上下左右都留白，选中项像一枚独立的标签
                        Rectangle pill = new Rectangle(r.Left + 3, r.Top + 3, Math.Max(4, r.Width - 6), Math.Max(4, r.Height - 6));
                        if (sel)
                        {
                            FillRound(e.Graphics, pill, RadiusBand, bg);
                            using (Pen pen = new Pen(C.SelLine))
                            {
                                e.Graphics.DrawPath(pen, RoundPath(new Rectangle(pill.Left, pill.Top, pill.Width - 1, pill.Height - 1), RadiusBand));
                            }
                        }
                        else if (hover)
                        {
                            FillRound(e.Graphics, pill, RadiusBand, bg);
                        }
                        else
                        {
                            using (SolidBrush b1 = new SolidBrush(bg)) { e.Graphics.FillRectangle(b1, pill); }
                        }

                        TextRenderer.DrawText(e.Graphics, t.TabPages[i].Text, t.Font, pill, fg,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                    }
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// 标签内容区外框覆盖层：与 TabControl 同尺寸，Region 挖掉页面区域，
        /// 只覆盖系统画在页面四周的那圈浅色边框；填充页面底色，并在底边
        /// 画 1px 分隔线，让内容区与下方终端之间是一条干净、等粗的细线。
        /// 页面区域被挖空，点击/输入事件照常落到页面里的控件上。
        /// </summary>
        internal sealed class TabFramePanel : Panel
        {
            internal Color Fill = Color.Black;
            internal Color Line = Color.Gray;

            public TabFramePanel()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);
                TabStop = false;
                // 不用 Transparent：Transparent 会让 WinForms 先画父容器背景，
                // 深色主题下若 Region 边界计算有偏差，边框区域就会露出白底。
                BackColor = Color.Black;
            }

            internal void ThemeColors(Color fill, Color line) { Fill = fill; Line = line; BackColor = fill; }

            protected override void OnPaint(PaintEventArgs e)
            {
                try
                {
                    // 整块填页面底色，把 TabControl 去原生主题后残留的系统浅色边框
                    //（左/上/右/下）彻底盖住，不再依赖 Region 边界是否精确。
                    e.Graphics.Clear(Fill);

                    // 四条边框线再兜底一次：即便 Region 挖空边界有 1~2px 偏差，
                    // 也能把残留的白色系统边框覆盖掉。
                    using (Pen pen = new Pen(Line))
                    {
                        int w = ClientSize.Width;
                        int h = ClientSize.Height;
                        if (w > 0 && h > 0)
                        {
                            e.Graphics.DrawLine(pen, 0, 0, w - 1, 0);           // 上
                            e.Graphics.DrawLine(pen, 0, h - 1, w - 1, h - 1);   // 下
                            e.Graphics.DrawLine(pen, 0, 0, 0, h - 1);           // 左
                            e.Graphics.DrawLine(pen, w - 1, 0, w - 1, h - 1);   // 右
                        }
                    }

                    // 底边即内容区与终端的交界：统一 1px 主题色分隔线（加强一遍，确保可见）
                    int y = ClientSize.Height - 1;
                    if (y > 0)
                    {
                        using (Pen pen = new Pen(Line))
                        {
                            e.Graphics.DrawLine(pen, 0, y, ClientSize.Width, y);
                        }
                    }
                }
                catch (Exception) { }
            }
        }

        internal sealed class TabBandColors
        {
            internal Color Band;
            internal Color BtnBg;
            internal Color BtnFg;
            internal Color SelBg;
            internal Color SelFg;
            internal Color SelLine;
            internal Color HoverBg;
            internal Color Sep;
        }
        // ---------------- 状态栏 / 工具栏 ----------------
        private static void StyleStatusStrip(StatusStrip ss)
        {
            ss.BackColor = _dark ? DBack : LSurface;
            ss.ForeColor = _dark ? DTextDim : LTextDim;
            // 系统渲染器（System）会无视 BackColor，浅色系统主题下画出一条灰白亮带；
            // 换成 Professional + 自定义配色表才吃我们的颜色。
            ss.RenderMode = ToolStripRenderMode.Professional;
            ss.Renderer = new StatusRenderer(_dark);
            ss.Padding = new Padding(8, 2, 8, 2);
            StyleToolStripImages(ss);
        }

        /// <summary>
        /// 状态栏渲染器：基类的边框绘制会画出一条系统白线（ProfessionalColorTable
        /// 里没有对应的覆盖点，ToolStripBorder 对它无效），这里整个接管边框，
        /// 改成 1px 主题色分隔线，与卡片描边、终端上沿保持同一粗细与色值。
        /// </summary>
        private sealed class StatusRenderer : ToolStripProfessionalRenderer
        {
            private readonly bool _dark;
            internal StatusRenderer(bool dark) : base(new StatusPalette(dark)) { _dark = dark; }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                try
                {
                    ToolStrip ts = e.ToolStrip;
                    if (ts == null || ts.Width <= 0) { return; }
                    using (Pen pen = new Pen(_dark ? DBorder : LBorder))
                    {
                        e.Graphics.DrawLine(pen, 0, 0, ts.Width, 0);
                    }
                }
                catch (Exception) { }
            }
        }

        /// <summary>状态栏配色表：把系统默认的蓝灰分隔线/边框换成主题色。</summary>
        private sealed class StatusPalette : ProfessionalColorTable
        {
            private readonly bool _dark;
            internal StatusPalette(bool dark) { _dark = dark; }

            public override Color ToolStripDropDownBackground { get { return _dark ? DBack : LSurface; } }
            // 状态栏顶边那一圈 1px 边框由渲染器按此色绘制；
            // 不覆盖时 Professional 默认给纯白，深色下就是一条刺眼白线。
            public override Color ToolStripBorder { get { return _dark ? DBorder : LBorder; } }
            public override Color StatusStripGradientBegin { get { return _dark ? DBack : LSurface; } }
            public override Color StatusStripGradientEnd { get { return _dark ? DBack : LSurface; } }
            public override Color MenuBorder { get { return _dark ? DBorder : LBorder; } }
            public override Color MenuItemBorder { get { return _dark ? DBorder : LBorder; } }
            public override Color MenuItemSelected { get { return _dark ? DCardHover : LCardHover; } }
            public override Color MenuItemSelectedGradientBegin { get { return _dark ? DCardHover : LCardHover; } }
            public override Color MenuItemSelectedGradientEnd { get { return _dark ? DCardHover : LCardHover; } }
            public override Color SeparatorDark { get { return _dark ? DBorder : LBorder; } }
            public override Color SeparatorLight { get { return _dark ? DBorder : LBorder; } }
            public override Color ImageMarginGradientBegin { get { return _dark ? DBack : LSurface; } }
            public override Color ImageMarginGradientMiddle { get { return _dark ? DBack : LSurface; } }
            public override Color ImageMarginGradientEnd { get { return _dark ? DBack : LSurface; } }
        }

        private static void StyleToolStrip(ToolStrip ts)
        {
            ts.BackColor = _dark ? DBack : Color.White;
            ts.ForeColor = _dark ? DText : LText;
            ts.ImageScalingSize = new Size(16, 16);
            ts.Padding = new Padding(4, 2, 4, 2);
            StyleToolStripImages(ts);
            // 菜单栏（MenuStrip）的悬停高亮由系统 ProfessionalRenderer 决定，
            // 深色主题下会出现浅色背景/看不清的文字，这里换成主题渲染器。
            if (ts is MenuStrip) { ts.Renderer = new MenuRenderer(_dark); }
        }

        /// <summary>菜单栏渲染器：统一悬停高亮色与文字色，避免深浅主题下看不清。</summary>
        private sealed class MenuRenderer : ToolStripProfessionalRenderer
        {
            private readonly bool _dark;
            internal MenuRenderer(bool dark) : base(new StatusPalette(dark)) { _dark = dark; }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                try
                {
                    using (SolidBrush brush = new SolidBrush(
                        e.Item.Selected ? (_dark ? DCardHover : LCardHover) : e.Item.Owner.BackColor))
                    {
                        e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                    }
                }
                catch (Exception) { }
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                try
                {
                    // 选中与非选中均统一用主题前景色，避免系统默认文字在主题背景上不清晰
                    e.TextColor = _dark ? DText : LText;
                    base.OnRenderItemText(e);
                }
                catch (Exception) { }
            }
        }

        // ---------------- 输入控件 ----------------
        private static void StyleInput(TextBoxBase tb)
        {
            if (tb.ReadOnly && !tb.BackColor.Equals(Color.Transparent))
            {
                // 只读框保留可辨识的浅灰底，深色下用更暗的卡片色
                tb.BackColor = _dark ? Color.FromArgb(0x2A, 0x2A, 0x2A) : Color.FromArgb(0xF0, 0xF0, 0xF0);
            }
            else
            {
                tb.BackColor = _dark ? DInput : Color.White;
            }
            tb.ForeColor = _dark ? DText : LText;

            // 圆角：Region 裁剪（原生直角边框会被裁掉圆角处的线头）
            HookRoundInput(tb);
            ApplyRoundRegion(tb, RadiusInput);
        }

        // 输入框底色刻意比面板更深一档，让下拉框/输入框在深色下有「凹陷」层次感；
        // 若用与面板相同的 DCard，边框几乎不可见，用户会看不清哪里能输入。
        private static readonly Color DInput = Color.FromArgb(0x1C, 0x1C, 0x1C);

        private static void StyleInput(ComboBox cb)
        {
            cb.BackColor = _dark ? DInput : Color.White;
            cb.ForeColor = _dark ? DText : LText;
            // ComboBox 没有 FlatAppearance 边框颜色可设。FlatStyle.Flat 的默认灰边框
            // 在深色下几乎不可见，用户看不出哪里能输入。挂一次 Paint 事件补画边框。
            cb.FlatStyle = FlatStyle.Flat;

            // 原生下拉箭头按钮用的是系统浅色主题，深色界面里是一块白块。
            // 尝试用 uxtheme 的深色主题变体（Win10 1809+；不支持的系统会静默回退）。
            ApplyNativeDarkTheme(cb, _dark);

            // 圆角：裁剪 + 自绘圆角描边（两种主题都需要）
            HookRoundCombo(cb);
            // 内缩 1px：原生 ComboBox 会画一圈系统浅色边框，深色下是刺眼的白边
            HookRoundInput(cb, 1);
            ApplyRoundRegion(cb, RadiusInput, 1);
            // 原生下拉箭头是浅色块，盖掉自绘
            CoverComboArrow(cb);
        }

        private static void StyleInput(NumericUpDown nud)
        {
            nud.BackColor = _dark ? DInput : Color.White;
            nud.ForeColor = _dark ? DText : LText;
            // 原生边框同样是系统浅色，直接关掉，靠圆角裁剪 + 底色分层
            try { nud.BorderStyle = BorderStyle.None; } catch (Exception) { }
            HookRoundInput(nud);
            ApplyRoundRegion(nud, RadiusInput);
            // 原生上下箭头是白色块，深色下盖掉自绘（浅色主题同样盖，避免系统灰块破坏圆角观感）
            CoverSpinner(nud);
        }

        /// <summary>
        /// 表格（DataGridView）配色。
        ///
        /// 原生 DataGridView 在深色主题下会保留「深色底 + 黑色单元格文字」，
        /// S50HTool 的扇区列表因此几乎看不清（截图中扇区行是灰黑对灰黑）。
        /// 这里把默认单元格、交替行、表头、选中行全部按主题显式设定。
        /// </summary>
        private static void StyleGrid(DataGridView g)
        {
            try
            {
                g.EnableHeadersVisualStyles = false;
                g.BorderStyle = BorderStyle.None;
                g.BackgroundColor = _dark ? DCard : Color.White;
                g.GridColor = _dark ? Color.FromArgb(0x3A, 0x3A, 0x3A) : Color.FromArgb(0xE4, 0xE4, 0xE4);

                g.DefaultCellStyle.BackColor = _dark ? DInput : Color.White;
                g.DefaultCellStyle.ForeColor = _dark ? DText : LText;
                g.DefaultCellStyle.SelectionBackColor = _dark ? Color.FromArgb(0x0E, 0x4A, 0x7A) : Color.FromArgb(0xCC, 0xE4, 0xF7);
                g.DefaultCellStyle.SelectionForeColor = _dark ? Color.White : LText;

                g.AlternatingRowsDefaultCellStyle.BackColor = _dark ? DSurface : Color.FromArgb(0xF7, 0xF9, 0xFC);
                g.AlternatingRowsDefaultCellStyle.ForeColor = _dark ? DText : LText;
                g.AlternatingRowsDefaultCellStyle.SelectionBackColor = g.DefaultCellStyle.SelectionBackColor;
                g.AlternatingRowsDefaultCellStyle.SelectionForeColor = g.DefaultCellStyle.SelectionForeColor;

                g.ColumnHeadersDefaultCellStyle.BackColor = _dark ? DSurface : Color.FromArgb(0xF0, 0xF2, 0xF5);
                g.ColumnHeadersDefaultCellStyle.ForeColor = _dark ? DText : LText;
                g.ColumnHeadersDefaultCellStyle.SelectionBackColor = g.ColumnHeadersDefaultCellStyle.BackColor;
                g.ColumnHeadersDefaultCellStyle.SelectionForeColor = g.ColumnHeadersDefaultCellStyle.ForeColor;

                g.RowHeadersDefaultCellStyle.BackColor = _dark ? DCard : Color.White;
                g.RowHeadersDefaultCellStyle.ForeColor = _dark ? DText : LText;
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 表单校验标记：把输入框标成「有效 / 无效」。
        ///
        /// 旧实现直接写 Color.Aquamarine / Color.Tomato（浅绿 / 浅红），
        /// 而深色主题下这些控件的前景色是浅色（DText）——浅底浅字几乎不可读，
        /// S50HTool 的十六进制框就是这样变成「白底白字」的。
        /// 这里按主题同时设置底色与前景色，两种主题下都保证足够对比度。
        /// </summary>
        internal static void MarkField(Control c, bool valid)
        {
            if (c == null) { return; }
            if (_dark)
            {
                c.BackColor = valid ? Color.FromArgb(0x17, 0x37, 0x2A) : Color.FromArgb(0x3E, 0x1C, 0x1C);
                c.ForeColor = valid ? Color.FromArgb(0xA7, 0xE8, 0xBE) : Color.FromArgb(0xFF, 0xB3, 0xB3);
            }
            else
            {
                c.BackColor = valid ? Color.FromArgb(0xD9, 0xF2, 0xE3) : Color.FromArgb(0xF8, 0xD7, 0xD7);
                c.ForeColor = Color.FromArgb(0x1F, 0x1F, 0x1F);
            }
            c.Invalidate();
        }

        /// <summary>把输入框恢复成主题默认配色（清空 / 未编辑状态）。</summary>
        internal static void ResetField(Control c)
        {
            if (c is TextBoxBase) { StyleInput((TextBoxBase)c); }
            else if (c is ComboBox) { StyleInput((ComboBox)c); }
        }

        /// <summary>
        /// 复选框 / 单选框适配。
        ///
        /// 坑：设 BackColor=Transparent 时，WinForms 会取**父容器**的背景色绘制。
        /// 本项目里这些控件的父容器（TableLayoutPanel / FlowLayoutPanel）在 resx 里
        /// 带**显式的 Color.White**，于是深色主题下就是「浅色文字 + 白底」——看不清。
        /// 解决办法：显式给一个与主题一致的实色背景，不依赖父容器继承。
        /// </summary>
        private static void StyleCheckBox(ButtonBase cb)
        {
            cb.ForeColor = _dark ? DText : LText;

            // 复选框/单选框只是设置项，不需要参与 Tab 键焦点序列。
            // 否则启动时它们会拿到焦点，WinForms 在深色主题下画一个刺眼的
            // 虚线/亮色焦点框围住整块文字（与分组框同款问题）。
            if (cb.TabStop) { cb.TabStop = false; }

            // 底色跟随父容器：父容器已被主题化（分组框=卡片色、页签页=面板色），
            // 直接继承即可，不会出现「卡片里嵌着一块深色方块」的高低错位。
            // 注意不能无条件写死颜色：RadioButton 也走这里，它们大多直接放在
            // TabPage 上（底色已经正确），一刀切会破坏已正确的控件。
            cb.BackColor = ParentBackOf(cb);
        }

        /// <summary>
        /// 终端输出区：深色主题下用深底浅字；浅色主题下沿用用户自定义颜色，
        /// 仅在对比度不足时兜底为深色（历史默认值是浅青字配浅蓝底，几乎不可读）。
        /// </summary>
        private static void StyleTerminal(RichTextBox rt)
        {
            // 等宽字体：让 libnfc 输出里的十六进制、扇区号对齐，便于逐行核对。
            // 主窗口终端（TerminalBox）例外：字体/字号由设置页统一管理，主题重刷时
            // 不能覆盖——否则系统切换深浅色后终端会退回 Consolas + 默认字号。
            if (!(rt is TerminalBox))
            {
                try { rt.Font = TerminalFont; } catch (Exception) { }
            }

            if (_dark)
            {
                rt.BackColor = Color.FromArgb(0x18, 0x18, 0x18);
                rt.ForeColor = Color.FromArgb(0xD4, 0xD4, 0xD4);
            }
            else if (Luma(rt.ForeColor) - Luma(rt.BackColor) < 70)
            {
                rt.ForeColor = Color.FromArgb(0x21, 0x21, 0x21);
            }

            // 终端区圆角：巨大的黑方块改成圆角面板，观感更整
            HookRoundInput(rt);
            ApplyRoundRegion(rt, RadiusCard);
        }

        private static double Luma(Color c) { return 0.299 * c.R + 0.587 * c.G + 0.114 * c.B; }

        /// <summary>按比例把颜色 a 向颜色 b 混合（保留 alpha），用于深色主题下提亮过暗的文字色。</summary>
        private static Color BlendColors(Color a, Color b, float t)
        {
            try
            {
                if (t < 0F) { t = 0F; } else if (t > 1F) { t = 1F; }
                return Color.FromArgb(a.A,
                    (int)(a.R + (b.R - a.R) * t),
                    (int)(a.G + (b.G - a.G) * t),
                    (int)(a.B + (b.B - a.B) * t));
            }
            catch (Exception) { return a; }
        }

        // ================= 系统主题探测 =================

        private static bool SystemIsLight()
        {
            // 优先级：调试环境变量 > 设置页「外观/主题」> Windows 应用主题。
            // 环境变量仅用于截图/自动化验证，不写入用户设置。
            try
            {
                string forced = Environment.GetEnvironmentVariable("M1TOOL_THEME");
                if (string.IsNullOrEmpty(forced)) { forced = Settings.Default.Theme; }
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