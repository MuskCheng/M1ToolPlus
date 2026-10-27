using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace M1ToolPlus
{
    /// <summary>
    /// 圆角化绘制（UiTheme 的 partial）。
    ///
    /// 原作界面是 VS2010 时代的直角控件：方按钮、方分组框、方输入框，
    /// 深色主题下配上方角白线边框，整屏显得生硬。这一部分不改 Designer 布局，
    /// 只在运行时把控件重绘成圆角，视觉向 Windows 11 Fluent 靠拢。
    ///
    /// 几个必须记住的坑：
    ///  · 圆角要 **Region 裁剪 + 自绘填充/描边** 两者配合：只裁剪不描边，
    ///    圆角处会残留直角边框的断头线；只描边不裁剪则整块仍是方的。
    ///  · 自绘时必须先用不透明填充盖掉原生绘制（文字、焦点虚线框）再重画文字，
    ///    否则文字重影；盖掉之后焦点框也随之消失，可自行画更干净的焦点环。
    ///  · 事件只挂一次（用表记录已挂载的控件）：换肤会重新执行样式代码，
    ///    无脑叠加会让 Paint/Resize 处理器越积越多。
    ///  · 分组框的原生标题是系统画的，自绘卡片时要整块覆盖再重画标题，
    ///    否则标题重影。
    /// </summary>
    internal static partial class UiTheme
    {
        // 圆角半径（像素）
        internal const int RadiusButton = 6;
        internal const int RadiusCard = 8;
        internal const int RadiusInput = 4;
        internal const int RadiusBand = 10;

        // 已挂载自绘处理器的控件，避免重复挂载
        private static readonly Hashtable _hookButton = new Hashtable();
        private static readonly Hashtable _hookGroup = new Hashtable();
        private static readonly Hashtable _hookInput = new Hashtable();
        private static readonly Hashtable _hookCombo = new Hashtable();


        // 按钮语义（primary/danger/muted）与基础底色，供自绘取用
        private static readonly Hashtable _btnKind = new Hashtable();
        private static readonly Hashtable _btnBase = new Hashtable();

        // ---------------- 基础绘制工具 ----------------

        /// <summary>生成圆角矩形路径。尺寸/半径为 0 时返回**空路径**（调用方须跳过绘制）。</summary>
        internal static GraphicsPath RoundPath(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            try
            {
                // 尺寸无效时绝不能 AddRectangle：零/负宽高会得到空路径，
                // 用它构造 Region 后在控件创建句柄时抛「Parameter is not valid」，
                // 且该异常发生在 WmCreate 里，会中断整页绘制与切页（血泪教训）。
                if (r.Width <= 0 || r.Height <= 0) { return p; }

                int max = Math.Min(r.Width, r.Height) / 2;
                if (max <= 0 || radius <= 0)
                {
                    p.AddRectangle(r);
                    return p;
                }

                // 半径不能超过短边的一半，否则 AddArc 会拿到负的宽高
                int rad = Math.Min(radius, max);
                int d = rad * 2;
                p.AddArc(r.Left, r.Top, d, d, 180, 90);
                p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
                p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
                p.CloseFigure();
            }
            catch (Exception) { }
            return p;
        }

        internal static void FillRound(Graphics g, Rectangle r, int radius, Color fill)
        {
            using (GraphicsPath path = RoundPath(r, radius))
            {
                if (path.PointCount < 3) { return; }
                using (SolidBrush br = new SolidBrush(fill))
                {
                    g.FillPath(br, path);
                }
            }
        }

        internal static void DrawRound(Graphics g, Rectangle r, int radius, Color border, float width)
        {
            using (GraphicsPath path = RoundPath(r, radius))
            {
                if (path.PointCount < 3) { return; }
                using (Pen pen = new Pen(border, width))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        /// <summary>给控件套圆角 Region（控件自身坐标系）。尺寸未确定时跳过，等 Resize 再套。</summary>
        internal static void ApplyRoundRegion(Control c, int radius)
        {
            ApplyRoundRegion(c, radius, 0);
        }

        /// <summary>
        /// 套圆角 Region，可整体内缩 inset 像素。
        /// ComboBox/NumericUpDown 会画一圈系统浅色边框，内缩 1px 正好把它裁掉。
        /// </summary>
        internal static void ApplyRoundRegion(Control c, int radius, int inset)
        {
            if (c == null) { return; }
            try
            {
                int w = c.Width - inset * 2, h = c.Height - inset * 2;
                // 布局尚未完成（0/极小尺寸）时不要设 Region：空 Region 会在
                // 控件创建句柄时由 Region.GetHrgn 抛 ArgumentException，
                // 异常从 WmCreate 抛出会打断整页绘制（表现为「整页只剩一张卡」）。
                if (w < 3 || h < 3)
                {
                    Region small = c.Region;
                    if (small != null) { c.Region = null; small.Dispose(); }
                    return;
                }

                using (GraphicsPath path = RoundPath(new Rectangle(inset, inset, w, h), radius))
                {
                    if (path.PointCount < 3) { return; }
                    Region nr = new Region(path);
                    Region old = c.Region;
                    c.Region = nr;
                    // 注意：Control.Region 保存的是**同一个对象引用**（不复制），
                    // 这里 Dispose(nr) 会让控件持有一个已释放的 Region，
                    // 之后创建句柄时 Region.GetHrgn 就会抛「Parameter is not valid」。
                    // 因此新 Region 交给控件持有，只释放被替换掉的旧对象。
                    if (old != null) { old.Dispose(); }
                }
            }
            catch (Exception) { }
        }

        // ---------------- 输入控件的圆角边框（由父容器绘制） ----------------

        // 为什么要父容器画：ComboBox / NumericUpDown 的圆角是用 Region 把最外 1px
        // 裁掉实现的（为了盖住原生浅色系统边框），于是「画在控件自己身上」的描边
        // 刚好落在被裁掉的那 1px 里 —— 永远看不见。控件被裁出的这一圈正好是父容器的
        // 背景，让父容器在这圈上补一条 1px 圆角线，输入框才有可见边界。
        private static readonly Hashtable _inputBorderChildren = new Hashtable();
        private static readonly Hashtable _inputBorderParents = new Hashtable();

        /// <summary>登记一个输入控件，让它的父容器负责画边框。</summary>
        internal static void HookInputBorder(Control c)
        {
            if (c == null || c.Parent == null) { return; }
            _inputBorderChildren[c] = true;

            Control parent = c.Parent;
            // 控件尺寸/位置变化后父容器画的那圈边框要跟着重画
            c.Resize += delegate(object s, EventArgs e) { try { parent.Invalidate(); } catch (Exception) { } };
            c.Move += delegate(object s, EventArgs e) { try { parent.Invalidate(); } catch (Exception) { } };
            c.VisibleChanged += delegate(object s, EventArgs e) { try { parent.Invalidate(); } catch (Exception) { } };

            if (_inputBorderParents.ContainsKey(parent)) { parent.Invalidate(); return; }
            _inputBorderParents[parent] = true;
            parent.Paint += delegate(object s, PaintEventArgs e) { PaintInputBorders((Control)s, e); };
        }

        /// <summary>父容器绘制：给登记过的可见子控件补 1px 圆角描边。</summary>
        private static void PaintInputBorders(Control parent, PaintEventArgs e)
        {
            try
            {
                Color line = _dark ? Color.FromArgb(0x4C, 0x4C, 0x4C) : Color.FromArgb(0xC6, 0xCE, 0xD6);
                using (Pen pen = new Pen(line))
                {
                    foreach (Control c in parent.Controls)
                    {
                        if (c == null || !c.Visible) { continue; }
                        if (!_inputBorderChildren.ContainsKey(c)) { continue; }
                        if (c.Width < 4 || c.Height < 4) { continue; }
                        using (GraphicsPath path = RoundPath(
                            new Rectangle(c.Left, c.Top, c.Width - 1, c.Height - 1), RadiusInput))
                        {
                            e.Graphics.DrawPath(pen, path);
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        /// <summary>两色混合（t=0 取 a，t=1 取 b）。</summary>
        internal static Color Mix(Color a, Color b, float t)
        {
            if (t < 0) t = 0; if (t > 1) t = 1;
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        /// <summary>取父容器底色（用于禁用态混色）。</summary>
        private static Color ParentBack(Control c)
        {
            try
            {
                if (c != null && c.Parent != null) { return c.Parent.BackColor; }
            }
            catch (Exception) { }
            return _dark ? DBack : LBack;
        }

        // ---------------- 圆角按钮 ----------------

        /// <summary>把按钮的语义与底色登记下来（自绘时要用）。</summary>
        internal static void RegisterButtonKind(Button b, string kind, Color baseColor)
        {
            _btnKind[b] = kind;
            _btnBase[b] = baseColor;
        }

        /// <summary>给按钮挂上圆角自绘（只挂一次）。</summary>
        internal static void HookRoundButton(Button b)
        {
            if (b == null || _hookButton.ContainsKey(b)) { return; }
            _hookButton[b] = true;
            b.Paint += delegate(object sender, PaintEventArgs e) { PaintRoundButton((Button)sender, e); };
            b.Resize += delegate(object sender, EventArgs e) { ApplyRoundRegion((Button)sender, RadiusButton); };
            // 句柄创建后尺寸才确定的控件（隐藏页里的按钮）在这里补一次圆角
            b.HandleCreated += delegate(object sender, EventArgs e) { ApplyRoundRegion((Button)sender, RadiusButton); };
            b.MouseEnter += delegate(object sender, EventArgs e)
            {
                Button bn = (Button)sender;
                bn.BackColor = bn.FlatAppearance.MouseOverBackColor;
            };
            b.MouseLeave += delegate(object sender, EventArgs e)
            {
                Button bn = (Button)sender;
                object o = _btnBase[bn];
                if (o is Color) { bn.BackColor = (Color)o; }
            };
        }

        private static void PaintRoundButton(Button b, PaintEventArgs e)
        {
            try
            {
                Rectangle r = new Rectangle(0, 0, b.Width, b.Height);
                string kind = _btnKind[b] as string;
                Color fill = b.BackColor;
                Color fg = b.ForeColor;
                Color parent = ParentBack(b);

                if (!b.Enabled)
                {
                    fill = Mix(fill, parent, 0.68F);
                    fg = Mix(fg, parent, 0.5F);
                }

                // 先用不透明圆角填充盖掉原生绘制（原生文字/焦点虚线框一并盖掉）
                FillRound(e.Graphics, r, RadiusButton, fill);

                Color border = ButtonBorderColor(kind, fill, b.Enabled);
                if (!border.IsEmpty)
                {
                    DrawRound(e.Graphics, new Rectangle(0, 0, b.Width - 1, b.Height - 1), RadiusButton, border, 1F);
                }
                if (b.Focused && b.Enabled)
                {
                    DrawRound(e.Graphics, new Rectangle(1, 1, b.Width - 3, b.Height - 3), RadiusButton,
                        _dark ? Color.FromArgb(0x8A, 0xC0, 0xFF) : Color.FromArgb(0x0D, 0x47, 0xA1), 1F);
                }

                TextRenderer.DrawText(e.Graphics, b.Text, b.Font, r, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
            catch (Exception) { }
        }

        /// <summary>按钮描边色：主按钮与填充同色（靠底色区分），次要/危险按钮给一圈描边。</summary>
        private static Color ButtonBorderColor(string kind, Color fill, bool enabled)
        {
            try
            {
                if (!enabled) { return Color.Empty; }
                if (kind == "muted") { return _dark ? DMutedBorder : LBorder; }
                // 色块按钮：按自身亮度往对比色方向混，浅色（近白）与深色色块都能看出边
                if (kind == "chip") { return Mix(fill, ContrastOn(fill), 0.45F); }
                if (kind == "danger") { return Mix(fill, Color.White, 0.18F); }
                return Mix(fill, Color.White, 0.22F);
            }
            catch (Exception) { return Color.Empty; }
        }

        // ---------------- 圆角卡片（分组框） ----------------

        /// <summary>给分组框挂上圆角卡片自绘（只挂一次）。</summary>
        internal static void HookRoundGroupBox(GroupBox g)
        {
            if (g == null || _hookGroup.ContainsKey(g)) { return; }
            _hookGroup[g] = true;
            g.Paint += delegate(object sender, PaintEventArgs e) { PaintRoundGroupBox((GroupBox)sender, e); };
        }

        private static void PaintRoundGroupBox(GroupBox g, PaintEventArgs e)
        {
            try
            {
                Rectangle r = new Rectangle(0, 0, g.Width, g.Height);
                Color card = g.BackColor;
                Color border = _dark ? DBorder : LBorder;
                int rad = RadiusCard;

                // 只把**四个角的小方块**用父容器底色盖掉——系统画的直角边框正好在这
                // 四角露头（圆角之外的角上是镂空的，系统边框就冒出来了）。
                // 千万别整块铺父容器底色：分组框是兄弟控件，铺满会连带擦掉
                // **先绘制**的兄弟分组框（绘制顺序靠后的会盖住先画的，实测整页只剩一张卡）。
                Color page = ParentBack(g);
                using (SolidBrush pb = new SolidBrush(page))
                {
                    e.Graphics.FillRectangle(pb, 0, 0, rad, rad);
                    e.Graphics.FillRectangle(pb, r.Width - rad, 0, rad, rad);
                    e.Graphics.FillRectangle(pb, 0, r.Height - rad, rad, rad);
                    e.Graphics.FillRectangle(pb, r.Width - rad, r.Height - rad, rad, rad);
                }

                // 再叠圆角卡片
                FillRound(e.Graphics, r, rad, card);
                DrawRound(e.Graphics, new Rectangle(0, 0, g.Width - 1, g.Height - 1), rad, border, 1F);

                string title = g.Text;
                if (!string.IsNullOrEmpty(title))
                {
                    int tw = MeasureTextWidth(title, g.Font);
                    int th = g.Font.Height;
                    Rectangle tr = new Rectangle(10, 3, tw + 6, th + 2);
                    TextRenderer.DrawText(e.Graphics, title, g.Font, tr, g.ForeColor,
                        TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                }
            }
            catch (Exception) { }
        }

        // ---------------- 无标题卡片（复制卡页第 4 格） ----------------

        private static readonly Hashtable _hookCard = new Hashtable();

        /// <summary>
        /// 把普通容器画成与分组卡同款的白底圆角卡片（没有标题）。
        /// 复制卡页第 4 格（勾选图标 +「尝试一下是否成功」+ 右下角「停止」）原本直接坐在
        /// 页签底色上，中间一大片空白、按钮孤零零悬在角落，看着像这页没做完。
        /// 套上卡片框后与前三张步骤卡同高同宽、四条边都对齐。
        /// </summary>
        internal static void HookCardPanel(Control c)
        {
            if (c == null || _hookCard.ContainsKey(c)) { return; }
            _hookCard[c] = true;
            c.Paint += delegate(object sender, PaintEventArgs e) { PaintCardPanel((Control)sender, e); };
        }

        private static void PaintCardPanel(Control c, PaintEventArgs e)
        {
            try
            {
                Rectangle r = new Rectangle(0, 0, c.Width, c.Height);
                int rad = RadiusCard;
                Color page = ParentBack(c);

                // 四角用页签底色盖掉（与分组卡同一手法：只盖角上的小方块，不整块铺）
                using (SolidBrush pb = new SolidBrush(page))
                {
                    e.Graphics.FillRectangle(pb, 0, 0, rad, rad);
                    e.Graphics.FillRectangle(pb, r.Width - rad, 0, rad, rad);
                    e.Graphics.FillRectangle(pb, 0, r.Height - rad, rad, rad);
                    e.Graphics.FillRectangle(pb, r.Width - rad, r.Height - rad, rad, rad);
                }

                FillRound(e.Graphics, r, rad, c.BackColor);
                DrawRound(e.Graphics, new Rectangle(0, 0, c.Width - 1, c.Height - 1), rad,
                    _dark ? DBorder : LBorder, 1F);
            }
            catch (Exception) { }
        }

        // ---------------- 圆角输入控件 ----------------

        /// <summary>给输入类控件挂上圆角（只挂一次）。</summary>
        internal static void HookRoundInput(Control c)
        {
            HookRoundInput(c, 0);
        }

        /// <summary>同上，可指定 Region 内缩量（ComboBox 需要 1px 裁掉原生浅色边框）。</summary>
        internal static void HookRoundInput(Control c, int inset)
        {
            if (c == null || _hookInput.ContainsKey(c)) { return; }
            _hookInput[c] = true;
            c.Resize += delegate(object sender, EventArgs e) { ApplyRoundRegion((Control)sender, RadiusInput, inset); };
            c.HandleCreated += delegate(object sender, EventArgs e) { ApplyRoundRegion((Control)sender, RadiusInput, inset); };
        }

        private static readonly Hashtable _hookSpinner = new Hashtable();

        /// <summary>
        /// 数字框的上下箭头是原生子窗口（msctls_updown32），深色主题下依然是白色块，
        /// 设 BackColor 也管不到它。这里用一块同底色的小面板盖住，自绘上下三角并转发点击。
        /// </summary>
        internal static void CoverSpinner(NumericUpDown nud)
        {
            if (nud == null) { return; }
            try
            {
                Panel cover;
                if (_hookSpinner.ContainsKey(nud))
                {
                    cover = _hookSpinner[nud] as Panel;
                    ApplyNativeDarkThemeDeep(nud, _dark);
                    PlaceSpinner(nud, cover);
                    return;
                }

                ApplyNativeDarkThemeDeep(nud, _dark);
                nud.HandleCreated += delegate(object s, EventArgs e) { ApplyNativeDarkThemeDeep(nud, _dark); };

                cover = new Panel();
                cover.TabStop = false;
                cover.Width = 16;
                nud.Controls.Add(cover);
                _hookSpinner[nud] = cover;
                cover.BringToFront();

                nud.Resize += delegate(object s, EventArgs e) { PlaceSpinner(nud, cover); };
                nud.Move += delegate(object s, EventArgs e) { PlaceSpinner(nud, cover); };

                cover.Paint += delegate(object s, PaintEventArgs e)
                {
                    try
                    {
                        Color fg = _dark ? DText : Color.FromArgb(0x33, 0x33, 0x33);
                        int cx = cover.Width / 2;
                        int half = cover.Height / 2;
                        // 上下两个三角的顶点方向必须朝外：上面的顶点在上、下面的顶点在下。
                        // 早期版本把顶点写反了（两箭头朝内相对），渲染出来是一个
                        // 「工」字形的实心块，看着就像坏掉的控件。
                        int w = 3;                     // 半宽：箭头宽 7px（接近系统原生 9px）
                        int h = 4;                     // 箭头高
                        int gap = 2;                   // 两个箭头之间的间隔（太小会粘成一坨）
                        // 抗锯齿：3~4px 的小三角不开 AA 会变成锯齿块，看着像坏控件
                        SmoothingMode smooth = e.Graphics.SmoothingMode;
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        int topBase = half - gap;      // 上箭头底边
                        int botBase = half + gap;      // 下箭头顶边
                        using (SolidBrush b = new SolidBrush(fg))
                        {
                            // 上箭头：底边在下，顶点在上
                            e.Graphics.FillPolygon(b, new[]
                            {
                                new Point(cx - w, topBase), new Point(cx + w, topBase),
                                new Point(cx, Math.Max(1, topBase - h))
                            });
                            // 下箭头：顶边在上，顶点在下
                            e.Graphics.FillPolygon(b, new[]
                            {
                                new Point(cx - w, botBase), new Point(cx + w, botBase),
                                new Point(cx, Math.Min(cover.Height - 2, botBase + h))
                            });
                        }
                        // 竖分隔线要 1px 硬边，恢复原来的模式再画
                        e.Graphics.SmoothingMode = smooth;
                        using (Pen p = new Pen(_dark ? DBorder : LBorder))
                        {
                            e.Graphics.DrawLine(p, 0, 1, 0, cover.Height - 2);
                        }
                    }
                    catch (Exception) { }
                };
                cover.MouseDown += delegate(object s, MouseEventArgs e)
                {
                    try
                    {
                        if (e.Y < cover.Height / 2) { nud.UpButton(); } else { nud.DownButton(); }
                        nud.Focus();
                    }
                    catch (Exception) { }
                };

                PlaceSpinner(nud, cover);
            }
            catch (Exception) { }
        }

        private static void PlaceSpinner(NumericUpDown nud, Panel cover)
        {
            try
            {
                if (nud == null || cover == null || cover.IsDisposed) { return; }
                cover.BackColor = _dark ? DInput : Color.White;
                // 高度用 Height-2（与 Region 裁切边界一致）：用 Height-3 会在框底露出
                // 一条原生白边，深色下非常明显（截图里箭头下面那道白横线）。
                cover.Bounds = new Rectangle(Math.Max(0, nud.ClientSize.Width - 18), 1, 17, Math.Max(8, nud.ClientSize.Height - 2));
                cover.BringToFront();
                cover.Invalidate();
            }
            catch (Exception) { }
        }

        private static readonly Hashtable _hookComboCover = new Hashtable();

        /// <summary>
        /// 盖掉 ComboBox 右端的原生箭头按钮（浅色块），自绘一个主题色箭头。
        /// 点击仍展开下拉列表（转发 DroppedDown）。
        /// </summary>
        internal static void CoverComboArrow(ComboBox cb)
        {
            if (cb == null) { return; }
            try
            {
                Panel cover;
                if (_hookComboCover.ContainsKey(cb))
                {
                    cover = _hookComboCover[cb] as Panel;
                    PlaceComboCover(cb, cover);
                    return;
                }

                cover = new Panel();
                cover.TabStop = false;
                cover.Width = 17;
                cb.Controls.Add(cover);
                _hookComboCover[cb] = cover;

                cb.Resize += delegate(object s, EventArgs e) { PlaceComboCover(cb, cover); };
                cb.Move += delegate(object s, EventArgs e) { PlaceComboCover(cb, cover); };

                cover.Paint += delegate(object s, PaintEventArgs e)
                {
                    try
                    {
                        Color fg = _dark ? DText : Color.FromArgb(0x33, 0x33, 0x33);
                        int cx = cover.Width / 2;
                        int cy = cover.Height / 2;
                        using (SolidBrush b = new SolidBrush(fg))
                        {
                            e.Graphics.FillPolygon(b, new[]
                            {
                                new Point(cx - 3, cy - 2), new Point(cx + 3, cy - 2), new Point(cx, cy + 2)
                            });
                        }
                    }
                    catch (Exception) { }
                };
                cover.MouseDown += delegate(object s, MouseEventArgs e)
                {
                    try
                    {
                        cb.Focus();
                        cb.DroppedDown = true;
                    }
                    catch (Exception) { }
                };

                PlaceComboCover(cb, cover);
            }
            catch (Exception) { }
        }

        private static void PlaceComboCover(ComboBox cb, Panel cover)
        {
            try
            {
                if (cb == null || cover == null || cover.IsDisposed) { return; }
                cover.BackColor = _dark ? DInput : Color.White;
                // 宽度按 125% DPI 实测：原生箭头按钮连左沿分隔线共占右侧 22px，
                // 只盖 17~21px 都会在箭头左边露出那条白竖线（逐像素定位到 x=523）。
                cover.Bounds = new Rectangle(Math.Max(0, cb.ClientSize.Width - 24), 1, 23, Math.Max(8, cb.ClientSize.Height - 2));
                cover.BringToFront();
                cover.Invalidate();
            }
            catch (Exception) { }
        }

        /// <summary>下拉框自绘圆角描边（ComboBox 没有可设的边框颜色，只能自己画）。</summary>
        internal static void HookRoundCombo(ComboBox cb)
        {
            if (cb == null || _hookCombo.ContainsKey(cb)) { return; }
            _hookCombo[cb] = true;
            cb.Paint += delegate(object sender, PaintEventArgs e) { PaintRoundCombo((ComboBox)sender, e); };
            // 句柄创建后再套一次原生深色主题（隐藏页里的下拉框句柄要等切页才创建）
            cb.HandleCreated += delegate(object sender, EventArgs e) { ApplyNativeDarkTheme((ComboBox)sender, _dark); };
        }

        /// <summary>
        /// Windows 10 1809+ 的 uxtheme 深色主题变体，让原生下拉箭头/边框走深色绘制。
        /// 老系统上主题名不存在，SetWindowTheme 会静默失败（不影响功能）。
        /// </summary>
        internal static void ApplyNativeDarkTheme(Control c, bool dark)
        {
            try
            {
                if (c == null || !c.IsHandleCreated) { return; }
                SetWindowTheme(c.Handle, dark ? "DarkMode_CFD" : "Explorer", null);
            }
            catch (Exception) { }
        }

        /// <summary>对控件及其子控件套用原生深色主题（数字框的上下箭头是子窗口）。</summary>
        internal static void ApplyNativeDarkThemeDeep(Control c, bool dark)
        {
            if (c == null) { return; }
            ApplyNativeDarkTheme(c, dark);
            try
            {
                foreach (Control ch in c.Controls) { ApplyNativeDarkThemeDeep(ch, dark); }
            }
            catch (Exception) { }
        }

        private static void PaintRoundCombo(ComboBox cb, PaintEventArgs e)
        {
            try
            {
                // 深色主题下原生「下拉箭头」按钮用的是系统浅色（SystemColors.Control），
                // 在深底上是一块刺眼的白方块。这里用输入框底色盖掉右端按钮区，
                // 再补一个自绘的小三角；点击行为仍由原生控件处理（点哪都会展开列表）。
                if (_dark)
                {
                    int aw = Math.Min(20, Math.Max(14, cb.Height));
                    if (cb.Width > aw + 6)
                    {
                        Rectangle ar = new Rectangle(cb.Width - aw - 1, 1, aw, cb.Height - 2);
                        using (SolidBrush b = new SolidBrush(DInput)) { e.Graphics.FillRectangle(b, ar); }
                        // 分隔竖线
                        using (Pen sp = new Pen(DBorder))
                        {
                            e.Graphics.DrawLine(sp, ar.Left, 3, ar.Left, cb.Height - 4);
                        }
                        // 三角（向下）
                        int cx = ar.Left + ar.Width / 2;
                        int cy = cb.Height / 2;
                        Point[] tri =
                        {
                            new Point(cx - 3, cy - 2),
                            new Point(cx + 3, cy - 2),
                            new Point(cx, cy + 2)
                        };
                        using (SolidBrush tb = new SolidBrush(DText)) { e.Graphics.FillPolygon(tb, tri); }
                    }
                }

                // 圆角描边不在这里画：控件自身的 Region 把最外 1px 裁掉了，
                // 画在这里会被裁没（这也是「下拉框只剩一个箭头」的根因）。
                // 边框统一由父容器在裁出的那圈上补画，见 HookInputBorder。
            }
            catch (Exception) { }
        }
    }
}
