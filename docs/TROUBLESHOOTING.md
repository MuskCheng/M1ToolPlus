# 常见问题排障（FAQ）

整理自原仓库高频 Issue 的已验证解决方案。硬件相关问题的 workaround 均来自
社区实测反馈，标注了对应 Issue 号。仅用于你有授权的卡片。

## 设备连接类

### PN532 检测设备/扫描很慢（Win10 超 100 秒）#26

原因：勾选"NewScan 扫描选项"后每次检测都会删除 `libnfc.conf`，
触发 libnfc 全量串口轮询，Win10 下极慢。

解决方案（任选其一）：

1. **本仓库 v1.7.1+ 已内置修复**：启动时自动恢复上次设备，无需再全扫
2. 手动 workaround（对旧版有效）：在 M1ToolPlus.exe 同目录新建
   `libnfc.conf`，写入正确 COM 口后**直接点"扫描卡片"，不要点"检测设备"**：

   ```
   allow_autoscan = false
   allow_intrusive_scan = false
   device.name = "NFC-Device"
   device.connstring = "pn532_uart:COM4:115200"
   ```

   COM4 改为设备管理器里 PN532 串口号

### PN532 一直显示"运行中"，连接不上 #39

- 先确认驱动（CH340 串口驱动）与接线（波特率 115200）
- 按上一条手动写 `libnfc.conf` 指定 COM 口，跳过"检测设备"直接"扫描卡片"
- 检测卡在"运行中"通常是 libnfc 在轮询其他 COM 口，等它超时或
  用上面的方法固定端口

### ACR122U 无法识别 / 无法打开设备 #9 #31

按社区验证顺序排查：

1. 安装 **122-libusb 驱动**（libusb-win32 filter 驱动，v1.6.4+ 支持 ACR122U）
2. 若仍失败，改装**旧版官方驱动**再试
3. 部分**新固件 ACR122U 已封锁 raw 接口**，此类设备无法用于 M1 破解/写卡
4. 换回原版 `libnfc(ACR122U).dll`（当前发行包内改名交换方案见
   docs/DEVELOPMENT.md 设备切换一节）

## 读写卡类

### 写卡报 "Mifare Authentication Failed" / CUID 卡写不进 #35

CUID 卡不能像 UID 卡那样用后门随便写，必须用**卡内已有的密钥**写入：

1. 用 hardnested 解出目标卡数据（或你已有该卡 dump），拿到正确 key
2. "写M1"时在 key.mfd 中选择包含该密钥的数据文件再写
3. 或用 MFF08 的"CUID修复"功能：选择包含卡内密钥的数据文件修复 CUID 卡
4. 手环类虚拟卡写坏后，最简单是用官方 APP 删掉重建一张

### 清空 CUID 空白卡 #40

1. 用密钥读出此卡数据，存为 `.dump` 文件，改后缀为 `.mfd`
2. 进入"高级操作模式"，选择 key.mfd → 选择刚才的文件
3. 点"清M1"，完成后重启软件

### 还原卡后变空白 #42

原卡写回请确认：使用的是该卡自己的密钥（不是默认密钥），且写完后
用"手动扫描"验证 UID/SAK 未变化。仍异常请带 dump 文件开新 Issue。

## Hardnested 类

### hardnested 采集慢 / 只采集部分扇区 #37 #28

- hardnested 攻击本身需要大量 nonce 采集，耗时取决于卡片与读卡器，
  PN532 采集时避免移动卡片
- 采集完成会输出 txt/nonce 文件，可上传云计算节点计算（软件日志会提示）
- 对比 Proxmark3：`hf mf hardnested`（社区反馈 PM3 采集更快）

## 界面语言

### 切换语言 #41 #38

设置页第一个下拉框选择：简体中文 / Русский / English，选择后自动重启生效。
本版本起内置英语界面。

