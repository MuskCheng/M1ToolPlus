# 后续开发路线图（交接备忘）

> 面向接续开发的交接文档：记录项目现状、已完成工作、待办事项与开发环境要点。
> 最后更新：2026-10-23（移除窗口缩放功能，固定窗口大小，补充 DPI/清晰度说明后）。
> 注意：本仓库原位于 `D:\repositories\MifareOneTool`，已更名为 **`D:\repositories\M1ToolPlus`**（纯文件夹改名，git 历史与远程配置不受影响）。
> **进行中的工作**：问题驱动审计（对照原作者 16 个 open Issue）与六步修复计划见 [docs/FIXPLAN.md](FIXPLAN.md)。

## 一、项目速览

| 项 | 内容 |
|---|---|
| 名称 | M1ToolPlus（原名 MifareOneTool，原作者 xcicode 已停更） |
| 定位 | Windows 平台 Mifare Classic 图形化工具（C# WinForms，.NET Framework 4.0 Client，x86） |
| 许可 | GPL-3.0：衍生发布须保留原版权声明、标注修改、提供对应源码（见 docs/DEPENDENCIES.md 检查清单） |
| 分支/状态 | `dev` 分支，本地领先 origin/dev（**全部未推送**，数量见 `git log origin/dev..dev`） |
| origin | `https://github.com/MuskCheng/M1ToolPlus.git`（**本人仓库，已推送 dev 分支**） |
| upstream | `https://github.com/xcicode/MifareOneTool.git`（原作者仓库，只读参考，用于 `fetch`） |
| 构建产物 | `M1ToolPlus\bin\Release\M1ToolPlus.exe`（+ en/ru/zh 卫星程序集 + nfc-bin） |
| 构建命令 | `.\scripts\build.ps1`（参数见 README / docs/DEVELOPMENT.md） |
| 用途声明 | 仅限对你有授权的卡片的安全研究（README 同样声明） |

## 二、已完成（本地提交清单）

| 提交 | 内容 |
|---|---|
| 9f416b7 | 收纳原 wiki 与发布产物到仓库目录、更新 .gitignore |
| f09e962 | 阶段①工程基础：修复 Debug 输出路径、清理 750KB 翻译残留、新增 scripts/build.ps1 与 fetch-deps.ps1、README、DEVELOPMENT.md |
| 18161f3 | 阶段②依赖合规：collect.exe / mfdetect.exe 来源查证（均为可重建开源分支）、docs/DEPENDENCIES.md（含发布检查清单）、winhex.exe 剔除策略 |
| 8e4df10 | 阶段③：新增英语界面（1208 条文案），修复 Issue #41/#38（英文版相关） |
| f5f5771 | 修复 Issue #26：启动恢复上次使用设备、设备正则放宽 |
| c453f2b | docs/TROUBLESHOOTING.md |
| 19d6094 | 存档原作者上一版完整 README（docs/README.original.md） |
| 1b97708 | 全面更名为 M1ToolPlus：命名空间/程序集/解决方案/目录/UI 文案/脚本/文档 |
| ddfa58a | feat(ui): 设置页可切换主题（跟随系统/深色/浅色），优化窗口缩放卡顿，不重启生效 |
| 425bea2 | fix(ui): 覆盖 TabControl 残留系统白边，TabFramePanel 改为实色背景并绘制四条边框线 |
| 6214a88 | refactor(ui): 移除窗口缩放功能，保持 FixedSingle 固定窗口，避免拖动卡顿与白边 |
| 35ed166 | docs: 移除缩放功能说明，补充窗口固定大小与 DPI/清晰度文档 |
| 99abace | fix(ui): 浅色主题下选项卡取消选中底色，拦截 WM_NCHITTEST 移除右下角缩放句柄 |
| 5005bc3 | fix(ui): 浅色主题下选项卡选中/悬停不填底色，终端强制白底深字 |
| 2b80487 | fix(ui): StatusStrip 右下角 grip 覆盖隐藏，彻底移除缩放句柄；浅色主题选项卡/终端白底 |
| 7c830c2 | fix(ui): 启动时强制把终端默认字号重置为12pt，避免旧配置残留奇怪字号 |
| 303e4a8 | fix(ui): StatusStrip.SizingGrip 从源头关闭右下角缩放把手/箭头；进程启用 PerMonitorV2 DPI 感知根治位图拉伸模糊 |
| 49e0247 | fix(dep): mff08.exe 改判随包附带（官方M1T-Release原样二进制）；外部工具缺失改报实际文件名，不再误称MFF08 |
| 8f305ff | fix(ui): ApplyFont 跳过 TerminalBox——切换主题（手动深/浅）后终端字号不再退回 12px 等宽字体「异常缩小」 |
| 859d7a0 | release: v1.8.0 —— README 重写为功能向导版（衍生声明并入许可证节）；AssemblyInfo 升 1.8.0.0，准备打 tag v1.8.0 与 GitHub Release |

## 三、待办（按优先级）

### A. 发布准备（推送前必做/必决策）

1. ~~**在 GitHub 建仓库并推送**~~ ✅ 已完成（2026-10-16）：`https://github.com/MuskCheng/M1ToolPlus`（public，默认分支 `dev`，GitHub 已识别 GPL-3.0，已设置简介与 10 个 topics）。远程布局：`origin` = 本人仓库，`upstream` = 原作者仓库（只读）。
   ~~本地积压的 13 个提交~~ ✅ 已全部推送（2026-10-23）：`origin/dev` = `e8ef51c..fde0903`，本地与远端已同步。
2. 复核两项暂缓决策（发布前定下来）：
   - `M1ToolPlus\app.config` 与 Settings 中 `GitHubR = xcicode/MifareOneTool`：目前为休眠配置（GitHubUpdate 类未启用）。若启用更新检查，需改为新仓库。
   - `M1ToolPlus\Form1.cs:1261` 硬编码原作者 releases 链接（下载最新版按钮），发布时决定改为新仓库或保留双链接。
3. ~~打开发布包~~ ✅ 已完成（2026-10-23）：`.\scripts\build.ps1 -Pack` → `artifacts\M1ToolPlus-local-build.zip`（含主题切换、移除缩放、终端字号12pt 等最新修复）。
   合规检查项已由脚本自动落实并实测通过（详见下方 A'）。
4. 版本号策略：AssemblyInfo 仍为 1.7.0.0（延续原版）。**2026-10-23 决策：首发 Release 延后到真机回归之后**，届时再定版本号（建议 1.8.0）。本地已积累 8 个 UI/主题/缩放修复提交，推送后可评估是否合并为一个功能版本。

### A' 发布包合规核对（2026-10-23 实测：`artifacts\M1ToolPlus-local-build.zip`，51 个文件 / 解压 8.06 MB / zip 2.79 MB）

