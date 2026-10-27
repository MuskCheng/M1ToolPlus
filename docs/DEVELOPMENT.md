# M1ToolPlus 开发文档

## 技术栈

- C# / .NET Framework 4.0 Client Profile / x86 / WinForms
- VS2010 解决方案格式（`M1ToolPlus.sln`），可用 VS2022 Build Tools 构建
- 唯一 NuGet 外部依赖：`Newtonsoft.Json.dll`（仓库内直接附带二进制，非 packages.config 管理）

## 构建

```powershell
.\scripts\build.ps1              # Release 构建
.\scripts\build.ps1 -Configuration Debug
.\scripts\build.ps1 -Pack        # 打包到 artifacts\M1ToolPlus-local-build.zip
.\scripts\build.ps1 -Clean       # 清理重建
.\scripts\fetch-deps.ps1         # 回收 nfc-bin 运行依赖
```

### al.exe 问题（重要）

原工程依赖 Windows SDK 的 `al.exe` 生成中/俄文卫星程序集（`ru\`、`zh\` 目录；v1.9.0 起只剩 `zh\`）。
未安装 Windows SDK 时 MSBuild 报 `MSB3086`。`build.ps1` 的处理：

1. 自动搜索 `al.exe`；找到 -> 走标准 MSBuild 全量构建
2. 找不到 -> 仍执行 `/t:Build`（主 exe 会成功产出，仅卫星步骤失败），然后用
   `csc` 手工把 `obj\...\*.zh.resources` 编译成
   `zh\M1ToolPlus.resources.dll`（嵌入 AssemblyCulture）

> 注意：不要用 `/t:CoreCompile` 单独编译，它会跳过引用解析，报一堆
> CS0518/CS0246（System.Object 未定义）——必须 `/t:Build`。

### 运行

`bin\Release\` 下需要 `nfc-bin\`（`fetch-deps.ps1` 回收）才能实际读卡。
GUI 程序启动后主窗口标题为 `M1ToolPlus`，即为冒烟测试通过。

## 代码架构

```
Program.cs          入口 + 单实例 Mutex
Form1.cs            主窗口，几乎全部业务逻辑（1490 行，待拆分）
Form1.Designer.cs   主窗口布局（1687 行，绝对定位，勿改布局）
ToolProcess.cs      子进程启动 + GBK(936) 输出解码封装
TerminalBox.cs      终端控件：Ctrl+滚轮转字号步进（禁用 RichEdit 的 ZoomFactor 缩放）
UiTheme.cs          运行时 UI 主题（跟随系统 / 深色 / 浅色，改配色只改这里）
ClassMifareS50.cs   S50/S70 卡数据模型 + MCT 格式转换
FormHTool.cs        S50HTool（卡数据分析/修复/转 MCT）
FormDiff.cs         卡数据对比
FormHardNes.cs      Hardnested 攻击对话框
FormMFF08.cs        mff08 工具对话框
SelectLanguage.cs   语言选择
GitHubUpdate.cs     GitHub Releases 检查更新（读 app.config 的 GitHubR 设置）
```

### UI 主题（UiTheme.cs）

- **运行时集中应用**，不改 Designer/resx 布局：本界面是 VS2010 时代的绝对定位布局，改坐标会导致控件错位
- **跟随系统深浅色**：读 `AppsUseLightTheme` 注册表值，监听 `WM_SETTINGCHANGE` 即时切换
- **手动主题**：设置页「外观 / 主题」可选 `跟随系统` / `深色` / `浅色`，保存在
  `Settings.Theme`（`""` = 跟随系统、`dark` / `light` = 强制）；切换后立即刷新所有已打开窗体，不重启
- **主题优先级**：`M1TOOL_THEME` 环境变量（仅调试/截图用） > `Settings.Theme` > Windows `AppsUseLightTheme`
- **窗口固定大小**：主窗口保持 `FixedSingle`，不再允许用户拖动边框缩放，避免拖动缩放时上百个
  控件逐帧重绘导致的卡顿与白边；功能区初始宽度若不足，仅在启动时一次性撑宽。
- **DPI / 显示清晰度**：进程在 `Program.Main` 中优先声明 **Per-Monitor DPI Aware V2**
  （Win10 1703+ `SetProcessDpiAwarenessContext`；旧系统回退 `SetProcessDPIAware()`），
  任何缩放比例（100%/125%/150%）与多显示器组合下都按**物理像素 1:1** 渲染，系统不再做
  GDI 位图拉伸，文字边缘清晰、不会发虚。界面字号使用像素值 `UiFontPx`（等价 9pt 微软雅黑），
  在任意 DPI 下都渲染出相同的物理像素高度，控件坐标固定，绝对定位布局不会错位；
  固定像素 UI 在高缩放显示器上物理尺寸偏小（不随 DPI 放大），是「清晰优先」的取舍。
- **右下角缩放把手**：`StyleStatusStrip` 中显式 `SizingGrip = false`。该属性默认开启——
  `StatusStrip` 自绘点阵把手，并在 `WM_NCHITTEST` 中对把手区域返回 `HTBOTTOMRIGHT`
  使鼠标变缩放光标；这是子控件内部处理的消息，主窗口级的 NCHITTEST 拦截收不到，
  必须在源头关闭，否则 coloring/覆盖绘制手段都只能盖住图形、盖不住光标。
- **按能力降级**：Win11 21H2+ 圆角、22H2+ Mica，Win10 回退纯色；所有 DWM 调用失败均静默忽略
- 改配色只改 `UiTheme.cs`
- **终端字体/字号例外**：主窗口终端是 `TerminalBox`，字体与字号由设置页 `numericCLIFontSize`
  统一管理（`UiTheme.StyleTerminal` 会跳过它），主题重刷不得覆盖；Ctrl+滚轮 = 字号 ±1 磅，
  `ZoomFactor` 恒为 1。`--ui-info` 的 `TerminalFont*` 行可用于自检。
- 截图验证：`.\scripts\screenshot.ps1 -Out <png>`（脚本需 UTF-8 BOM）

### 工作模式（Form1）

- **子进程调用**：所有 NFC 操作通过 `Process.Start("nfc-bin/xxx.exe")` 调用
  libnfc 工具链（nfc-list / nfc-mfclassic / nfc-mfsetuid / mfoc / mfcuk /
  libnfc_hardnested / mfdetect / mff08），GUI 解析 stdout 输出刷新界面
- **后台执行**：`BackgroundWorker`，进度码约定：
  - `100` = 任务结束（普通）
  - `101` = 任务结束且需要处理 dump 文件（自动保存到 `auto_keys\` 或弹保存框）
- **配置**：`libnfc.conf` 由 `writeConfig()` 写入（设备 connstring），
  `NewScan` 设置会先删除旧配置触发重扫
- **设备切换**：ACR122U / PN532 通过交换 `nfc-bin\libnfc(ACR122U.dll)` 等
  dll 文件名实现（`File.Move` 三交换，见 Form1.cs ~1136 行）

### 本地化

- 中性资源 = 简体中文（`Form1.resx` 界面文本即中文）
- 卫星：只剩 `Form1.zh.resx`（中文覆盖）。**v1.9.0 起移除英文/俄文**：
  `*.en.resx` / `*.ru.resx` 已删除（原英语文案移植自 xavave/MifareOneTool-English fork），
  `build.ps1` 的 csc 卫星步骤也只生成 `zh`，CI 只校验 `zh\M1ToolPlus.resources.dll`
- 运行时在 `Form1()` 构造里固定 `CurrentUICulture = zh-CN`（`Form1.UiCulture`），
  不再读 `Settings.Language`——系统区域是英文/俄文时也一律显示中文，不会落到无资源可用的回退态
- 文案全部经 `Properties\Resources` 强类型访问（`Resources.xxx`）
- 设置页「语言和地区」分组与语言下拉框已随多语言一起移除（`comboBox1` /
  `ApplyLanguageSelection` / `ComboBox1_SelectedIndexChanged` 均不存在了）。
  若将来要恢复多语言：重新加入 `.xx.resx` 并注册进 csproj → `build.ps1` 的
  `foreach ($cult in @("zh"))` 加回语言代号 → 恢复下拉框与按索引切文化的逻辑
- 废弃：`SelectLanguage.cs`（MultiLang 模板自带的语言选择窗体，从未被引用）已删除

### 设置项（Properties.Settings）

`GitHubR`(应用配置) + 用户级：`Language` `Theme` `AutoABN` `WriteCheck`
`AutoLoadUidKey` `MainCLIColor` `MainCLIFontSize` `TerminalFontVersion` `DefIsAdv` `AutoSave`
`HardLowCost` `LastTryKey` `NewScan` `MultiMode` `CuidKeyOver`

## 测试

分两层：

**① 单元测试（无硬件，改动后必跑）**

```
.\scripts\build.ps1 -Test
```

构建主程序 + `tests\M1ToolPlus.Tests`（控制台断言程序，无第三方测试框架，退出码即成败），
当前 19 个用例覆盖纯逻辑：`S50` 的 BCC/访问位校验与 mfd/mct 往返、`Utils` hex 工具、
`ReleaseInfo.ParseVersion`、`GitHubUpdate.VerifyPackage`（SHA256 正/反/无 digest）、
`CardToolFlow.BuildClassicArgs/BuildPsi`。访问内部类型靠主工程
`Properties\AssemblyInfo.cs` 的 `[assembly: InternalsVisibleTo("M1ToolPlus.Tests")]`。

新增可测逻辑时按同样方式在 `Tests.*.cs` 加用例；涉及 UI、子进程、真机的逻辑不进单测，
落到 `docs/TESTPLAN.md`。

**② 最低验证线（改动后）**

1. `.\scripts\build.ps1 -Test` 构建成功且单测全过
2. 启动 `bin\Release\M1ToolPlus.exe`，主窗口正常显示
3. 涉及读卡逻辑的改动需要真机（PN532/ACR122U + 卡片）回归——
   本环境无硬件，需协作者配合
4. 推送后 GitHub Actions `CI / Build & verify` 必须为绿

## 已知问题 / 技术债

- `Form1.cs` 巨石类承担全部业务逻辑，UI 与逻辑强耦合，是重构第一优先级
  （2026-10-26 已起步：终端渲染抽到 `TerminalView.cs`，读写卡流程骨架抽到 `CardToolFlow.cs`，
  `mf_read`/`mf_write` 已迁入，其余 16 处复制的 DoWork 处理器待迁）
- .NET Framework 4.0 Client Profile 已过支持期（现代化目标：.NET 8+ WinForms）
- ~~无 CI（后续加 GitHub Actions），无单元测试~~ **已解决（2026-10-26）**：
  `.github/workflows/build.yml`（net40 引用程序集注入 + 产物校验 + 跑单测）与
  `tests\M1ToolPlus.Tests`
- 官方 Release 包不含卫星程序集（历史问题），我们 build.ps1 已解决