下拉框的三个选项固定用**各语言自己的写法**：`简体中文` / `Русский` / `English`，
**不随界面语言翻译**——中文、英文、俄语界面下看到的都是这三项，外文用户才能认出自己的语言
（顺序 `0=简体中文`、`1=Русский`、`2=English`）。
首次运行或配置里没有 `Language` 时按简体中文处理并选中第 0 项。
注意：从旧版本升级时，若配置里仍是空的 `Language`，旧版会把它显示成「标准」；
本版本起该选项统一叫「简体中文」，与 `zh-cn` 的语义一致，避免被误认为「跟随系统」。

**2026-10-20 修复：切换后界面仍是中文（1.7.0.0 及更早）**

- 现象：下拉框选 Русский / English，程序确实自动重启，但界面依旧全中文；
  此时 `%LOCALAPPDATA%\XciCode\M1ToolPlus.exe_Url_*\1.7.0.0\user.config` 里的
  `Language` 已正确写入 `ru-RU` / `en-US`——说明切换、保存、重启都没问题。
- 根因（两个都在**构建侧**，与切换逻辑无关）：
  1. `build.ps1` 手工生成的卫星程序集叫 `en\M1ToolPlus.en.dll`；
     CLR 只按 `<程序集名>.resources.dll` 探测，这个文件永远不会被加载。
  2. 改名成 `M1ToolPlus.resources.dll` 后**仍然无效**：本机
     （Windows 11 + .NET Framework 4.8.10008）的 `ResourceManager`
     只按**旧式资源名** `<基名>.<文化>.resources` 在卫星里查找；
     卫星里只嵌现代名 `M1ToolPlus.Form1.resources` 时同样找不到。
- 修复：卫星输出为 `<文化>\M1ToolPlus.resources.dll`，并且每个资源
  **同时**嵌入现代名与旧式名（见 `scripts/build.ps1` 第 3 步）。
- 自检：`M1ToolPlus.exe --ui-info` 会把 `SettingLanguage` /
  `CurrentUICulture` / `TabPage1Text` / `ButtonListDevText` / `LanguageCombo`
  写进 `%TEMP%\m1tool-ui-info.txt`；语言生效时前几行应与所选语言一致，
  `LanguageCombo` 应形如 `0:简体中文` / `1:Русский` / `2:English`。

**2026-10-21 修复：下拉框空白 / 不回显当前语言**

- 现象：设置页语言下拉框是空的，用户无法判断当前是哪种语言。
- 根因：下拉框只在 `SelectedIndexChanged` 里写配置并重启，**构造时从未设置
  `SelectedIndex`**，所以界面上始终没有选中项。
- 修复：构造期调用 `ApplyLanguageSelection()` 按
  `CultureInfo.TwoLetterISOLanguageName` 选中对应项，并用
  `suppressLanguageChanged` 屏蔽这次赋值触发的事件——否则会在启动时
  立刻 `Settings.Save()` + `Application.Restart()`，形成重启死循环
  （实测 `--ui-info` 每次启动进程数恒为 1，无循环）。

**2026-10-21 补充：语言名不再按界面语言翻译**

- 现象：英语界面下中文选项显示 `Chinese`、俄语显示 `Russian`；俄语界面下显示
  `Китайский` / `Английский`——中文用户 / 俄语用户在别的界面语言里认不出自己的语言。
- 修复：`comboBox1.Items` 由设计器固定为 `简体中文` / `Русский` / `English`
  （顺序 `0/1/2` 与 `ApplyLanguageSelection()` 一致），并删除
  `Form1.resx` / `Form1.zh.resx` / `Form1.en.resx` / `Form1.ru.resx`
  里对应的本地化条目。
- 自检：`--ui-info` 新增 `LanguageItems=简体中文|Русский|English`，
  中文 / 俄语 / 英语三种界面下都必须完全一致。

**2026-10-21 修复：俄语窗口比中文更大、卡片被拉宽**

- 现象：切到俄语后主窗口变成 1348×693（中文为 900×839），
  复制模式的三张卡片被拉到两倍宽，卡片之间的箭头错位。
- 根因：`Form1.ru.resx` 是原项目用俄语设计器保存的，里面残留了整套
  **旧版俄语布局**：`$this.ClientSize = 1098, 456` 加上 122 个 `Size`、
  36 个 `Location`。这些值会在 `ApplyResources` 时覆盖新布局。