| 检查项 | 结果 |
|---|---|
| 主程序为 `M1ToolPlus.exe` + `M1ToolPlus.exe.config` | ✅ |
| en/ru/zh 三套卫星程序集齐全 | ✅ |
| winhex.exe 未打包（专有软件） | ✅ `build.ps1` 只取 `nfc-bin\` 子目录；本地回收缓存里的 winhex.exe 也已删除 |
| **`mff08.exe` 来源不明** | ~~已按策略剔除~~ **改判（2026-10-23）：随包附带**——`build.ps1` 不再剔除，取自官方 `M1T-Release.zip`（v1.7.0）原样二进制；依赖文档/发布说明保留来源与索取源码指引（见 `docs/DEPENDENCIES.md`）。无 nfc-bin 的开发环境下 GUI 弹提示并自动关闭 MFF08 对话框（`FormMFF08_Load`） |
| 随包附带 GPL-3.0 LICENSE + README + DEPENDENCIES.md | ✅ `build.ps1 -Pack` 自动附带（原缺失，已补） |
| nfc-bin 内上游 `*.bak`（libgcc_s_dw2-1/libnfc/libwinpthread-1/pcre3，约 1.0 MB） | ✅ 已清理：程序零引用、Windows 也不会加载。ACR122U 切换只依赖 `libnfc.dll` / `libnfc(ACR122Uslow).dll` / `libnfc(532).dll` + **运行期生成**的 `libnfc.dll.acr122u.bak`（见 `Form1.cs` 的 `buttonEnAcr122u_Click`），`build.ps1` 每次构建自动清理并保留运行期备份 |
| nfc-bin\data\logs_proxmark3\trace*.txt | 官方包自带的日志残留（约 10 KB），无害，可选清理 |
| zip 内路径分隔符 | ⚠️ PowerShell 5.1 `Compress-Archive` 写入的是反斜杠路径，Windows 解压正常，Linux/macOS 的 `unzip` 可能出错（本项目仅支持 Windows，暂不处理） |

### B. 真机回归（本开发环境无硬件，必须待真机）

**照着 `docs/TESTPLAN.md` 执行即可**（含准备清单、逐项勾选、问题记录模板、回归后收尾步骤）。

- Issue #26 修复实测：PN532 / ACR122U 启动与设备恢复。
- en/ru/zh 界面切换实测（下拉索引 0=zh / 1=ru / 2=en）。
- MFF08 对话框回归：随包附带 `mff08.exe` 时窗口正常打开；仅缺失 nfc-bin 的开发环境下弹提示自动关闭，不崩溃。
- 无回归后，到原作者仓库对应 Issue 下回复（措辞注意：非官方衍生、GPL-3.0、避免暗示原作者背书）。

### C. 阶段④现代化（未开始，需先装 .NET SDK）

- .NET Framework 4.0 Client Profile → .NET 8+ WinForms 迁移。
- 拆分 Form1.cs 巨石类（约 1456 行，UI 与逻辑强耦合，重构第一优先级）。
- GitHub Actions CI（构建 + 无硬件冒烟）。
- 迁移注意：GitHubUpdate.cs 需显式启用 TLS 1.2、Newtonsoft.Json 依赖处理、现代 SDK 的 resx 本地化流程与现有 csc 卫星方案的取舍。

### D. 其他小事

- `.github/ISSUE_TEMPLATE/` 两个模板文件名为原作遗留（`---request.md`、`bug--.md`），可规范化改名并补充设备/语言信息。
- `artifacts\M1T-Release\`（原版发布包解压）与 `wiki\`（原 wiki 克隆）保留为素材，未清理。
- 用户设置迁移：更名后 exe 为 M1ToolPlus.exe，旧版用户配置目录不会沿用（新身份）；如需可做一次性迁移提示。

## 四、开发环境与踩坑备忘

- **构建**：VS2022 Build Tools 的 MSBuild；本机无 Windows SDK（无 al.exe），build.ps1 自动用 csc 手工生成卫星程序集。
  卫星生成必须走 `/t:Build`（用 `/t:CoreCompile` 会因跳过引用解析报 CS0518/CS0246）。
- **.NET SDK**：本机暂未安装——阶段④开始前先安装（8.x）。
- **PowerShell 5.1 编码坑**：含中文的 .ps1 必须存为 **UTF-8 with BOM**，否则会被按 GBK 误读导致语法错乱；.cs/.resx 为 UTF-8（BOM 混杂，修改时逐文件保留原编码）。
- **验证 x86 exe 资源**：64 位 PowerShell 不能 LoadFrom x86 程序集；
  用 `[Reflection.Assembly]::ReflectionOnlyLoadFrom` + `ResourceReader` 检查资源名与取值（应为 `M1ToolPlus.Form1.resources` 等）。
- **冒烟测试**：启动 `bin\Release\M1ToolPlus.exe`，主窗口标题应为 `M1ToolPlus`。
- **git 身份**：本地提交使用 `-c user.name="CXF" -c user.email="cxf@local"`（沿用惯例，可按需更换）。
- **推送网络**：本机 github.com 常被解析到 `20.205.243.166`（该 IP 的 443 不通，报 `Connection was reset`）。
  可用可达 IP 覆盖解析（SNI/证书仍按 github.com 校验）：
  `git -c http.curloptResolve=github.com:443:140.82.112.3 push origin dev`（备用 IP：140.82.113.3、20.27.177.113）。
- **语言资源**：切换按“下拉索引”映射（0=zh-cn 1=ru-RU 2=en-US）；新增语言三步：加 `.xx.resx` 并注册 csproj、combo 加一项、switch 加分支。
- 依赖再获取：`.\scripts\fetch-deps.ps1`（默认从原作者 releases v1.7.0 的 M1T-Release.zip 回收 nfc-bin）。
- **浅克隆教训（重要）**：本仓库最初从原作者克隆时使用了 `--depth`（浅克隆），推送到空仓库时报 `remote unpack failed: did not receive expected object`，且该对象在本地也不存在（`git cat-file` 失败）；已用 `git fetch --unshallow upstream` 修复（现 109 个提交、非浅克隆）。今后重建克隆**不要加 `--depth`**。

## 五、接手检查清单（新会话/新开发者第一步）

1. 确认工作目录已是 `D:\repositories\M1ToolPlus`（若会话仍指向旧路径，先切换/移动会话）。
2. `git status --short` 应为空；当前分支应为 `dev`。
3. `git log --oneline -3` 应包含 `1b97708`（更名）与 `19d6094`（README 存档）。
4. 快速构建验证：`.\scripts\build.ps1 -Clean`，确认输出 `M1ToolPlus.exe` + en/ru/zh 卫星。
5. 阅读：README.md、docs/DEVELOPMENT.md、docs/DEPENDENCIES.md、docs/TROUBLESHOOTING.md，然后按「三、待办」从 A 开始推进。

### UI 主题与截图验证

- **主题实现**：`M1ToolPlus\UiTheme.cs`，集中式皮肤，**运行时应用、不改 Designer 布局**。
  该界面是 VS2010 时代的绝对定位布局（FixedSingle 窗体 + 固定坐标），改布局会导致控件错位，
  因此改配色一律改 `UiTheme.cs`，不要动 Designer 里 `DodgerBlue` 之类的硬编码色。
- **主题来源与优先级**：`M1TOOL_THEME=light|dark`（仅调试/截图） > 设置页 `Settings.Theme`
  （`""`=跟随系统 / `dark` / `light`） > `HKCU\...\Themes\Personalize\AppsUseLightTheme`，
  并监听 `WM_SETTINGCHANGE` 即时刷新。设置页「外观 / 主题」保存后立即刷新所有已打开窗体，无需重启。
- **按能力降级**：Win11 21H2+ 自动圆角、22H2+ 尝试 Mica；Win10 或不支持时回退纯色，功能不受影响。
- **截图验证**：`.\scripts\screenshot.ps1 -Out <png>`（启动 → 等主窗体 → `PrintWindow` → 自动退出）；
  逐标签页用 `.\scripts\screenshot-tabs.ps1 -MaxWidth 1400`（配合程序的 `--tab N` 参数）。
  改 UI 后用它肉眼对比深浅两套效果；确认脚本需 UTF-8 BOM（本仓库脚本统一要求）。
- **⚠️ 不要在 Resize 事件里修改控件尺寸**：会触发父容器 SizeChanged 形成无限重排循环，
  表现为界面卡死（已踩过一次，见提交 7a1af2c 的回退）。需要动态调整尺寸时，
  只在初始化阶段做一次，或用防抖 + 标记位避免重入。
- **窗口固定大小**：主窗口保持 `FixedSingle`，不再允许用户拖动边框缩放，避免拖动缩放时
  上百个控件逐帧重绘导致的卡顿与白边；功能区初始宽度若不足，仅在启动时一次性撑宽。
- **DPI / 显示清晰度**：进程在 `Program.Main` 中优先声明 **Per-Monitor DPI Aware V2**
  （Win10 1703+ `SetProcessDpiAwarenessContext`；旧系统回退 `SetProcessDPIAware()`），
  任何缩放比例（100%/125%/150%）与多显示器组合下都按**物理像素 1:1** 渲染，系统不再做
  GDI 位图拉伸，文字边缘清晰、不会发虚。界面字号使用像素值 `UiFontPx`（等价 9pt 微软雅黑），
  在任意 DPI 下都渲染出相同的物理像素高度，控件坐标固定，绝对定位布局不会错位。
- **右下角缩放把手**：`StyleStatusStrip` 中显式 `SizingGrip = false`。默认开启时
  `StatusStrip` 自绘把手并在 `WM_NCHITTEST` 中对把手区域返回 `HTBOTTOMRIGHT`
  （子控件内部处理，主窗口 NCHITTEST 拦截收不到），必须从源头关闭。
- **⚠️ GroupBox 无法干净自绘**：系统已绘制标题，自绘边框会与之重影、或边框线横穿标题。
  深色下只调 `ForeColor`，不要接管 Paint。
- **自动回归**：`.\scripts\regression-auto.ps1` —— 覆盖无需硬件的部分（子进程依赖完整性、
  ACR122U 驱动变体、启动冒烟、设备扫描行为、缺文件保护），结果写入 `artifacts\regression-auto-report.md`。
  需真机的部分见 `docs/TESTPLAN.md`。
- 已知遗留：「C/FUID卡操作」等分组框标题仍折行（受原作固定列宽限制，非本次引入）；
  要根治需重排该区域列布局，或留到阶段④随布局重构一并处理。

## 六、原仓库 Issue 跟进清单（快照 2026-10-16，共 16 个 open）

| Issue | 标题（简） | 状态 / 建议动作 |
|---|---|---|
| #42 | win10 点「一键解原卡」蓝屏（2025-07，最新） | **最高关注**：代码路径已定位（见下），需真机复现 + 崩溃转储；先回复索取卡型/读卡器/系统信息，并在 TROUBLESHOOTING.md 加风险提示 |
| #41 | English version? | ✅ 已实现（8e4df10）→ 推送后回复 |
| #40 | CUID 加密卡（已知密钥）如何清卡 | 评估「清 M1 卡」是否覆盖该场景，能答复则答复，否则记为功能需求 |
| #39 | PN532 检测一直显示「运行中」 | 与 #26 同源，f5f5771 可能覆盖 → 真机验证后回复 |
| #38 | Eng translation | ✅ 已实现（8e4df10）→ 推送后回复 |
| #37 | hardnested 只采集不计算，不生成 .txt？ | 已离线定位（见下）→ 可直接答复；可选改进见下 |
| #35 | 补写 dump 到小米手环 6 报认证失败 | 卡片兼容性（SaferKey / 已知密钥场景）→ 需真机 |
| #34 | 非 Classic 卡（SAK 00）读取报错 | 非目标支持场景 → 说明即可 |
| #33 | 支持滚动码卡（GTU/GDM/GUID） | 不支持（滚动码防复制）→ 说明即可 |
| #32 | nfc-mfsetuid 来源 / 是否开源 | 合规相关 → 引用 docs/DEPENDENCIES.md 的来源结论答复 |
| #31 | ACR122U 支持不完善、响应慢 | 与 #26 同源 → 真机验证后回复 |
| #30 | Hardnested 云计算服务端程序 | 原作者服务已失效（见 #29），自行实现成本高 → 低优先，说明现状 |
| #29 | GCP 一键部署脚本 | 同 #30 |
| #28 | hardnested 时移动窗口报「无法创建堆栈防护页面」 | 离线未能定位（见下）→ 需真机 + 用户环境信息 |
| #26 | win10 扫描设备巨慢 | ✅ 已修（f5f5771）→ 真机验证后回复 |
| #9 | 无法连接 ACR122U（29 条评论） | 设备兼容老问题 → 真机验证后回复 |

> 回复纪律：先推送本 fork；回复中注明「非官方衍生、GPL-3.0、未经原作者背书」，再引用对应提交号。

### 离线分析结论（2026-10-16，勿重复劳动）

- **#42「一键解原卡」代码路径**：`buttonEMfoc_Click`（Form1.cs 1078）直接转调 `buttonMfoc_Click`，其 worker 启动 `nfc-bin/mfoc.exe`（Form1.cs 588），输出 `Mfoc.tmp`；另一路 `buttonECmfoc_Click`（带已知 Key）走 1348 行的 mfoc2 路径（参数 `/T:0A`）。蓝屏发生在 mfoc 进程运行期间，需崩溃转储才能定位。
- **#37「只采集」行为**：勾选「只采集」→ `FormHardNes.collectOnly()` 为真 → `Form1.CollectNonce` 启动 `nfc-bin/collect.exe`，参数仅含 key / 块号 / A-B（`GetArg()`），**不含输出文件名**；输出 .txt 由 collect.exe 按默认名生成在程序目录，退出码 0 时进度码 102 只提示「Nonce 收集完毕，可上传云计算」。GUI 原本有改名逻辑（`GetFileAfter()` 生成 `_<块号>AB.txt` 后缀），但 Form1.cs 1396 行已注释，故文件名不带 UID。
  - 可选改进：恢复 1396 行或参照 101 分支的 `auto_keys\` 自动归档逻辑（需真机验证）。
- **#28「无法创建堆栈防护页面」**：核查本地与原版官方 `nfc-bin` **均不含 stackguard.exe / stackguard.dll**，`libnfc_hardnested.exe`、`libnfc_hardnestedlc.exe`、`collect.exe` 中也**无 stackguard 字符串引用** → 离线无法定位报错来源；需真机复现并收集用户系统环境（Win 版本、杀软、DEP 设置）。
- **#26 / #31 / #39 / #9** 同属设备连接与扫描耗时问题，f5f5771 的改动集中在「启动恢复上次设备 + 设备正则放宽」，理论上覆盖 #39/#26 的常见场景，但 ACR122U 本身在 Win10 上的 ~100s 延迟另有根因（#26 报告人提到所有 ACR122U 操作都慢），真机回归时要分开观察。

## 七、本次核查发现的杂项

- `artifacts\M1T-local-build.zip`（改名前旧产物）与 `artifacts\package\`（旧打包中间目录）已过期，可清理；`artifacts\M1T-Release\`（官方包解压）与 `M1T-Release.zip` 保留为素材（`artifacts/` 整体已被 gitignore）。
- `dotnet` 命令存在但**未安装任何 SDK**（报 `No .NET SDKs were found`）→ 阶段④开始前需安装 8.x。
- `.gitignore` 与 `.github` 模板里的中文注释本身是**正常 UTF-8**；用 PowerShell 5.1 的 `Get-Content`（默认按 GBK）读取会显示乱码——排查时加 `-Encoding UTF8`，不要误判为文件损坏。
- 单实例互斥锁用 `Application.ProductName`（现为 M1ToolPlus），与改名一致，无需处理。

## 八、深色主题 / 字体 / 标签头专项修复教训（2026-10-19，提交 d8079be…85d3054）

### 字体

- **resx 里 81 个控件带显式 `Microsoft Sans Serif`**：窗体字体的继承在这些控件上失效，只改 `Form.Font` 没用 → `UiTheme.ApplyFont` 逐控件套微软雅黑 9pt。
- 字号定 9pt 不是 9.5pt：工具页按钮列宽固定 84~97px，再大「从UID卡读回」「写(UF)UID卡」等长文案会溢出（已实测）。
- 终端区用 Consolas 等宽（日志对齐需要），终端颜色/字号设置项照常生效。

### 布局 / AutoSize

- 分组框宽度自适应：手设 `Width` 会被 AutoSize 引擎按**内容首选宽度**拉回，而首选宽度**不含标题**——「C/FUID卡操作」这类标题会被裁。
  方案：`MinimumSize` 下限约束与 AutoSize 引擎共存（`FitGroupWidth`，对表格用 TLP 绝对列宽增量、对流式父容器用 MinimumSize）。
- **红线：不要把 GroupBox/Button 的 AutoSize 关掉**——分组框塌缩、按钮消失。
- `TextRenderer.MeasureText` 用 4 参重载 `(text, font, new Size(10000,10000), flags)`；勿传 null dc 或 `int.MaxValue`（会抛/溢出）。

### 配色归类

- **深色下「哪个控件是残留浅底」按亮度判**：`Luma(c.BackColor) > 150` 归为浅底翻深。
  **不要**用 `== SystemColors.Control` / `Color.Transparent` 判据——resx 里有显式 `Color.White`，旧判据从不命中，这就是「设置页成片白底」旧问题的根因。
- **⚠️ `Color.Transparent` 只对有透明支持的父容器有效**：透明底在 Panel/分组框/表格/复选框这层（无透明继承）会**回退默认窗口色 = 白**，实测踩坑。这些容器一律给实色底（DSurface/DCard 系）。
- ComboBox 深色：`DInput`(#1C1C1C) + `FlatStyle.Flat` + `Paint` 自画 DBorder 边框（ComboBox 没有 FlatAppearance 可设，边框只能自画）。
- 复选框/分组框焦点虚线框：`TabStop = false` 消掉深色下的焦点圈。

### 标签头（本轮新踩的大坑）

- 现象：深色主题下标签条右侧、按钮间隙、选中按钮描边、按钮上下沿线全是**原生浅色白带**（用户截图实锤）。
- **三种常规手段全部无效**（逐次实测确认）：
  1. 设 `TabControl.BackColor` —— 余白不走控件背景色；
  2. 挂 `Paint` 事件 —— 收不到原生绘制的那块区域；
  3. `Appearance = FlatButtons` —— 余白/间隙/描边仍是浅色。
- **唯一可行**：整条标签头用自绘面板 `TabBandPanel`（UiTheme.cs）盖住，实现要点：
  - 面板挂**窗体**（TabControl 的 Parent）上，`SetChildIndex(p, 0)` 压到最上层——盖在 TabControl 的子控件层没用，母窗原生绘制会盖回去；
  - 坐标基准 = TabControl 客户区（面板恰好覆盖标签头全高 `DisplayRectangle.Y`），`GetTabRect(i)` 的坐标可直接用于绘制；
  - 按 GetTabRect 复绘按钮（配色与原 DrawItem 一致），`MouseClick` 按命中矩形设 `SelectedIndex`，`MouseMove` 悬停高亮 + 按钮区手型光标，未选中按钮画右侧 `Sep` 分隔线；
  - **`t.SelectedIndexChanged += → p.Invalidate()` 必须有**：面板盖着原生重绘，不自刷的话切换页签后选中高亮滞留在旧页签上；
  - `SetWindowTheme(handle, "", "")` 去原生主题（uxtheme P/Invoke），否则场所绘制指标与 GetTabRect 有几像素偏差；
  - 面板挂载必须 `BeginInvoke` 延迟：套主题发生在 `StyleChildren` 的 `foreach (parent.Controls)` 遍历中，此刻直接往 Controls 集合加控件抛「集合已修改」；句柄未建再挂 `HandleCreated` 重入。
- 复验已知边界：窗口最左侧 0~7px 的浅色边条是 **DWM 非客户区边框**，应用侧无法控制，不算 app 的锅（深色边框需 `DwmSetWindowAttribute` 系列另行处理，非本轮目标）。

### 调试方法（均实测有效）

- `M1TOOL_DEBUG_SIZE=1` 曾驱动 `File.AppendAllText` 落盘运行时尺寸/颜色埋点；**已从代码移除**，需要时按同样模式临时加回去，事后清理。
- **空 `catch (Exception) {}` 会吞真异常**：本轮 `LayoutBandStrips` 失效半天就因异常被吞——加「环境变量门控的文件落盘日志」再跑一次就能定位；排查 UI 不生效问题先怀疑被吞。
- **`scripts\build.ps1` 必须顺序执行，勿并发**：两个 build 同时跑会抢 `M1ToolPlus\bin` 下临时文件。
- 截图用 `PrintWindow`（32 位 DWM 兼容）；`CopyFromScreen` 会截到前台浏览器。
- 像素采样脚本：`$env:TEMP\opencode\crop.ps1`（截图放大，参数：图片 x y w 高 缩放 输出）与 `px.ps1`（单点采样）；先 `GetPixel` 横/纵扫找 `#F0F0F0` 残留的精确范围，别靠肉眼判断。
- `screenshot-tabs.ps1 -MaxWidth 1500` 逐页签出图；`$env:M1TOOL_THEME='light'` 强制浅色复验。

### 窗口尺寸 / DPI（用户实测暴露，2026-10-19）

- **现象**：用户屏（高系统缩放，逻辑工作区小于设计尺寸）下，初始窗口上下溢出屏幕、拖不动。
- **根因**：`ApplyAdaptiveLayout` 把 `MinimumSize` 写死 `(900,640)`；在逻辑工作区 < 640 高的屏幕上，
  窗口比屏幕大、又缩不下去（拖拽全被 MinimumSize 顶回）；`CenterScreen` 只居中不查边界，上下两端被裁。
  `.NET` 的 `WM_WINDOWPOSCHANGING` 会强制执行 MinimumSize——**连 SetWindowPos 都缩不下去**（实测）。
- **修法（UiTheme.cs）**：`MinimumSize` 取「功能下限」与「工作区-16」两者较小（下限兜底 420x360）；
  启动末尾 `ClampToWorkArea` 先缩到放得下、再把窗口挪回可视区；功能区撑宽（`MeasureRequiredWidth`）
  也受工作区宽度约束。
- **调试要点**：
  - 应用是 **DPI-unaware**（.NET 4.0 + 未声明 dpiAware）：`GetWindowRect`/`SetCursorPos` 在 PowerShell
    测试进程里的坐标要先 `SetProcessDPIAware()`，否则 125% 下命中带差一位（光标「看似在边框」实为客户区）；
  - 合成输入拖非客户区边框（`mouse_event` 式 border drag）在本环境反复失败，但
    `WM_NCHITTEST` 返回 `HTRIGHT` 证实命中带存在、`WS_THICKFRAME` 在位——判定为自动化环境局限，
    真实用户鼠标不受影响；客户端点击（tab 切换）合成输入正常；
  - `PrintWindow` 出图尺寸与窗口物理/虚拟坐标的换算跟着**测试进程**的 DPI 感知走，别混用两套坐标空间；
  - 主题探测卫生检查：注册表 `AppsUseLightTheme` 与 app 渲染亮度抽样比对（曾有一张浅色截图属
    运行期间系统被切主题，非 Bug——先核对注册表再定性）。