- 修复：清空 `Form1.ru.resx` 里的全部几何条目，只保留文案，使其与
  `Form1.zh.resx` / `Form1.en.resx` 一致；布局统一由设计器基准值 +
  运行期 `UiTheme.ApplyAdaptiveLayout` / 流式布局计算。
- 附带修正：`build.ps1` 的新鲜度检查不再把语言专用 resx
  （`Form1.ru.resx` 等）算作主程序输入——它们只影响卫星程序集，
  以前只改俄语文案会误报「产物早于源文件」并中止构建。

## 界面主题与显示清晰度

### 设置页找不到「外观 / 主题」

- 该设置组是本版本新增的，位于「软件设置」页、语言/地区等设置组的右侧；
  `1.7.0.0` 及更早的旧构建没有这个组。
- 组由 `Form1.InitializeThemeSelector()` 运行时创建，不改 Designer 布局。
  若整个组不显示，先确认用的是 `M1ToolPlus\bin\Release\M1ToolPlus.exe`
  （不是 `artifacts` 里的旧包），再重新运行 `.\scripts\build.ps1`。

### 切换主题后重启又变回去

主题来源优先级（高到低）：

1. 环境变量 `M1TOOL_THEME=light|dark`（仅调试/截图用，正常使用请删除）
2. 设置页「外观 / 主题」→ `user.config` 的 `Theme`
   （空 = 跟随系统，`dark` = 深色，`light` = 浅色）
3. Windows 设置 → 个性化 → 颜色 → 「选择默认应用模式」

若手动选了浅色却被环境变量强制成深色，先检查是否设置了 `M1TOOL_THEME`。
用户配置在 `%LOCALAPPDATA%\XciCode\M1ToolPlus.exe_Url_*\<版本号>\user.config`
（如 `1.8.1.0`；版本升级后由 `Settings.Upgrade()` 自动迁移）。

### 窗口固定大小，不支持用户拖动缩放

主窗口保持 `FixedSingle` 固定尺寸，不允许用户拖动边框缩放，这是有意设计：
避免拖动缩放时上百个控件逐帧重绘导致的卡顿、白边与撕裂；功能区初始宽度若不足，
仅在启动时一次性撑宽。若需要更大窗口，请调整系统缩放比例（见下）。

### DPI / 显示清晰度（100% / 125% / 150% 缩放）

进程优先声明 Per-Monitor DPI Aware V2（Win10 1703+ `SetProcessDpiAwarenessContext`；
旧系统回退系统级 `SetProcessDPIAware()`），任何缩放比例与多显示器组合下界面都按
**物理像素 1:1** 渲染、不会被系统位图拉伸，文字边缘清晰、不会发虚；界面字号使用像素值
（等价 9pt 微软雅黑），在任意 DPI 下渲染出相同的物理像素高度，控件坐标固定，
绝对定位布局不会错位。

- 若在 125%/150% 缩放下觉得窗口偏小，请调整 Windows「设置 → 系统 → 显示 → 缩放」
  到更高比例，窗口物理尺寸不变，但系统会按比例放大整个桌面（窗口在屏幕上占比
  会相应变小，文字本身保持清晰）。
- 若确认缩放为 100%、显示器为原生分辨率仍觉得文字发虚，请检查：
  1) 确认运行的是最新构建的 exe（旧包可能未声明 DPI 感知，会被系统拉伸）；
  2) Windows「字体平滑」是否关闭（控制面板 → 系统属性 → 性能设置 →「平滑屏幕字体边缘」
     需开启，ClearType 调谐器可按屏幕情况微调）；
  3) `M1ToolPlus.exe --ui-info` 中的 `DeviceDpi` 应为 96（若为 120/144，说明系统缩放实际
     不是 100%）。

## 高级工具（S50HTool / HardNested）

### S50HTool 十六进制框「浅底浅字」、扇区列表看不清（1.7.0.0）

**现象**：
1. 载入卡片后，十六进制 / KeyA / KeyB / 权限下拉框变成 Aquamarine（浅绿）或 Tomato（浅红），
   而文字是深色主题的浅色 `#E6E6E6`——浅底浅字，几乎无法阅读；