- 本轮用户截图显示「设置页白底 + 灰字」在最新构建**不可复现**（是旧 exe 在跑），而「标签头白带」在最新构建**真实存在**——先按用户实际窗口尺寸（默认 900 宽、`--tab 2` 复现设置页）跑当前构建再定性，区分 stale 二进制与真回归，别照着旧图盲修。
- 用户机 Win11 26300、125% DPI、系统深色。`截图为 PrintWindow 物理像素`，坐标采样时注意 DPI。

### 边框拖拽失效（真根因，2026-10-19 第二轮）

- **现象**：改成 Sizable 后窗口仍然「按住边框拖不动」；用户多次反馈未解决。
- **真正的根因**：本程序是 **x86 + DPI-unaware**，在 125% 缩放屏上被系统做 GDI 位图缩放：
  窗口边框被撑成 16px（`GetWindowRect` 与 `GetClientRect` 差 16），但系统的非客户区命中测试仍按
  未缩放的边框算，于是**最外侧 8px 被判为 `HTBORDER`(18)（不可缩放的普通边框）**，
  真正可缩放的 `HTRIGHT` 区域被挤到内侧 10~16px——而那一段已属客户区、被终端框等子控件抢走。
  结果：用户抓窗口最外沿 = 抓到 `HTBORDER` = 什么都不会发生。
  同机对照：64 位标准 WinForms 窗体/记事本同位置返回 `HTRIGHT`；`FormBorderStyle` 运行时改写、
  `MinimumSize`、`ClientSize`、Dock、hook 与否都**不是**原因（逐一排除）。
- **修法（Form1.Resize.cs）**：窗体接管 `WM_NCHITTEST`——收到 `HTBORDER` 时按命中点相对四条边的
  位置翻译成 `HTRIGHT/HTLEFT/HTTOP/HTBOTTOM` 及四角；客户区最外圈再补 8px 热区。
- **教训**：
  - 判定「边框能不能拖」要看**真实鼠标路径**的 `WM_NCHITTEST` 结果，而不是把 `SendMessage(主窗体,…)`
    的返回值当结论——后者绕过了「系统把消息发给光标下最深子窗口」这一层；
  - `HTBORDER`(18) 与 `HTRIGHT`(11) 别看错：早前把十六进制 `0x12` 当成 `HTRIGHT`，其实是十进制 18 = `HTBORDER`；
  - 合成拖拽要用**真实光标移动**（`SetCursorPos` + `mouse_event`），只 post `WM_MOUSEMOVE` 无效
    （模态缩放循环读真实光标位置）；拖拽前先点标题栏激活窗口，否则第一次点击被激活动作吃掉；
  - 测试期间桌面上的其它窗口（Windows 终端 `CASCADIA_HOSTING_WINDOW_CLASS`、浏览器、资源管理器）
    会盖住抓点，导致「拖不动」的假象——用 `WindowFromPoint` 核对抓点归属，必要时临时置顶测试窗口；
  - **`build.ps1` 原来会吞掉编译错误**：MSBuild 报错时 obj 里残留上一次的 exe，脚本只判断「文件存在」
    就报「编译成功」，导致连续多轮拿旧程序做测试。已改为检出 `error CS/MSB` 即失败。
  - `UiTheme` 的 `NativeWindowHook` 原先是临时对象（无字段持有），可能被 GC 回收后 `ReleaseHandle`
    还原窗口过程、主题变更监听失效——已改为静态字段保活。

## 九、圆角美化改版（2026-10-20，UiTheme.Round.cs）

用户要求「UI 美化、多用圆角矩形」。约束：**不改 Designer 布局、不动 `Form1.Resize.cs` 的边框拖拽**，
只做运行时重绘。新增 `M1ToolPlus/UiTheme.Round.cs`（`UiTheme` 的 partial），风格向 Win11 Fluent 靠拢。

### 做法

| 对象 | 手法 |
| --- | --- |
| 按钮 | `FlatStyle.Flat` + `BorderSize=0` + 圆角 `Region` + `Paint` 自绘填充/描边/文字/焦点环（半径 6px） |
| 分组框 | 不设 Region（见坑 2），只填四角小方块盖掉系统直角的残线 + 自绘 8px 圆角卡片、边框、标题 |
| 输入框/终端 | `Region` 圆角裁剪（4px / 8px）+ 统一深色底 `DInput` |
| 下拉框 | 1px 内缩 `Region`（裁掉系统浅色边框）+ 子面板盖住右端原生浅色箭头按钮，自绘三角 |
| 数字框 | `BorderStyle=None` + 子面板盖住原生白色上下箭头，自绘 ▲▼ 并转发 `UpButton/DownButton()` |
| 标签页 | 药丸标签（10px）+ 深色状态栏（`Professional` + 自定义 `StatusPalette`） |

层次：窗体 `DBack` → 页签页 `DBack` → 卡片 `DCard` → 输入框 `DInput`。容器/复选框一律继承父容器底色。

### 三个致命的坑（本轮耗时最多）

1. **`Control.Region` 保存的是同一个 `Region` 对象引用，不复制。**
   `Region nr = new Region(path); c.Region = nr; nr.Dispose();` 会让控件持有一个**已释放**的 Region，
   之后控件创建句柄时 `Control.OnHandleCreated → Region.GetHrgn` 抛
   `ArgumentException: Parameter is not valid.`。
   该异常发生在 **`WmCreate`** 里，会：
   - 打断整页的创建/绘制 → 表现为「整页卡片只剩第一张」；
   - 切页时弹 WinForms 未处理异常对话框（441×174、标题是程序名），**模态循环卡住 `SelectedIndex` setter**，
     于是「标签条高亮切了、页面内容没切」。
   正解：新 Region 交给控件持有，**只 Dispose 被替换掉的旧 Region**。
2. **空 Region 同样会在 `WmCreate` 抛异常。** 控件尺寸还是 0×0 时构造的 Region 是空区域，
   `GetHrgn` 一样报 "Parameter is not valid"。必须 `宽高 >= 3` 才设 Region，尺寸不足时清空并等
   `Resize`/`HandleCreated` 再设。另外 `GraphicsPath.AddArc` 的宽高不能为负 →
   圆角半径必须 `min(radius, min(w,h)/2)`，零/负尺寸直接返回空路径且调用方跳过绘制。
3. **分组框不能设 Region，也不能整块铺父底色。**
   带 `AutoSize` 的分组框一旦裁剪，布局会塌陷、内容全部消失；整块铺父底色则会擦掉
   **先绘制**的兄弟分组框（同页多卡片时只剩最后一张）。只能填四角 `RadiusCard×RadiusCard` 的小方块。

### 页签错位（标签条高亮与内容不一致）

- 真因不是主题代码，而是**在 `Load` 里切页**：句柄创建阶段原生 TabControl 会排下一条
  `TCN_SELCHANGE`（报告旧索引 0），消息循环启动后处理它会把 WinForms 内部索引写回 0；
  此后 `SelectedIndex` setter 与 getter（原生值）比较，同值赋值被当成无变化忽略。
- 修法：初始页签一律延后到 `Shown` + `BeginInvoke`（消息队列排空后）再设，
  `SelectTab()` 只做下标校验后赋值，不再需要「切到相邻页再切回」的强制刷新。
- 早期尝试的「`SelectedIndexChanged` 里同步 `TabPage.Visible` + 250ms guard 定时器」会与
  TabControl 内部逻辑打架，出现**两页同时可见**，已弃用（基线对照实验证明 HEAD 点击切页本来就正常）。

### 深色主题细节

- **黑色 PNG 图标在深色底上等于隐形**（箭头、对勾、状态栏图标）。按图像平均亮度判定「整体偏暗」才反色
  （保留彩色/浅色图片），原件与反色件都缓存；`PictureBox` 与 `ToolStripItem.Image` 都处理。
- `SystemColors.HotTrack`（深蓝）和设计器里的深色自定义文字在深色底上对比度不足：
  HotTrack 映射为浅蓝 `#6CB6FF`，其余深色文字向白色混合 45%（保留色相）。
- `ComboBox` 即使设了 `BackColor`，原生箭头按钮仍是系统浅色；`SetWindowTheme(h,"DarkMode_CFD",null)`
  只对边框/底有效，箭头仍需自绘遮盖。
- 多行且 `Dock=Left` 的标签会被表格行高钳死（行高按单行算），第二行被裁：取消 Dock 交回 `AutoSize`。
- 标题栏深色由 `DwmSetWindowAttribute(20)` 实现（Win11 生效）；**`PrintWindow` 截图里标题栏是白的**，
  那是抓图渲染的假象，要用屏幕截图（`CopyFromScreen`）核对。

### 验证与回归

- `scripts/screenshot-tabs.ps1` 用 `--tab N` 启动截图；`--tab` 在修复后工作正常（此前错位是上面的异常导致）。
- 点击路径另用真实鼠标事件（`ClientToScreen` + `SetCursorPos` + `mouse_event`）验证过，
  标签条中心 client 坐标约为 x=72/215/352、y=17（zh 界面）。
- `scripts/regression-auto.ps1`：PASS 3 / WARN 1 / FAIL 0（WARN 为既有的「缺 nfc-bin 时提示」人工确认项）。
- 待真机回归通过后再升版本号到 1.8.0 并发版；UI 改动本身不升版本。

### 浅色系统边框清理（白线条）

用户反馈「白色、粗细不均的分割线」，逐像素扫描定位到三处，全部由系统绘制、与主题色无关：

| 位置 | 现象 | 根因 | 修法 |
| --- | --- | --- | --- |
| 页签内容区四周 | 左/右/下各 **4px** `#F0F0F0` | `TabControl` 去掉原生主题后，WinForms 用 `SystemColors.Control` 自画页面外框 | `TabFramePanel`：与 TabControl 同尺寸、`Region` 挖掉页面区域的面板，填页面底色盖住外框；挖空后页面内控件点击不受影响 |
| 终端区右侧/下沿 | 右 2px、下 1px 白边 | `Prepare()` 把所有 `TextBoxBase`（含 `RichTextBox`）强制改成 `BorderStyle.FixedSingle` | `Prepare()` 跳过 `RichTextBox`，终端保持设计器的 `BorderStyle.None`（靠圆角 Region 塑形） |
| 状态栏上沿 | **1px 纯白** `#FFFFFF` | `StatusStrip` 的边框由渲染器单独绘制，`ProfessionalColorTable` **没有**对应覆盖点（`ToolStripBorder` 对它无效） | `StatusRenderer : ToolStripProfessionalRenderer` 重写 `OnRenderToolStripBorder`，改画 1px `DBorder/LBorder` |