2. 左侧「扇区列表」是深色底 + 黑色单元格文字，扇区行（「扇区 N 空扇区」）几乎看不清。

**根因**：
1. `block0Edit_Validating` / `keyAEdit_Validating` / `comboBox1_Validating` 直接写
   `Color.Aquamarine` / `Color.Tomato`，与主题无关；主题给这些控件的前景色是浅色，
   于是形成浅底浅字。
2. `buttonSaveSectorEdit_Click` 用「背景色是否等于 Aquamarine」判断输入是否有效，
   颜色一改就误判。
3. `DataGridView` 的背景被主题化，但 `DefaultCellStyle.ForeColor` 仍是黑色——深底黑字。

**修复**：
- 新增 `UiTheme.MarkField(Control,bool)` / `ResetField(Control)`：按当前主题**同时**设置底色与前景色
  （深色：有效 `#17372A` / 文字 `#A7E8BE`，无效 `#3E1C1C` / 文字 `#FFB3B3`；
  浅色：有效 `#D9F2E3`，无效 `#F8D7D7`，文字统一 `#1F1F1F`）。
- 保存校验改用 `IsHex()` 正则，与颜色彻底解耦；`reloadEdit(-1)` 调 `ResetField` 恢复默认配色。
- 新增 `UiTheme.StyleGrid(DataGridView)`：显式设置单元格、交替行、表头、选中行的底色与文字色。
- `FormHardNes` 的校验配色同步改用 `MarkField`。

**自检**：打开 S50HTool → 载入 dump → 有效字段应为深绿底浅绿字、无效字段深红底浅粉字；
扇区列表每行文字清晰可读。探测脚本可直接读控件颜色：
`block0Edit.BackColor=FF17372A / ForeColor=FFA7E8BE`、错误字段 `FF3E1C1C / FFFFB3B3`。

### 点 HardNested 弹「Unable to cast ... RadioButton ... CheckBox」

**现象**：点击主界面「HardNested」立即弹出
`Unable to cast object of type 'System.Windows.Forms.RadioButton' to type 'System.Windows.Forms.CheckBox'`，
对话框打不开。

**根因**：`UiTheme.StyleControl` 把 `RadioButton` 也交给 `StyleCheckBox((CheckBox)c)` 处理。
`RadioButton` 与 `CheckBox` 是**兄弟类型**（都继承 `ButtonBase`），没有继承关系，强制转换必然抛异常。

**修复**：`StyleCheckBox` 参数类型改为 `ButtonBase`，`CheckBox` / `RadioButton` 分支都传
`(ButtonBase)c`；主题适配逻辑（前景色、取消 TabStop、底色跟随父容器）对两者通用。

**自检**：点「HardNested」应正常弹出「初始化HardNested参数」窗口，
标题栏下方能看到 KeyA / KeyB 单选按钮，不出现任何异常框。

## 终端输出

### 写卡过程没有日志，结束才一次性出现（1.7.0.0）

**现象**：写卡时终端一直空白，直到进程结束才瞬间出现
`需要写入 64 个块 |.x...|`、`完成！写入了 63 / 64 个块.`、
`nfc_initiator_transceive_bytes: Mifare Authentication Failed` 等全部内容。

**根因**：`nfc-mfclassic` 的进度用 `printf("%c", '.')` 逐字符输出、**不换行**，
只在扇区边界 `fflush(stdout)`，整条进度直到最后的 `"|\n"` 才有换行。
`ToolProcess.Pump` 原来用 `StreamReader.ReadLine()`——它必须等到 `\n` 才返回，
于是写卡全程读不到内容，进程结束时才一次性返回。

**修复**：
- `ToolProcess.Pump` 改为**按字符读取**：`\n` 定稿一行；超过 250ms 仍未换行的内容
  以 `ProgressPercentage = -1`（`ToolProcess.ProgressLiveLine`）上报，约每 120ms 刷新一次；
  流结束时把残余内容定稿。`\r` / CRLF 均做了处理。
- `Form1.default_rpt` 收到 -1 时走 `logLive()`：从 `liveLineStart` 到末尾**原地替换**，
  进度条逐格增长，不会变成每个字符一行。
- `logAppend()` 写正式日志前先删除临时进度行。
- **坑**：终端 `richTextBox1.ReadOnly = true`，此时 `SelectedText = ""` 会被系统
  **静默忽略**（插入 / 替换却有效），导致临时行删不掉、正式行接在它后面形成
  `|...| |...|` 重复进度条。删除前必须临时关闭 `ReadOnly`。
- `buttonConClr_Click` 清空终端时同步把 `liveLineStart` 复位为 -1。

**自检**：用伪造的 `nfc-scan-device.exe`（每 140ms 输出一个进度字符、每 4 个字符 flush、
整条进度不换行）点「检测连接」：终端最后一行应逐格变长，结束时只剩**一行**完整进度条，
后面紧跟「完成！」与设备列表。自动化脚本：`scripts\test-livelog.ps1`
（`growing=True singleLine=True` → PASS；脚本会自动准备测试副本，不影响 bin\Release）。

### 点「检测连接」后终端一片空白（1.7.0.0 及更早）

**现象**：设备已连接，点「检测连接」，下方日志框完全没有内容，状态栏显示「运行中」。

**两个叠加的根因**：
1. **布局**：`UiTheme.ApplyAdaptiveLayout` 把终端 `RichTextBox` 设为 `Dock.Fill`，
   而它在 z 序里位于 `TabControl` 之下。Fill 把它拉成 `(0,0,整窗)`，
   日志从控件顶部开始写——前 300 多像素正好被页签盖住，用户看到的就是空白。
   自动回归里的 `TerminalTextLength=28` 证明文字一直在写，只是看不见。
2. **输出回显**：`list_dev` 为了解析设备名，把 stdout 行交给了 `lineHandler`，
   但处理器只做正则匹配、没有再把行转发给界面（`ToolProcess` 重构时漏掉了上游的
   `b.ReportProgress(0, _e.Data)`），所以 `nfc-scan-device` 的原始输出永远不显示。

**修复**：
- 终端区显式放在「页签下方、状态栏上方」，四边锚定跟随窗口缩放（不再用 Dock.Fill）。
- 设备扫描的每一行先回显到终端，再做设备名解析。
- 「开始执行检测设备」提示补充「扫描可能需要数十秒，请稍候」——
  `nfc-scan-device` 的 stdout 是块缓冲的，扫描结束前可能一直没有新行。

**顺带修复**：`Form1.ActiveForm.Text = ...` 在窗口未激活时 `ActiveForm` 为 null，
点击按钮会抛 `NullReferenceException`（弹出「.NET Framework 未处理异常」）。
已全部改为 `this.Text`（共 20 处）。

### 终端中文乱码（`ʹ��`、`δ���ֿ�Ƭ`）

- `nfc-bin` 里的 libnfc 系工具是中文定制版，提示文字按 **GBK(936)** 写成字节，
  例如「使用libnfc」= `CA B9 D3 C3`、「未发现卡」= `CE B4 B7 A2 CF D6 BF A8`。
- 在英文/俄文 Windows 上按系统 ANSI 解码会乱码；**按 UTF-8 解码同样错**——
  `CA B9` 恰好是合法的 UTF-8 两字节序列，会解成 `ʹ`。
- `ToolProcess` 现在固定用 **936** 解码（取不到时退回系统 ANSI），
  在任何系统区域设置下都能正确显示中文。
- 自检：`nfc-scan-device.exe` 的输出第一行应为「… 使用libnfc 1.7.1」，
  第二行「找到 N 个NFC设备:」。

### 终端字体不是微软雅黑 /「文字大小」与 Ctrl+滚轮不同步

- **现象**：
  1. 终端英文是 Consolas、中文靠回退，中英混排高度不一致；
  2. 设置页「终端文字大小」的数值和终端实际显示对不上；
  3. 在终端上 Ctrl+滚轮字变大了，设置页的数字却**不变**。