- 统一后的分隔层级：卡片描边 1px `DBorder` → 内容区与终端之间 1px `DBorder` → 状态栏上沿 1px `DBorder`，
  粗细/色值完全一致；页签内容区左右不再有内缩白框，视觉上与窗口边缘齐平。
- 验证方式：对截图做**逐行/逐列亮像素统计**（亮度 >190 的像素占比），客户端区域内应无任何长亮线；
  注意 `PrintWindow` 截图的标题栏恒为白色（DWM 假象）、窗口外框是系统强调色，二者不算。
  （2026-10-20 补充：真实截屏取色主窗口标题栏 = `#272727`、对话框 = `#2B2B2B`，**深色标题栏是生效的**，
  白只是 PrintWindow 渲染 DWM 边框的假象——不要据 PrintWindow 图判断标题栏颜色。）

### 高 DPI 1:1 渲染（2026-10-20，用户要求「初始 UI 1:1 显示」）

- **现象**：用户机 1080p @125%，初始窗口几乎占满屏幕且整块发虚。实测：进程 unaware 时被系统做 GDI 位图拉伸，
  窗口物理尺寸 = 900×816 × 1.25 ≈ **1125×1020**，文字边缘全是插值灰边。
- **根因（两层）**：
  1. 进程未声明 DPI 感知（.NET 4.0 无清单、无 API 调用）→ Windows 按 96 DPI 渲染后整体拉伸 1.25×；
  2. 字号用 **Point（磅）** 单位 → 即便声明感知，125% 下文字会放大 25%，而绝对定位坐标不变 → 裁字、间距失衡。
- **修法**：
  - `Program.Main` 第一行 `SetProcessDPIAware()`（系统级 DPI 感知，V1；per-monitor V2 需 .NET 4.7+ 处理 `WM_DPICHANGED`，不适用）；
  - `UiTheme` 字号改**像素**：`UiFontPx = 12F`（= 96 DPI 下的 9pt 雅黑，行高恒 16px），
    `UiFont/TerminalFont/GetFont/ApplyFont` 全部 `GraphicsUnit.Pixel`；
  - `ApplyFont` 增加 `cur.Unit == GraphicsUnit.Pixel` 判断：设计器里若已有同族同号字体，那是**磅**值字体，
    在 125% 下会被放大，必须强制替换成像素字体；
  - 终端字号设置沿用「磅」的用户语义（默认 9.0），创建时换算 `value * 96/72` 像素。
- **结果**：125% 屏上窗口 = **900×839 物理像素**（此前 1125×1020），界面按 96 DPI 像素尺寸渲染、清晰锐利；
  `Font.Height` 恒为 16px；GDI 测量残差约 9%（120 DPI 网格拟合），仅影响自适应按钮的几像素宽度。
- **验证**（本机就是 125%，可直接测）：
  - `M1ToolPlus.exe --ui-info` → `%TEMP%\m1tool-ui-info.txt` 输出 `DeviceDpi/ClientSize/AutoScaleDimensions/关键控件 Bounds`；
  - 截图像素对比（同区域放大 4 倍）：新构建文字锐利、旧构建有插值灰边；
  - 真实鼠标：三页签点击切换正确；四边 + 四角缩放光标（含**最外沿 1px**）全部正确（`GetCursorInfo` 判形状）；
    逐级拖拽右边框 +10×6 → 宽度 900→960 精确跟手。
- **教训**：
  - `Font.Size` 的**单位随 `Font.Unit` 变化**（Pixel 字体返回像素值、Point 字体返回磅值），比较字体时必须同时看 `Unit`；
  - 进程改成 DPI 感知后，**截图/点击脚本也必须 `SetProcessDPIAware`**，否则 `GetWindowRect`/`SetCursorPos` 是虚拟坐标，
    抓图尺寸与点击位置全错（`scripts/screenshot-tabs.ps1` 已补）；
  - `GetWindowRect` 连续两次启动位置不同（(96,96)/(224,181)/(64,64)）是 **Windows 级联默认定位**（Form1 未设 `StartPosition`），
    不是本程序逻辑，与本次改动无关；
  - 拖拽测试别用单次 `mouse_event(MOVE, dx)` 大跳变（会被系统尺寸循环放大成 +109），要**逐级小步**移动才与真实拖拽等价。

## 十、语言切换失效（2026-10-20，用户实测暴露）

### 现象
设置页下拉框切 Русский / English → 程序自动重启 → 界面仍全中文。

### 排查路径（可复用）
1. 先看用户配置：`%LOCALAPPDATA%\XciCode\M1ToolPlus.exe_Url_*\1.7.0.0\user.config`。
   本次 `Language` 已写入 `ru-RU` → 切换、保存、重启逻辑本身没问题，问题在**资源加载**。
2. `--ui-info` 加打印 `SettingLanguage / CurrentUICulture / TabPage1Text`。
   `bin\Release` 启动时 `Language` 为空（那是另一份 user.config），而 `artifacts\package`
   启动时读到 `ru-RU` → 用户测的是打包目录里的 exe，用同一路径复现。
3. 反射探针（x86 程序集必须用 32 位 PowerShell 加载）：
   - `Assembly.GetSatelliteAssembly(ru)` 能加载 `ru\M1ToolPlus.resources.dll` ✓
   - `new ResourceManager("M1ToolPlus.Form1", asm).GetString("buttonListDev.Text", ru)`
     却返回中文 ✗
   - 用 csc 手搓「main + satellite」最小复现同样失败 → 与项目代码无关，是**卫星命名**问题。

### 两个根因
| # | 根因 | 证据 |
|---|---|---|
| 1 | 卫星文件名是 `M1ToolPlus.<culture>.dll`，CLR 只探测 `<程序集名>.resources.dll` | 改名为 `M1ToolPlus.resources.dll` 后 `GetSatelliteAssembly` 可加载 |
| 2 | 本机（Windows 11 + .NET Framework 4.8.10008）的 `ResourceManager` 只按**旧式资源名** `<基名>.<文化>.resources` 在卫星里查找；只嵌现代名 `M1ToolPlus.Form1.resources` 时找不到 | 最小复现：卫星只放现代名 → 返回中性值；只放旧式名 → 返回 RUSSIAN；两个都放 → 正常 |

> 上游原包**根本没有** `zh/en/ru` 目录 → 语言切换在上游就从未生效过，不是改名引入的回归。

### 修复
`scripts/build.ps1` 的 csc 卫星步骤：
- 输出/复制为 `<culture>\M1ToolPlus.resources.dll`（程序集名 `M1ToolPlus.resources`）；
- 每个 `.resources` **同时**用现代名与旧式名嵌入（`/resource:file,logicalName` 两次）；
- 构建时清掉旧命名的 `M1ToolPlus.<culture>.dll` 残留。

### 验证
- `artifacts\package\M1ToolPlus.exe --ui-info --tab 2`（`Language=ru-RU`）→
  `TabPage1Text=Режим копирования карты`、`ButtonListDevText=Определить` ✓
- 改 `en-US` → `TabPage1Text=Copy card mode`、`ButtonListDevText=Testing Equipment` ✓
- 俄语三页签截图：文案完整无裁切；窗口按文案自动加宽（1320×839）✓
- `--ui-info` 保留 `SettingLanguage / CurrentUICulture / CurrentCulture / TabPage1Text / ButtonListDevText` 诊断行

### 教训
- 手搓卫星程序集（绕过缺失的 al.exe）必须同时满足两点：**文件/程序集名 `<主程序集>.resources`** +
  **资源名新旧两种都嵌**；否则运行时**静默**回退到中性资源，不抛任何异常。
- 语言类问题先查「设置是否真的写进当前 exe 的 user.config」，再查卫星加载；
  同一程序不同路径（bin\Release vs artifacts\package）用的是**不同的 user.config**。

## 十一、语言下拉框空白与俄语布局（2026-10-21，用户实测暴露）

### 现象
1. 设置页语言下拉框是**空的**，看不出当前是哪种语言；
2. 切俄语后主窗口 1348×693（中文 900×839），复制模式卡片被拉到两倍宽、箭头错位。

### 根因
1. 下拉框只在 `SelectedIndexChanged` 里保存 + 重启，构造时**从未设置 `SelectedIndex`**，
   所以永远没有选中项。