- **根因**：
  1. `UiTheme.TerminalFont` 写死 Consolas（无中文字形）；
  2. `MainCLIFontSize` 只在改数字时保存，**启动时从不读取**（死设置）；
  3. RichTextBox 自带的 Ctrl+滚轮改的是 `ZoomFactor`（显示缩放），不是 `Font`，
     所以字变大了而字号值不变；`ZoomFactor` 还会和字号叠加成双重缩放。
- **修复**（1.7.0.0 之后）：
  - 新增 `TerminalBox`：在 `WndProc` 拦截 `WM_MOUSEWHEEL`，Ctrl 按下时转成字号 ±1 磅并吃掉消息
    （`ZoomFactor` 恒为 1）；Ctrl 判定用消息里的 `MK_CONTROL` 位。
  - `numericCLIFontSize` 是唯一权威值：滚轮 / 手改数字都走 `ValueChanged` → 改字体 + 存设置。
  - 终端字体改为 **Microsoft YaHei UI**，字号 pt→px（×96/72）按像素建字体，保持 1:1 渲染。
  - 默认 10 磅；`TerminalFontVersion` 一次性把老配置的 `MainCLIFontSize` 提到 10。
  - 主题重刷（系统深浅色切换）跳过 `TerminalBox` 的字体，避免被打回 Consolas。
- **自检**：`M1ToolPlus.exe --ui-info` →
  `TerminalFontSize=10pt`、`TerminalFontFamily=Microsoft YaHei UI`、
  `TerminalFontPx=… h=17`、`TerminalZoom=1`；滚轮后 `TerminalFontSize` 变、`TerminalZoom` 不变。

### DiffTool 窗口被撑成 900×640、标题显示 DiffTool（1.7.0.0）

**现象**：打开「差异比较」后窗口约 900×640，A / B / 比较按钮与输出框挤在左上角，
右侧和下方是大片空白；标题栏显示英文 `DiffTool`。

**根因**：
1. `UiTheme.ApplyAdaptiveLayout` 原本只为主窗口（`FixedSingle`）设计，却对
   `FormDiff`（`FixedDialog`）也设置了 `MinimumSize=(900,640)`。`FormDiff` 是
   `AutoSize=true`，内容实际只有 370×456，窗体被下限强行撑大后，内容仍停在左上角。
2. 标题来自 `FormDiff.resx` / `FormDiff.zh.resx` 的 `$this.Text`，两处都还是 `DiffTool`。

**修复**：
- `ApplyAdaptiveLayout` 现在只处理 `FixedSingle` 主窗口；`FixedDialog` 工具窗/对话框
  保留 Designer 按内容计算的尺寸。S50HTool 也因此去掉了右侧同样的空白，窗口从
  900×640 收紧到内容实际需要的约 738×650。
- 标题改为「差异比较」（英文 `Compare Dumps`，俄文 `Сравнение дампов`）。

**自检**：DiffTool 客户端约 398×383，A / B / 比较按钮与输出框铺满窗口，
标题栏为「差异比较」。

### S50HTool 扇区列表左侧的空白窄竖条（1.7.0.0）

**现象**：扇区列表最左侧有一条约 40px 宽、贯穿 16 行、没有任何文字的窄竖条
（用户截图红框圈出的位置）。

**这是什么**：`DataGridView` 的**行头**（RowHeader），默认 `RowHeadersVisible=true`。
它只显示当前行的小箭头/选中状态，不承载业务数据。S50HTool 只有一列「扇区」，
扇区号已经写在单元格文本「扇区 N: 空扇区/有数据」里，所以行头本来就是空的，
不是有内容没显示出来。

**修复**：`dataGridView1.RowHeadersVisible=false`，把宽度让给扇区信息；
`SelectionMode` 仍是 `FullRowSelect`，点击任意单元格照常选中整行并加载扇区。

**自检**：列表左侧不再有空白竖条，单元格从 GroupBox 左内边距开始。

## 构建相关

### 报 MSB3086 / 缺少 al.exe

不影响使用——用 `scripts\build.ps1` 构建，会自动用 csc 绕过生成
本地化程序集。详见 docs/DEVELOPMENT.md。