2. `Form1.ru.resx` 是原项目用俄语设计器保存的，带了一整套**旧版俄语布局**：
   `$this.ClientSize = 1098, 456` + 122 个 `Size` + 36 个 `Location`。
   `ApplyResources` 时这些值覆盖新布局，窗口尺寸/卡片宽度全被带偏。
   `Form1.zh.resx` / `Form1.en.resx` 都是纯文案，没有这个问题。

### 修复
- `Form1.ApplyLanguageSelection()`：按 `CultureInfo.TwoLetterISOLanguageName`
  选中 0/1/2，用 `suppressLanguageChanged` 屏蔽赋值触发的
  `SelectedIndexChanged`（否则构造期就 `Settings.Save()` + `Application.Restart()`，
  变成重启死循环）。
- 下拉首项文案由「标准」改为「简体中文」（en=`Chinese`、ru=`Китайский`），
  与 `zh-cn` 语义一致，避免误认为「跟随系统」。
- 清空 `Form1.ru.resx` 的全部几何条目（`ClientSize/Size/Location`），只留文案；
  布局统一由设计器基准值 + 运行期自适应计算。
- `build.ps1` 新鲜度检查排除语言专用 resx（只影响卫星，不影响 exe），
  否则只改俄语文案会误报「产物早于源文件」并中止构建。
- **语言名使用各语言本名**：`comboBox1.Items` 固定为 `简体中文` / `Русский` / `English`，
  并删除 `Form1.resx` / `Form1.zh.resx` / `Form1.en.resx` / `Form1.ru.resx`
  里的本地化条目——否则英语界面下会显示 `Chinese` / `Russian`，
  中文、俄语用户在别的界面语言里认不出自己的语言。

### 验证
- `--ui-info` 新增 `LanguageCombo`：`zh-cn → 0:简体中文`、`ru-RU → 1:Русский`、
  `en-US → 2:English`，每次启动进程数恒为 1（无重启循环）✓
- `--ui-info` 新增 `LanguageItems`：三种界面语言下均为
  `简体中文|Русский|English`，不随界面语言翻译 ✓
- 俄语窗口恢复为 1348×839（宽度仍是按文案自适应加宽，高度与中文一致）；
  复制模式卡片宽度、箭头位置与中文一致 ✓
- 三语设置页截图：下拉框均回显当前语言 ✓

### 教训
- WinForms 本地化 resx 里的 `$this.ClientSize` / 控件 `Location` / `Size`
  是**设计器快照**，不是文案；改版布局后必须清理，否则只有对应语言会错位。
- 「显示当前值」的控件要显式初始化，且初始化时必须屏蔽
  `SelectedIndexChanged`，否则会把「回显」当成「用户切换」。

## 十二、终端区无输出（2026-10-21，真机首测暴露）

### 现象
连接 PN532（pn532_uart:COM6）后点「检测连接」：状态栏进入「运行中」，
下方日志框**完全空白**，无法判断是否在扫描、卡在哪一步。

### 根因（两处叠加）
| # | 根因 | 证据 |
|---|---|---|
| 1 | 终端 `RichTextBox` 被设为 `Dock.Fill`，但 z 序在 `TabControl` 之下，被拉成 (0,0,整窗)；日志从控件顶部写，前 300+ 像素被页签盖住 | `--ui-info` 显示 `richTextBox1={X=0,Y=0,Height=646}`、`tabControl1={Height=358}`；`TerminalTextLength=28` 说明文字一直存在 |
| 2 | `list_dev` 把 stdout 行交给 `lineHandler` 解析设备名，但没再转发给界面（`ToolProcess` 重构时漏掉了上游的 `b.ReportProgress(0, _e.Data)`） | 上游代码有该行，重构 diff 中被删除；无设备时终端只剩「开始执行检测设备」 |

### 顺带发现
- **编码**：nfc-bin 的中文提示是 **GBK(936)** 字节（「使用libnfc」= `CA B9 D3 C3`），
  按 UTF-8 解码会得到 `ʹ��`（`CA B9` 恰好是合法 UTF-8 序列，不会抛异常）。
  之前按 UTF-8 解码的修复方向是错的，现固定 936。
- **空引用**：`Form1.ActiveForm.Text = ...` 在窗口未激活时 `ActiveForm` 为 null，
  点击按钮直接抛 `NullReferenceException`（自动化点击时必现）。20 处已改为 `this.Text`。
- **扫描耗时**：勾选「减少找设备延迟」会删除 `libnfc.conf` 触发全量扫描，
  实测 20~60 秒；而 `nfc-scan-device` 的 stdout 是块缓冲的，扫描结束前没有新行。

### 修复
- 终端区显式放在页签下方、状态栏上方，四边锚定（不再 Dock.Fill）。
- 设备扫描逐行回显，再做正则解析。
- `ToolProcess` 固定 936 解码；`GetUID` 同步。
- 扫描提示追加「扫描可能需要数十秒，请稍候」（zh/en/ru 三语资源）。

### 验证
- 点「检测连接」→ 终端依次出现：开始提示 → `nfc-scan-device` 原始输出 →
  「找到 1 个NFC设备:」→ `pn532_uart:COM6:115200` → 自动选用该设备 → `##运行完毕##` ✓
- 中文显示正常，无 `ʹ��` 乱码 ✓
- 自动化点击（窗口未激活）不再弹「.NET Framework 未处理异常」✓
- 自动回归：PASS 3 / WARN 1 / FAIL 0，且第 4 项已能识别到 COM6 ✓

### 教训
- `Dock.Fill` 与 `Dock.Top` 组合依赖 **z 序**；不确定顺序时会得到「被遮住的全尺寸控件」，
  这类问题用 `--ui-info` 打印 Bounds 一眼就能定位，不要靠肉眼猜。
- 手工重构输出管线时，要检查每条 `lineHandler` 分支是否仍然把行送给 UI。
- 「没有输出」先分辨是 **没写** 还是 **看不见**：`RichTextBox.TextLength` 是最快的判据。

## 十三、终端字体与字号（2026-10-21，用户实测暴露）

### 现象
1. 终端默认 Consolas，中文字形缺失，靠系统回退成雅黑，中英混排高度不一致；
2. 设置页「终端文字大小」显示 9.0，但和实际观感对不上，改完重启也不一定生效；
3. 在终端上 Ctrl+滚轮，字确实变大了，但「终端文字大小」的数字**不变**——两者永远对不上。

### 根因
| # | 根因 | 证据 |
|---|---|---|
| 1 | `UiTheme.TerminalFont` 写死 Consolas（无中文字形） | 中文回退雅黑、英文 Consolas，同一行里字重与行高不一致 |
| 2 | `MainCLIFontSize` 只在 `numericCLIFontSize_ValueChanged` 里写入，**启动时从未读取** | 设置形同虚设，重启后终端仍是主题给的默认值；设计器初值硬编码 9.0 |
| 3 | WinForms `RichTextBox` 自带的 Ctrl+滚轮改的是 `ZoomFactor`（显示缩放），不是 `Font` | 设置页读 Font，滚轮后数字不变；`ZoomFactor` 还会叠加在字号上，形成双重缩放 |

### 修复
- 新增 `M1ToolPlus/TerminalBox.cs`（`RichTextBox` 子类）：在 `WndProc` 拦截 `WM_MOUSEWHEEL`，
  按消息里的 `MK_CONTROL` 位判断 Ctrl，把滚轮翻译成「字号 ±1 磅」并**吃掉消息**，
  RichEdit 不再改 `ZoomFactor`（恒为 1）。
- `numericCLIFontSize` 成为唯一权威值：滚轮 → 改 `Value` → `ValueChanged` → 改字体 + 存设置。
  设置页显示多少，终端就是多少。
- 终端字体改为 **Microsoft YaHei UI**（与界面同一字体族）；字号按 `pt × 96 / 72` 转**像素**建字体，
  与全程序「物理像素 1:1」的约定一致。
- 默认字号 9 → **10 磅**：`Settings.settings` 默认值、`Settings.Designer.cs`、设计器初值同步修改。
- 新增 `TerminalFontVersion` 做一次性迁移：旧配置里的 `MainCLIFontSize` 从未真正生效过，
  升级后统一提升到 10 磅，避免老用户看不到变化。
- `UiTheme.StyleTerminal` 对 `TerminalBox` 跳过字体赋值：系统深浅色切换会重刷主题，
  以前会把终端字体重置回 Consolas + 默认字号。

### 验证
- `--ui-info` 新增 `TerminalFontSize / TerminalFontFamily / TerminalFontPx / TerminalZoom`：
  默认 `10pt`、`Microsoft YaHei UI`、`h=17`（界面 9pt → h=16，确为大一号）、`Zoom=1` ✓
- Ctrl+滚轮（悬停在终端上即可，无需点选）：+3 格后设置页数字 13→16，落盘 `MainCLIFontSize=16`；
  下滚 6 格回到 10 ✓
- 设置页截图：「终端文字大小」显示 `10.0`，终端中文为雅黑、无裁切 ✓

### 教训
- `ZoomFactor` 是**显示缩放**，不改变 `Font`。任何「按字号显示/保存」的 UI 都不能让它和 Font 各说各话：
  要么彻底禁用（本轮做法），要么把 `Font × ZoomFactor` 当作有效字号参与显示与保存。
- RichTextBox 的 Ctrl+滚轮是 RichEdit 原生行为，`MouseWheel` 事件不可靠，
  必须在 `WndProc` 里拦截 `WM_MOUSEWHEEL`；判断 Ctrl 要用消息 wParam 的 `MK_CONTROL` 位
  （`ModifierKeys` 读全局键盘状态，自动化/触摸板消息可能不置位）。
- 老项目的「设置项」可能只是写进配置、从没被读取过。改默认值时要加版本迁移，
  否则老配置会永远压着新默认值。
- 截图验证用 `CopyFromScreen`（窗口置前后抓屏）；`PrintWindow` 在 DWM 下会返回旧帧或叠入其他窗口。

## 十四、2026-10-22 三项用户实测问题（写卡日志 / S50HTool 配色 / HardNested 崩溃）

> 状态：代码修复完成并已用自动化脚本验证；**真机复测通过后**再升 1.8.0 并发布。

### 1. 写卡过程没有实时日志

**现象**：写卡时终端空白，结束时一次性出现
`需要写入 64 个块 |.x...|`、`完成！写入了 63 / 64 个块.`、认证失败等全部输出。

**根因**：`nfc-mfclassic` 的进度条逐字符 `printf`、不换行，仅在扇区边界 `fflush`；
`ToolProcess` 用 `ReadLine()`，必须等到 `\n` 才返回，所以直到进程结束才读到内容。

**修复**：
- `ToolProcess.Pump` 改字符流：`\n` 定稿；未换行内容以 `ProgressLiveLine = -1` 上报（约 120ms 一次）；
  流结束残行定稿；CRLF / `\r` 进度重写均处理。
- `Form1`：`logLive()` 从 `liveLineStart` 原地替换最后一行；`logAppend()` 先删临时行；
  `buttonConClr_Click` 复位。
- **关键坑**：`richTextBox1.ReadOnly = true` 时 `SelectedText = ""` 被静默忽略
  （插入/替换有效），必须临时关闭只读再删，否则出现 `|...| |...|` 重复进度条。

**验证**：伪造 `nfc-scan-device.exe`（每 140ms 一个字符、每 4 字符 flush、最后才换行）
跑端到端：采样终端最后一行的长度从 4 → 7 → 10 → … → 62 逐格增长；
结束时只剩一行完整进度条。自动化脚本：`scripts\test-livelog.ps1`
（输出 `RESULT: growing=True singleLine=True` → `PASS`）。

### 2. S50HTool 文本看不清

**现象**：载入卡片后十六进制框 / 权限下拉框为 Aquamarine / Tomato 底 + 浅色字；
扇区列表深底黑字。

**根因**：校验色是硬编码浅色，与深色主题的浅色前景色冲突；DataGridView 单元格前景色未主题化。

**修复**：
- `UiTheme.MarkField / ResetField`：按主题同时设置底色与前景色（深色有效 `#17372A`/`#A7E8BE`，
  无效 `#3E1C1C`/`#FFB3B3`；浅色有效 `#D9F2E3`、无效 `#F8D7D7`，文字 `#1F1F1F`）。
- `buttonSaveSectorEdit_Click` 改用 `IsHex()` 正则，不再比较背景色。
- `UiTheme.StyleGrid`：单元格 / 交替行 / 表头 / 选中行全部显式配色。
- `FormHardNes` 配色同步改用 `MarkField`。

**验证**：控件实测 `b0/kA back=FF17372A fore=FFA7E8BE`、`kB back=FF3E1C1C fore=FFFFB3B3`；
截图确认深绿/深红底配可读文字，扇区列表浅字深底。

### 3. HardNested 类型转换崩溃

**现象**：点「HardNested」弹出
`Unable to cast object of type 'System.Windows.Forms.RadioButton' to type 'System.Windows.Forms.CheckBox'`。

**根因**：`UiTheme.StyleControl` 把 RadioButton 传给 `StyleCheckBox((CheckBox)c)`；
两者是兄弟类型（都继承 `ButtonBase`）。

**修复**：`StyleCheckBox(ButtonBase)`，两个分支都 `(ButtonBase)c`。

**验证**：Win32 `PostMessage(BM_CLICK)` 点击「HardNested」→ 出现
「初始化HardNested参数」窗口（标题、KeyA/KeyB 单选按钮均正常），无 `#32770` 错误框。

### 待真机复测清单

- [ ] 写卡：1 KB / 4 KB 普通卡，进度条实时增长、结束无重复行
- [ ] S50HTool：深/浅主题下有效/无效字段与扇区列表均可读，保存校验正常
- [ ] HardNested：对话框可打开，KeyA/KeyB 可切换，执行/取消均正常
- [ ] 上述通过后：版本号 1.7.0.0 → 1.8.0，打 tag，更新 `docs/TESTPLAN.md` 勾选并发布

## 十五、2026-10-22 DiffTool 尺寸/标题 + S50HTool 行头空白（用户实测）

### 1. 差异比较窗口过大、内容缩在左上角

**现象**：窗口约 900×640，A / B / 比较按钮与输出框只占左上约 400×380，
右侧和下方大片空白；标题栏是 `DiffTool`。

**根因**：
- `UiTheme.ApplyAdaptiveLayout` 里的 `MinimumSize=(900,640)` 是为主窗口
  （`Form1`，`FixedSingle`）设计的，却无差别套到了所有窗体；
  `FormDiff` 是 `FixedDialog` + `AutoSize`，内容只有 370×456，
  被下限强行撑到 900×640 后内容不会跟着放大。
- 标题写在 `FormDiff.resx` / `FormDiff.zh.resx` 的 `$this.Text`，值仍是 `DiffTool`。

**修复**：
- `ApplyAdaptiveLayout` 在入口判断 `FormBorderStyle != FixedSingle` 时直接返回；
  主窗口的缩放/撑宽/边界钳制逻辑不变。
- 标题改为「差异比较」；英文 `Compare Dumps`；俄文沿用 `Сравнение дампов`。

**验证**：PrintWindow 截图确认 DiffTool 客户端 398×383，A/B/比较按钮与输出框铺满，
标题栏「差异比较」；S50HTool 也从 900×640 收紧为约 738×650，右侧空白消失。

### 2. S50HTool 扇区列表左侧的空白竖条

**现象**：列表最左侧一条贯穿 16 行、无文字的窄竖条（用户红框）。

**结论**：这是 `DataGridView` 的行头（RowHeader），默认可见，只显示行选中箭头，
不承载业务数据。扇区号已包含在单元格文本「扇区 N: 空扇区/有数据」中，
**没有隐藏列或未显示的内容**。

**修复**：`FormHTool` 的 `dataGridView1.RowHeadersVisible=false`；
保留 `FullRowSelect`，点击单元格仍选中整行。

**验证**：截图确认左侧空白竖条消失，扇区行从 GroupBox 左边距开始，选中/加载正常。

### 待真机复测（追加）

- [ ] DiffTool：标题「差异比较」，窗口不再出现大片空白，比较按钮可用
- [ ] S50HTool：左侧无空白竖条，点击扇区行可正常加载
- [ ] 与前一轮三项一起复测通过后再升 1.8.0

