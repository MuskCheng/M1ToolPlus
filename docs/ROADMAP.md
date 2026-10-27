# 后续开发路线图（交接备忘）

> 面向接续开发的交接文档：记录项目现状、已完成工作、待办事项与开发环境要点。
> 最后更新：2026-10-26（落地 CI 构建门禁、更新包 SHA256 校验、Form1 首轮拆分 + 单元测试）。
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
| 构建产物 | `M1ToolPlus\bin\Release\M1ToolPlus.exe`（+ zh 卫星程序集 + nfc-bin；v1.9.0 起只有一个语言） |
| 构建命令 | `.\scripts\build.ps1`（`-Test` 跑单元测试、`-Pack` 打包、`-Clean` 重来；参数见 README / docs/DEVELOPMENT.md） |
| CI | `.github/workflows/build.yml`：干净 Windows runner 上构建 + 产物校验 + 跑单测（**推送受 token `workflow` scope 限制，见待办 A.5**） |
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
| f49ecd9 | feat(update): 「检查更新」指向本仓库最新 Release，内置自动下载（进度条）+「继续使用/重启更新」两选；重构 GitHubUpdate.cs、新增 FormUpdate.cs；GitHubR 改 MuskCheng；UpgradeRequired 升级迁移设置；打包统一 .NET ZipFile |
| b729457 | docs(USERGUIDE): 新增应用手册 docs/USERGUIDE.md（改写自原作者 M1T 手册，适配 v1.8.0），README 加链接 |
| 45559fa | docs(USERGUIDE): 合并段落内硬折行，保留空行/表格/列表/标题等结构性换行 |
| c133f1b | **ci**: 新增 `.github/workflows/build.yml`（net40 引用程序集注入 + 产物校验 + artifact 上传）；`build.ps1` 加 `M1TP_MSBUILD_PROPS` 注入口 |
| c3a323d | **feat(update)**: 下载包 SHA256 校验（GitHub asset `digest`，同尺寸篡改拒绝、无 digest 不阻断）；`-Pack` 打印哈希；四语新增「更新_校验失败」；v1.8.0 Release 说明补 SHA256 段 |
| fd3c8cb | **refactor(form1)**: 抽出 `TerminalView.cs`（四处重复的终端渲染 + 只读删进度行）与 `CardToolFlow.cs`/`IToolHost`（工具流程执行骨架），`mf_read`/`mf_write` 先行迁入 |
| 54ee71f | **test**: 新增 `tests\M1ToolPlus.Tests`（19 个纯逻辑用例，net40 控制台断言，无第三方框架）；`build.ps1 -Test`；CI 跑单测；DEVELOPMENT/TESTPLAN 补录 |

## 三、待办（按优先级）

### A. 发布准备（推送前必做/必决策）

1. ~~**在 GitHub 建仓库并推送**~~ ✅ 已完成（2026-10-16）：`https://github.com/MuskCheng/M1ToolPlus`（public，默认分支 `dev`，GitHub 已识别 GPL-3.0，已设置简介与 10 个 topics）。远程布局：`origin` = 本人仓库，`upstream` = 原作者仓库（只读）。
   ~~本地积压的 13 个提交~~ ✅ 已全部推送（2026-10-23）：`origin/dev` = `e8ef51c..fde0903`，本地与远端已同步。
2. ~~复核两项暂缓决策~~ ✅ 已完成（2026-10-26 复核）：
   - `Settings.GitHubR` 已是 `MuskCheng/M1ToolPlus`（`app.config` 与 `Settings.Designer.cs` 同步），
     「检查更新」指向本仓库的 Release，不再是休眠配置。
   - `Form1.cs` 里原作者 releases 链接已随 v1.8.0 重写为 `Settings.GitHubR`，无硬编码残留。
3. ~~打开发布包~~ ✅ 已完成（2026-10-23）：`.\scripts\build.ps1 -Pack` → `artifacts\M1ToolPlus-local-build.zip`（含主题切换、移除缩放、终端字号12pt 等最新修复）。
   合规检查项已由脚本自动落实并实测通过（详见下方 A'）。
4. ~~版本号策略~~ ✅ 已定（`859d7a0`）：AssemblyInfo 升至 **1.8.0.0**；本轮（2026-10-26）
   的多语言移除 + 版面修复记为 **1.9.0** 待真机回归后发版。
5. ~~推送阻塞~~ ✅ 已解除（2026-10-26 复核）：`origin/dev` 与本地 `c14bd8a` 一致，工作区干净，
   含 `.github/workflows/` 的提交已推送成功。CI 首跑用 `gh run list -R MuskCheng/M1ToolPlus` 查看。

### A' 发布包合规核对（2026-10-23 实测：`artifacts\M1ToolPlus-local-build.zip`，51 个文件 / 解压 8.06 MB / zip 2.79 MB）

| 检查项 | 结果 |
|---|---|
| 主程序为 `M1ToolPlus.exe` + `M1ToolPlus.exe.config` | ✅ |
| 卫星程序集 | ✅ 只剩 `zh\`（v1.9.0 移除 en/ru） |
| winhex.exe 未打包（专有软件） | ✅ `build.ps1` 只取 `nfc-bin\` 子目录；本地回收缓存里的 winhex.exe 也已删除 |
| **`mff08.exe` 来源不明** | ~~已按策略剔除~~ **改判（2026-10-23）：随包附带**——`build.ps1` 不再剔除，取自官方 `M1T-Release.zip`（v1.7.0）原样二进制；依赖文档/发布说明保留来源与索取源码指引（见 `docs/DEPENDENCIES.md`）。无 nfc-bin 的开发环境下 GUI 弹提示并自动关闭 MFF08 对话框（`FormMFF08_Load`） |
| 随包附带 GPL-3.0 LICENSE + README + DEPENDENCIES.md | ✅ `build.ps1 -Pack` 自动附带（原缺失，已补） |
| nfc-bin 内上游 `*.bak`（libgcc_s_dw2-1/libnfc/libwinpthread-1/pcre3，约 1.0 MB） | ✅ 已清理：程序零引用、Windows 也不会加载。ACR122U 切换只依赖 `libnfc.dll` / `libnfc(ACR122Uslow).dll` / `libnfc(532).dll` + **运行期生成**的 `libnfc.dll.acr122u.bak`（见 `Form1.cs` 的 `buttonEnAcr122u_Click`），`build.ps1` 每次构建自动清理并保留运行期备份 |
| nfc-bin\data\logs_proxmark3\trace*.txt | 官方包自带的日志残留（约 10 KB），无害，可选清理 |
| zip 内路径分隔符 | ⚠️ 打包已统一为 .NET ZipArchive（`ZipFile.CreateFromDirectory`，替代 PS 5.1 的 Compress-Archive，与更新器解压端同源）。仍写入平台默认的反斜杠分隔符：Windows 解压、内置更新器均正常；Linux/macOS 的 `unzip` 不兼容（本项目仅支持 Windows） |

### B. 真机回归（本开发环境无硬件，必须待真机）

**照着 `docs/TESTPLAN.md` 执行即可**（含准备清单、逐项勾选、问题记录模板、回归后收尾步骤）。

- Issue #26 修复实测：PN532 / ACR122U 启动与设备恢复。
- ~~en/ru/zh 界面切换实测~~ 已不需要（v1.9.0 只保留简体中文，语言下拉框已移除）。
- MFF08 对话框回归：随包附带 `mff08.exe` 时窗口正常打开；仅缺失 nfc-bin 的开发环境下弹提示自动关闭，不崩溃。
- 无回归后，到原作者仓库对应 Issue 下回复（措辞注意：非官方衍生、GPL-3.0、避免暗示原作者背书）。

### C. 阶段④现代化（进行中）

- ~~GitHub Actions CI（构建 + 无硬件冒烟）~~ ✅ **2026-10-26 完成**（`c133f1b` + `54ee71f`）：
  net40 引用程序集注入（runner 无 .NET 4.0 targeting pack 也能编）→ `build.ps1 -Test` →
  产物校验（exe / Newtonsoft / zh 卫星）→ artifact 上传。**待观察首跑。**
- ~~单元测试基建~~ ✅ **2026-10-26 完成**（`54ee71f`）：`tests\M1ToolPlus.Tests`，19 用例，`build.ps1 -Test`。
- **拆分 Form1.cs 巨石类：已起步**（`fd3c8cb`）——`TerminalView.cs`（终端渲染，四处重复归一）、
  `CardToolFlow.cs` + `IToolHost`（18 处「启工具→泵输出→等退出→翻标志」骨架，先迁 `mf_read`/`mf_write`）。
  下一步：其余 16 处 DoWork 处理器按同一接口迁移；`writecheck`、`GetUID`、密钥加载等纯逻辑继续下沉为可测函数。
- .NET Framework 4.0 Client Profile → .NET 8+ WinForms 迁移（未开始，需先装 .NET SDK）。
- 迁移注意：GitHubUpdate.cs 需显式启用 TLS 1.2、Newtonsoft.Json 依赖处理、现代 SDK 的 resx 本地化流程与现有 csc 卫星方案的取舍。

### D. 其他小事

- `.github/ISSUE_TEMPLATE/` 两个模板文件名为原作遗留（`---request.md`、`bug--.md`），可规范化改名并补充设备/语言信息。
- `artifacts\M1T-Release\`（原版发布包解压）与 `wiki\`（原 wiki 克隆）保留为素材，未清理。
- 用户设置迁移：更名后 exe 为 M1ToolPlus.exe，旧版用户配置目录不会沿用（新身份）；如需可做一次性迁移提示。

## 四、开发环境与踩坑备忘

- **构建**：VS2022 Build Tools 的 MSBuild；本机无 Windows SDK（无 al.exe），build.ps1 自动用 csc 手工生成卫星程序集。
  卫星生成必须走 `/t:Build`（用 `/t:CoreCompile` 会因跳过引用解析报 CS0518/CS0246）。
- **CI 无 .NET 4.0 targeting pack 时**：设 `M1TP_MSBUILD_PROPS=CustomAfterMicrosoftCommonTargets=<pkg>\build\Microsoft.NETFramework.ReferenceAssemblies.net40.targets`
  （来自 `Microsoft.NETFramework.ReferenceAssemblies.net40/1.0.3` 包），build.ps1 会把它追加到所有 MSBuild 调用；
  本地模拟 CI 也用同样办法。
- **单元测试**：`.\scripts\build.ps1 -Test` → `tests\M1ToolPlus.Tests`（19 用例，控制台断言，无 NuGet）。
  测试工程用 `BuildProjectReferences=false` 构建（主程序已先构建，避免再撞 al.exe 缺失）；
  访问内部类型靠主工程 `InternalsVisibleTo`。新增用例放 `Tests.*.cs` 并在 `Program.cs` 注册。
- **.NET SDK**：本机暂未安装——阶段④开始前先安装（8.x）。
- **PowerShell 5.1 编码坑**：含中文的 .ps1 必须存为 **UTF-8 with BOM**，否则会被按 GBK 误读导致语法错乱；.cs/.resx 为 UTF-8（BOM 混杂，修改时逐文件保留原编码）。
- **验证 x86 exe 资源**：64 位 PowerShell 不能 LoadFrom x86 程序集；
  用 `[Reflection.Assembly]::ReflectionOnlyLoadFrom` + `ResourceReader` 检查资源名与取值（应为 `M1ToolPlus.Form1.resources` 等）。
- **冒烟测试**：启动 `bin\Release\M1ToolPlus.exe`，主窗口标题应为 `M1ToolPlus`。
- **git 身份**：本地提交使用 `-c user.name="CXF" -c user.email="cxf@local"`（沿用惯例，可按需更换）。
- **推送网络**：本机 github.com 常被解析到 `20.205.243.166`（该 IP 的 443 不通，报 `Connection was reset`）。
  可用可达 IP 覆盖解析（SNI/证书仍按 github.com 校验）：
  `git -c http.curloptResolve=github.com:443:140.82.112.3 push origin dev`（备用 IP：140.82.113.3、20.27.177.113）。
- **语言资源**：v1.9.0 起只保留简体中文（`CurrentUICulture` 固定 `zh-CN`）。
  要恢复多语言见 docs/DEVELOPMENT.md「本地化」一节的三步法。
- 依赖再获取：`.\scripts\fetch-deps.ps1`（默认从原作者 releases v1.7.0 的 M1T-Release.zip 回收 nfc-bin）。
- **浅克隆教训（重要）**：本仓库最初从原作者克隆时使用了 `--depth`（浅克隆），推送到空仓库时报 `remote unpack failed: did not receive expected object`，且该对象在本地也不存在（`git cat-file` 失败）；已用 `git fetch --unshallow upstream` 修复（现 109 个提交、非浅克隆）。今后重建克隆**不要加 `--depth`**。

## 五、接手检查清单（新会话/新开发者第一步）

1. 确认工作目录：主仓在 `D:\repositories\M1ToolPlus`（`dev` 分支）；自动化/并行开发可能在
   git worktree 里（如 `%USERPROFILE%\WorkBuddy\Worktrees\M1ToolPlus\<id>`，分支 `workbuddy/<id>`），
   两者共用同一份 git 历史，提交前先 `git rev-parse --show-toplevel` 认清位置。
2. `git status --short` 应为空；确认当前分支（`dev` 或 workbuddy 工作分支）。
3. `git log --oneline -3` 应包含 `1b97708`（更名）与 `19d6094`（README 存档）。
4. 快速构建验证：`.\scripts\build.ps1 -Clean -Test`，确认输出 `M1ToolPlus.exe` + `zh\` 卫星、单测全过。
5. 阅读：README.md、docs/DEVELOPMENT.md、docs/DEPENDENCIES.md、docs/TROUBLESHOOTING.md，然后按「三、待办」从 A 开始推进。

### 布局排查工具（2026-10-26 新增）

- `M1ToolPlus.exe --ui-info` 会把窗体尺寸/DPI/字体和**全部控件的绝对坐标**写到
  `%TEMP%\m1tool-ui-info.txt`（`--- controls ---` 段，`abs=` 为相对客户区的坐标）。
  怀疑「某控件被窗口裁掉」时，直接拿 `abs.X+Width` 与 `ClientSize` 比较，不要靠猜。
- `scripts\screenshot-tabs.ps1` 用 `CopyFromScreen` 抓整窗矩形，会把 DWM 的透明边框
  （窗口右侧约 10px）一起截进来，看起来像「有控件被切掉」——**这是抓图假象**，
  已用上面的控件坐标核对确认没有溢出。判断裁切记住这一条。
- `scripts\screenshot-dialog.ps1 -AppArgs "--update-dialog" -Title "软件更新"`：抓模态对话框。
  `--update-dialog` 是配合它加的调试参数（与 `--tab` 同一套路，只在显式传参时生效）。
  注意：**锁屏/远程会话断开时 `CopyFromScreen` 只能抓到壁纸**，看到整屏一张背景图就是
  这个原因，不是程序没起来。

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

---

## 十六、v1.9.0：移除多语言 + 版面与更新对话框整改（2026-10-26）

本轮由「项目检查」触发，先体检再按结论修，依据一并记在这里，避免下次重复排查。

### 1. 只保留简体中文（移除 en/ru）

- 删除全部 `*.en.resx` / `*.ru.resx`：6 个窗体各 2 个 + `Properties\Resources.en/ru` 2 个。
- `M1ToolPlus.csproj` 移除对应的 12 个 `EmbeddedResource` 条目。
- `Form1()` 构造里把 `CurrentUICulture/CurrentCulture` **固定为 `zh-CN`**（`Form1.UiCulture` 常量），
  不再读 `Settings.Language`——系统区域是英文/俄文时也稳定显示中文，不会落到无资源的回退态。
- 设置页「语言和地区」分组整体移除（`groupBox15` / `flowLayoutPanel23` / `label12` / `comboBox1`），
  连带删掉 `ApplyLanguageSelection`、`ComboBox1_SelectedIndexChanged`、`suppressLanguageChanged`，
  以及 `--ui-info` 里的语言诊断行。
- 删除从未被引用（且不在 csproj 里）的 `SelectLanguage.cs/.designer.cs/.resx`（MultiLang 模板遗留），
  以及 `scripts/gen-en-resx.ps1`（生成英文 resx 的脚本，语言移除后已无意义）。
- `build.ps1` 的卫星步骤只生成 `zh`；`.github/workflows/build.yml` 的产物校验也只校验 `zh`。
- 结果：`bin\Release` 下只剩 `zh\M1ToolPlus.resources.dll` 一个语言目录。

### 2. 检查更新对话框按钮重排（FormUpdate.cs）

- 动作按钮（重试 / 继续使用 / 重启更新）统一**右对齐 + 8px 等间距**，随可见数量重排
  （`LayoutActions()`；原先「重试」固定在最左 x=14，与右侧两个按钮分居两端，中间空一大块）。
- 只剩一个按钮的终态（已是最新 / 暂无发布 / 无更新包）同样右对齐，右边不再留空档。
- `_info` / `_detail` 改 `AutoSize=false` + `AutoEllipsis`：失败原因是异常消息、长度不可控，
  原先 AutoSize 会被窗口右缘硬裁。
- 失败态补退出路径：`继续使用` 改为「关闭」并启用（原先保持 disabled，用户只能点标题栏 ×）。

### 3. 主窗口与版面

- `MaximizeBox` 在 Designer 里也显式关闭（`UiTheme.ApplyAdaptiveLayout` 本来已设，
  这里补上，保证主题未生效时也不会出现「最大化后只剩左上角内容」的破版）。
- **终端区高度按页签分配**（`UiTheme.FitTerminalHeight`，页签切换时重算）：
  复制卡模式 132px（约 5 行）、高级操作模式 320px、软件设置 200px；窗口高度随之调整，
  窗口最小高度下限 640→480。实测：复制卡 900×525 / 高级 900×713 / 设置 900×593
  （改前三页都是 900×827，日志区固定吃掉 434px，而里面常年只有一行版本号）。
- 深色主题次要按钮专用色（`DMutedFill/Hover/Down/Border`）：实测原来 muted 按钮底色
  与分组卡**同为 `#333333`**，只剩一条 `#454545` 细边，按钮形状几乎不成立；
  改为 `#3E`/`#49`/`#54` + `#55` 描边，主操作蓝、破坏性红三层语义不变。
- 状态栏「计时器」空闲态显示 `运行时间0秒`（原先只有「计时器」三个字、后面空着，像坏了）。
- 删除 `Form1.Resize.cs`（接管 `WM_NCHITTEST` 做边框缩放热区）：与「固定窗口不再缩放」
  的决策冲突，且 `UiTheme` 已显式剥掉 `WS_THICKFRAME`，属于遗留未清理。

### 4. 体检结论更正（重要，避免后续被误导）

上一轮检查里有两条 UI 判断经实测**不成立**，已用数据推翻：

| 当时判断 | 复核结果 |
|---|---|
| 第二/三页右缘有控件被窗口裁掉 | **假象**。`--ui-info` 输出全部控件绝对坐标，无一个超出 `ClientSize`；右侧那条彩色竖带是 `screenshot-tabs.ps1` 用 `CopyFromScreen` 抓整窗矩形时把 DWM 透明边框（约 10px）一起截进来造成的 |
| 「请放原卡/请放新卡」两个标签压字重叠 | **不成立**。4 倍放大截图确认：两条标签分别位于卡片 1 内与卡片 2/3 之间，各占 35×38，两行文字完整、无重叠；缩小看会误判成「碎字」 |

「深色下次要按钮层级丢失」也不准确——配色逻辑一直是灰的，真实问题是灰得和卡片同色（见 3. 最后两条）。

### 5. 终端欢迎语改三行（2026-10-27）

原来首行文案写在 Designer 的 `richTextBox1.Text`（末尾没有换行），而 `Form1_Load` 又用
`logAppend(Resources._软件版本 + 版本)` 往后追加，两者粘成一行：`Hello,cardman!#软件版本 1.8.0.0`。

现在：
- 清空 `Form1.resx` / `Form1.zh.resx` 里的 `richTextBox1.Text` 与 `richTextBox1.Lines[0]`（两个键都清，
  因为 zh 卫星用的是 `Lines[0]` 而不是 `Text`，只清一个会漏）；
- `Form1_Load` 逐行输出：`Resources.欢迎语` → `Resources.首次运行提示` → `Resources._软件版本 + 版本号`；
- 新增资源键 `欢迎语`（`Hello,cardman!`）与 `首次运行提示`（首次运行建议点击"检测设备"以加快后续运行速度），
  中性 resx 与 zh resx 各一份，并同步 `Properties\Resources.Designer.cs` 的强类型属性。

### 6. 设置页三处输入/色块控件观感修复（2026-10-27）

三处有一个共同根因：**控件用 Region 裁掉最外 1px**（为了盖住原生浅色系统边框），
而描边画在控件自己身上 —— 正好落在被裁掉的那 1px 里，永远看不见。

- 现在由**父容器**在裁出的那一圈上补 1px 圆角描边（`HookInputBorder` / `PaintInputBorders`），
  下拉框与数字框因此都有可见边界；数字框的 Region 也改为内缩 1px 以留出这圈。
- 深色下原生箭头/上下箭头按钮比预想宽（125% DPI 下连分隔线占右侧 22px），
  覆盖面板原来只盖 17~21px，会在箭头左边露一条白竖线（逐像素定位到 x=523）；
  改为盖 23px 并把高度补到裁切边界（`Height-2`），箭头下沿那条白横线一并消失。
- 「终端文字颜色」原先把所选颜色设成按钮**文字色**，主题一应用又被 `ForeColor` 覆盖，
  结果是灰块 + 灰字。现在按钮直接画成色块：底色 = 当前配色、文字 = `#RRGGBB` 对比色、
  `chip` 专属描边（按色块亮度往对比方向混）；选色后调 `UiTheme.SetColorChip()` 即时刷新，
  不整窗重套主题。
- 已知未做：色块按钮（x=291）与数字框（x=302）**左边缘差 11px**，来自两者分属不同的
  表格/流式容器；要对齐需动 Designer 里该卡片的两列布局，本轮先留着。

### 待真机复测

- [ ] 三个页签切换时窗口高度变化是否顺畅（复制卡 525 / 高级 713 / 设置 593）
- [ ] 复制卡模式日志区 5 行够不够看写卡结果（真机写卡会刷多行输出）
- [ ] 检查更新：失败态「关闭」可点、三个按钮右对齐不重叠（断网或改坏 `GitHubR` 复现）

### 7. 出包与截图工具（2026-10-27）

- 本轮改动尚未提交，工作区 35 文件（+746 / −8891，主要是删掉的 12 个 en/ru 资源）。
- 交付包：`artifacts\M1ToolPlus-1.8.0-20261027.zip`（2.7 MB，含 `nfc-bin\` 42 个文件 +
  `zh\` 卫星 + LICENSE/README/DEPENDENCIES），可直接解压运行。
- **`build.ps1 -Pack` 的已知缺陷（已修）**：原先打包前会 `Remove-Item` 旧的
  `artifacts\package` 与 `M1ToolPlus-local-build.zip`，在受管的 safe-delete 环境里回收站删除
  失败会 fail-closed 直接中断打包（`SAFE_DELETE_FAIL_CLOSED`）。
  现改为每次用新目录 `artifacts\package-<时间戳>` + `artifacts\M1ToolPlus-<时间戳>.zip`，
  不再先删；包内缺 `nfc-bin` 时给 Warning 提示先跑 `fetch-deps.ps1`。实测 `-Pack` 直通。
- **`screenshot-dialog.ps1` 已重写**：原先用 `FindWindow(null, "软件更新")` 搜不到窗口
  （进程里有该窗口，FindWindow 返回 0）。改为枚举该进程的顶层窗口按标题匹配，
  并加 250ms 重绘等待——否则抓到的是刚创建还没画的空白块。
  另注：「已是最新」态 1.6 秒就自动关（`_autoClose.Interval = 1600`），
  所以必须 150ms 轮询 + 出现即抓。

### 8. 仍未处理（下轮清单）

| # | 问题 | 判断依据 | 建议 |
|---|---|---|---|
| 1 | 设置页「指定设备」未检测设备时是**空白框** | `SetDeviceCombo` 只在检测设备或恢复上次设备时填项，全新环境为空 | 加灰字占位「（先点检测设备）」或预置一条禁用项 |
| 2 | 色块按钮 x=291 / 数字框 x=302，左边缘差 11px | 两者分属不同容器 | 动 Designer 该卡两列布局 |
| 3 | 卡片高度参差（设置页三列、高级页第二排） | 截图实拍 | 统一 `MinimumSize.Height` 或改等高布局 |
| 4 | ~~高级模式日志区固定 320px 只显示 3 行~~ **已解决**（见第 17 节：客户区改 1:1） | 截图实拍 | — |
| 5 | 复制卡模式「停止」按钮孤悬右缘中部 | 截图实拍 | 并入「运行/终端」卡或移到状态栏 |
| 6 | 更新对话框检查中上方约 110px 空白；禁用态两按钮同色，主次不分 | 对话框截图 | 空白是失败态 `_hint`/`_lnk` 的位置，可接受；禁用态建议 `_btnRestart` 保持实心蓝仅降透明度 |
| 7 | 版本号仍 1.8.0.0，而本节记为 v1.9.0 | `AssemblyInfo.cs` | 发布前决定是否升 1.9.0.0（会换 user.config 目录，`Upgrade()` 已能迁移） |
| 8 | `screenshot-tabs.ps1` 的 `--tab` 偶发不生效（浅色第 3 页抓到第 1 页内容） | 实测两轮，浅色 tab_3 尺寸 525 = 复制卡高度 | 疑与 `Shown → BeginInvoke` 时序有关，需排查 |
| 9 | 硬件相关功能 | 本工作区无读卡器 | 真机回归，见上方待测项 |


---

## 十七、日志区高度：终版按 1:1 分配（2026-10-27）

起因：复制卡模式的日志区只给 132px（约 5 行），真机写卡会刷多行输出，用户反馈「显示范围太小」。

### 最终形态（当前代码）

**客户区按 1:1 分成「功能区」和「日志区」**，日志高度直接取功能区高度（`want = tabs.Bottom`）。
- 实测：功能区 331 / 日志 331 / 状态栏 27 → 客户区 894×689，窗口 **900×724**。
- 三个页签高度一致（以前随页签 525/713/593 来回跳，切页签窗口会跳高跳矮，现在不会）。
- 日志从 5 行涨到 **15 行左右**，写卡流水基本不用滚。
- 上限仍按工作区算（`wa.Height − 边框 − contentH − 4`），只防「小屏 + 高 DPI」顶出屏幕。
- **不提供手动拖拽**：面板比例固定，用户不需要调（2026-10-27 决策）。

### 已废弃：中间实现（仅存档，代码已删）

先做过一版「按页签比例 + 可拖动分隔条」，用户评估后要求改成 1:1 并去掉拖拽，故整体移除：
按页签 35%/47%/36% 的 `DefaultLogHeight`、7px 高的 `EnsureLogSplitter` 拖拽条
（`Cursors.SizeNS`、胶囊抓手、悬停变主题蓝、双击恢复默认）、`Settings.LogHeights` 持久化。
**如果以后又要拖拽，别重写**：`git log` 里 2026-10-27 那次提交可直接 revert 回来。

当年验证过的数据（真机鼠标事件）：拖动 +130px → 窗口 593→723、终端 323px；重启恢复；
双击 → 593；`LogHeights=323;0;0`。

### 踩坑记录（仍有价值）

- **自写 PS 脚本里中文注释会吃掉下一行**：文件存成无 BOM 的 UTF-8，PS 5.1 按 GBK 解码，
  中文连续字节把行尾换行吞掉，下一行整行变成注释（现象是 `$变量` 莫名是空）。
  自用脚本一律 ASCII 注释，或存 UTF-8 with BOM。
- `$r.Left` 从 `New-Object 结构体+RECT` 取值时会莫名拿到空值，改成 C# 返回 `int[]` 一次取全。

### 追加：数字框上下箭头画反了（2026-10-27）

「终端文字大小 12.0」后面的微调箭头显示成一个实心「工」字块。原因在
`UiTheme.Round.cs` 的 `CoverSpinner`：两个三角的坐标把**顶点方向写反了**——
上面的三角顶点朝下、下面的朝上，两个箭头朝内相对，粘在一起就成了梯形块。

- 改为顶点朝外：上箭头底边在下、顶点在上；下箭头顶边在上、顶点在下。
- 尺寸从 6×3 调整为 7×4（接近系统原生的 ~9×4），两箭头间隔 2px，避免再粘成一坨。
- 绘三角时临时开 `SmoothingMode.AntiAlias`，画完恢复再描 1px 竖分隔线（要硬边）。
- 顺带确认：`12.0` 文本本身没有被裁切，问题只在箭头。
- 该修复对所有 `NumericUpDown` 生效（不止设置页这一个）。

### 追加：「请放原卡」改单行并与按钮对齐（2026-10-27）

原来该标签文本里带硬换行（`请放\n原卡`），配 35px 宽 → 竖成两行，夹在按钮之间像碎字。

- 文本改单行 `请放原卡`（**中性 + `Form1.zh.resx` 两处都要改**，中文卫星里也有 `label7.Text`，
  只改一个会被覆盖 —— 这是本项目改文案最容易漏的地方）。
- 补 `label7.TextAlign = MiddleLeft`：单行后默认顶对齐，实测文字中心比右侧「扫描卡片」按钮高 13px，
  补上后两者在同一条中线上；左边缘与上方「检测连接」按钮齐平。
- 单元格在 `tableLayoutPanel1` 的 (0,1)，与「检测连接」(0,0) 同列，列宽由按钮 71px 决定，
  单行文本 61px 不会撑宽列，因此**整卡布局零位移**。
- **「请放新卡」保持两行**：它所在的是两张卡片之间的窄缝（flowLayoutPanel），
  单行需要 ~56px 会把卡片挤开，反而破坏版面。若以后要统一，需先重排那一列。

---

## 十八、窗口改为 4:3 竖版，日志区吃满剩余高度（2026-10-27）

起因：用户截图把复制卡页**右侧一条竖带**画了红框（x≈762~820，全高），说「太空旷」，
要求「高宽比改为 4:3，尽量拉大监控面板」。

### 1. 那条空白带是窗口被硬顶到 900 宽造成的

两个原因叠加：

1. `ApplyAdaptiveLayout` 里 `form.MinimumSize = (min(900, …), …)` —— MinimumSize 是**外层窗口**尺寸，
   等于把宽度锁死在 900，无论内容多窄都缩不回去；
2. 撑宽用的是 `MeasureRequiredWidth`，量的是**容器右边缘**。而 `tableLayoutPanel3`
   （「尝试一下是否成功」那一格）是 Dock=Fill，宽度由窗口决定 —— 量它等于量窗口自己，
   于是「量宽度 → 撑窗口 → 再量更宽」，窗口启动一次就固定在 894 客户区。

实测三个页签的真实内容右缘：复制卡 **742**、高级操作 **769**、设置 **708**。
894 的客户区意味着复制卡页右侧白扔了 150px。

### 2. 定宽：改成「实测溢出量」迭代，最多三轮

不用「量最右内容」：本项目大量控件是 `Dock=Fill/Bottom` 或右锚定，位置由父容器决定，
量不出真实需求；而单元格内容比父表还宽的情况（窄窗口下 `tableLayoutPanel3` 溢出 41px）
只有溢出量能反映。做法（`UiTheme.MeasureOverflow`）：

```
width = 620
重复 ≤3 次：按 width 布局 → 量谁越过客户区右缘 → width += 溢出 + 4
width += 8（右边距）
```

### 3. 定高：外层窗口 高:宽 = 4:3

`clientH = (clientW + 边框) * 4/3 - 标题栏`，上限 = 屏幕工作区高度。
若内容所需宽度超过「4:3 在屏幕高度内允许的宽度」，**宽度优先**（比例略扁平），
不把卡片挤进换行/裁切。

1080p（工作区 1020）实测：**窗口 765×1020 = 精确 3:4**，客户区 **759×985**。

### 4. 日志区改成「吃剩余高度」

`FitTerminalHeight` 不再算 1:1、也不再改窗口高度，直接
`want = 客户区高 - 功能区高 - 状态栏高`：**331 → 627px（约 29 行）**。

| | 改前 | 改后 |
|---|---|---|
| 窗口 | 900×724 | **765×1020（4:3 竖版）** |
| 客户区 | 894×689 | 759×985 |
| 功能区 | 331 | 331（不变） |
| 日志区 | 331 | **627（+89%）** |
| 复制卡页右侧空白 | 150px | **10px**（与左边距 11 对称） |

### 5. 连带调整

- `MinimumSize` 宽度下限 900 → 560（否则宽度设不进去）；高度 480 → 420。
- 窗口尺寸变了以后 `CenterScreen` 的位置是按设计尺寸（726×559）算的，会停在左上角，
  改为定格后按工作区**重新居中一次**（`_sizeApplied` 静态标志，切主题不再把窗口拽回来）。
- 「停止」按钮本身是 `Anchor=Bottom,Right`，宽度收缩后**自动**贴到最后一格右下角（x=701），
  不用手工挪；仍属「孤悬」观感，等第 5 条统一处理。

### 6. 截图工具：新增 `-Print`（离屏渲染）

深色主题下 `SetForegroundWindow` 抢不到前台时，`CopyFromScreen` 抓到的是**壁纸**
（本轮连续三次都是），拿不到可验收的图。
`screenshot-tabs.ps1` 增加 `-Print`：改用 `PrintWindow(..., 2 = PW_RENDERFULLCONTENT)`
让窗口自己离屏画一遍，不依赖前台，深浅两套都能稳定出图。
（已知瑕疵：RichTextBox 偶尔少画一行，但 `--ui-info` 的 `TerminalLines` 可核对文本确实在。）

### 7. 验收

- `--ui-info`：`ClientSize=759x985`、`WindowSize=765x1020`、`Bounds={X=577,Y=0,...}`（水平居中）、
  终端 `627`、`tableLayoutPanel3` 右缘 `749 < 759`（无溢出）。
- 深浅两套 × 三页共 6 张截图：卡片均未换行、未裁切，右侧无空白带；
  `--ui-info` 新增 `TerminalFore/TerminalLines` 两行诊断，确认深色下三行欢迎语颜色正常
  （`#D4D4D4` on `#181818`）。

---

## 十九、改成方形窗口（787×787），并把「定宽」测量彻底修对（2026-10-27）

第十八节的 4:3 竖版（765×1020）已被本节取代：用户要求「改成 675×675 试试看」。

### 1. 675×675 放不下，先给了证据

| 窗口 | 客户区 | 高级操作页最右卡片 | 结果 |
|---|---|---|---|
| 675×675 | 669 | 769 | 溢出 100px：第 4 张卡（尝试/停止）被切，高级页 C/FUID 卡被切 |
| 765×765 | 759 | 769 | 仍溢出 10px（右缘卡片贴边切掉边框） |
| **787×787** | **781** | **769** | ✅ 12px 右边距 |

四列卡片（含三个 7px 间隔 + 「请放新卡」竖排标签）的硬需求是 **769px**，
所以窗口宽度不可能低于 ~780；要更小只能改流程区排布（2 行或图标化），本轮没动。

### 2. 定宽算法：三次作废，最后落在「探针量一次」

前两版的毛病（都实测踩过，写下来免得再走）：

1. **按 Dock/Anchor 排除后量最右内容** —— 正文里的「尝试一下是否成功」本身就是
   `Dock=Fill`，被排除后量到 698，窗口被压到 712，卡片直接溢出 41px。
2. **边改边量、按溢出量迭代** —— 流式容器在临界宽度上换不换行是**非单调**的，
   同一次收敛会在 767~801 之间乱跳；而且改高度也会触发整树重排，
   会把手已经排好的卡片弹回第一行。
3. **量之前不同步未选中页签** —— `TabPage` 没被选中时尺寸是上一次的旧值（残留 839px），
   把窗口虚撑到 855；更坑的是未选中页的 `Visible=false`，若按 Visible 剪枝
   就完全量不到（漏掉高级页那 8~100px 溢出）。

最终实现：

- `SyncPageSizes`：量之前把所有 TabPage 的宽度同步到当前 TabControl（只改尺寸，不切页签）。
- 在 **工作区宽度**的临时尺寸下布局一次（宽到不会溢出），
  再用 `MeasureContentRight` 取「不铺满父容器」的控件右缘最大值
  —— 判据是「右缘距父容器右缘 < 6px 就算被拉伸」，比 Dock/Anchor 判据稳，
  因为正文标签本身就可能是 Dock=Fill。
- `width = 内容右缘 + 12`；`clientH = width + 边框 - 标题栏`（外层正方形），上限工作区。

1080p 实测：**窗口 787×787**（精确 1:1），客户区 781×752，
功能区 331 + 日志 **394** + 状态栏 27。

### 3. 调试开关：`--win-size WxH`

```
M1ToolPlus.exe --win-size 675x675
set M1TOOL_WINSIZE=675x675 && M1ToolPlus.exe
```
跳过自适应计算，按给定**外层窗口**尺寸定格，用来快速比对版式（本轮就是靠它扫的宽度）。

### 4. 验收

- `--ui-info`：`ClientSize=781x752`、`WindowSize=787x787`、`Bounds={X=566,Y=116}`（居中）、
  终端 394；三页内容右缘 767 / 769 / 708，均 < 781。
- 深浅两套 × 三页 6 张截图：四列卡片、「尝试一下是否成功」、「停止」按钮全部完整，
  高级页 C/FUID 卡与设置页「优化设置」卡都有可见右边距。

---

## 二十、787×787 定稿后的观感优化（2026-10-27）

窗口尺寸按用户确认的 **787×787** 定稿，本轮只修「看着不完美」的地方。

### 1. 复制卡页第 4 格补成卡片

原来第 4 格（勾选图标 +「尝试一下是否成功」+「停止」）直接坐在页签底色上：
中间一大片空白、红色按钮孤零零悬在右下角，和前三张白卡完全不搭。
`UiTheme.StyleCardPanel` 把 `tableLayoutPanel3` 按分组卡同款处理——白底（深色 `#333`）
+ 圆角描边，四条边与前三张卡齐平。

- 不能在 `tableLayoutPanel3` 自己的 Paint 里画圆角描边就完事：圆角外那四个角要先用
  页签底色盖掉，否则页签底色会在圆角处露成直角（与 `PaintRoundGroupBox` 同一手法）。
- 「停止」是 `Anchor=Bottom,Right`，原来距卡片右边 3px、下边 2px，贴着边线。
  直接改它的 `Left` **无效**（TableLayoutPanel 布局会覆盖回去，实测两次都没生效），
  改成给卡片 `Padding=(0,0,6,6)`，锚定按内边距算，现在是右 9px / 下 8px。

### 2. 指定设备：空下拉框给占位文案

没检测到设备时下拉框是一个纯白空框，用户反馈「像坏了」。
`ApplySettings` 里补一条占位项「（未检测到设备）」；`SetDeviceCombo_SelectedIndexChanged`
里遇到占位项直接 return，不会写进 `libnfc.conf`。检测到设备时 `Items.Clear()` 会把它顶掉。

### 3. 日志区文字离窗边留 8px

`RichTextBox` 的 `Padding` 属性无效，走 `EM_SETMARGINS`（左右各 8px）。
首行原来距窗口左缘只有 2px，一排 hex 输出顶到边上。

### 4. 色块与数字框左边缘对齐

设置页「终端文字颜色」色块与「终端文字大小」数字框同列，但 `Margin` 不同，
实测数字框右移 11px。`AlignCardControls` 用 Margin 差值纠正（TLP 按 Margin 定位，
改 `Left` 会被布局覆盖）；已对齐时 dx=0，切主题重复调用无副作用。

### 5. 定宽算法彻底改为「固定尺寸 + 溢出体检」

本轮又试了两版在线测量，都被这套布局打脸，最终放弃自动测量：

| 方案 | 实测结果 |
|---|---|
| 在探针宽度下量「最右内容」（排除 Dock/右锚定） | 760 被撑到 **1901**（设置页一个铺满的标签被当成内容） |
| 同上，加「宽度≈父容器」判据 | 仍 1901（铺满标签的宽度没到父容器 -12） |
| 边改边量、按溢出量收敛 | 767~801 之间乱跳，且改高度会触发重排把卡片弹回第一行 |

结论：**尺寸写死 787**（= 内容 769 + 右边距 12 + 边框 6），高度同值即正方形，
仍受屏幕工作区钳制。原来那套测量改成「体检」：`--ui-info` 新增
`Overflow=0 (0=内容未越界, >0=被右缘裁切)`，换 DPI/字号后一旦溢出能立刻发现，
不再靠猜。

### 验收

`--ui-info`：`ClientSize=781x752`、`WindowSize=787x787`、`Overflow=0`、
日志 394、`buttonEStop` 距卡片边 9/8px。深浅两套 × 三页共 6 张截图核对：
- 复制卡页：四张卡（第 4 张已成形）+ 勾选 +「尝试一下是否成功」+ 停止全部完整；
- 高级操作页：C/FUID 卡有可见右边距，两行卡片无裁切；
- 设置页：占位文案「（未检测到设备）」、色块与数字框左对齐。

---

## 二十一、v1.8.1：版本号与两处文字观感修正（2026-10-27）

本分支定为 **v1.8.1.0**（`Properties/AssemblyInfo.cs` 的 `AssemblyVersion` /
`AssemblyFileVersion` 同步）；`Settings.Upgrade()` 已能把 1.8.0 的偏好迁到新版本目录。

### 1. 状态栏「检查更新」文字色与左侧状态不一致

`toolStripCheckUpdate` 是 `StatusStrip` 里的 `ToolStripSplitButton`，Designer 里写死
`ForeColor = SystemColors.ControlText`（纯黑）。项自带的 ForeColor 不继承状态栏的
`DTextDim / LTextDim`，于是左边「空闲 / 运行时间 / 本地版本」是灰蓝，右边「检查更新」
是纯黑，深色主题下更明显。

- 删掉 Designer 里的硬编码 ForeColor；
- `UiTheme.StyleStatusStrip` 逐项覆盖 `ForeColor`（含 SplitButton 的下拉项），
  再在 `StatusRenderer.OnRenderItemText` 统一给 `DTextDim / LTextDim`——渲染层兜底，
  以后谁再给某一项写死颜色也不会破坏一致性。

### 2. 「外观主题」四字不要把行拆开

设置页原来组标题只写「外观」，组内再放一个「主题」标签，视觉上像四个字被折成两行。
合并成一个组标题「外观主题」（新增资源键 `外观主题`），组内只留下拉框，
`flowLayoutPanelTheme` 下移到 `(6, 24)` 给组框标题让位。

### 3. 「终端文字颜色」色块太大

`buttonCLIColor` 在 resx 里是 `AutoSize=True` + `Dock=Fill`，放进
`tableLayoutPanel12` 后跟着行高（47px）撑满，比下面 21px 的数字框高出一倍多，
一块大色板压在「终端文字颜色」右侧，看着像没做完的控件。

- 色块改成 21px 高的横条：`AutoSize=false` + `Dock=None` +
  `Anchor=Left|Right`（不含上下 = 在单元格内垂直居中，与左侧标签同一行），
  宽度按 `#RRGGBB` 实测（约 73px，7 个字符固定宽度）。
- 数字框跟着同宽：AutoSize=True 的 NumericUpDown 只认内容首选宽度，
  单靠 Anchor 拉不动，必须 `AutoSize=false` 再左右锚定 + 保留 Top
  （去掉 Top 会垂直居中，与「终端文字大小」标签错位）。

### 验收

`scripts/build.ps1 -Test`：19/19 PASS，exe 711 KB / v1.8.1.0；
三页截图核对：状态栏右侧「检查更新」与「本地版本」同色，设置页标题为单行「外观主题」，
色块与数字框同为 76×21、右缘齐平且各自与左侧标签对齐。

---

## 二十二、README 重构（2026-10-27，v1.8.1）

原来的 README 是纯文字功能清单，新用户第一眼看不到「这是什么软件 / 长什么样 / 能不能用我的卡」。
本轮按「首屏回答三个问题」重写，功能表基本沿用（原文已经准确），改动集中在结构、截图与纠错：

### 1. 首屏：一句话定位 + 实机截图 + 版本差异

- 标题下补英文副标题与四枚徽章（许可 / 最新 Release / 平台 / 运行库）；
- 放一张 787×787 实机截图 `docs/images/main-copy.png`（复制卡模式四张卡 + 终端欢迎语）；
- 「这个版本有什么不同」用六条说清相对原作者 v1.7.0 的增量：修老问题、界面重做、
  单语言、检查更新、MFF08 救砖、文档齐全。这一节是用户判断「该不该换」的关键，
  原来埋在许可证声明里只有一句「非官方衍生版本」。

### 2. 新增「支持的设备与卡型」速查

原 README 只有读卡器表，卡型兼容矩阵藏在 USERGUIDE。现在给一张「卡型 → 能做什么」的
四行速查（S50 / S70 / 魔改卡 / CPU 模拟卡），完整矩阵仍指向手册 §2。

### 3. 新增「常见问题」表

六条最高频现象（检测慢、认证失败、终端乱码、主题回滚、杀软误报、0 块写坏）
每条给一句可执行处理，细节点到 TROUBLESHOOTING 对应章节。原来只有一个链接。

### 4. 纠错与对齐

- 「简体中文（v1.9.0 起只保留…）」→ 版本号写错，本分支实际是 **v1.8.1**；
- 「终端配色 #M1T#」→ 色块现在直接显示 `#RRGGBB`，且本轮刚改成 21px 横条；
- 下载步骤的按钮名与实机对齐：「检测连接」（复制卡页）/「检测设备」（高级页）；
- 首次使用路径改为实机按钮：检测连接 → 扫描卡片 → 已知密钥读回 / 从 UID 卡读回 → 写入普通卡；
- 补系统要求（Win7 SP1 + .NET 4.0，建议 Win10/11）与「无需安装」的准确措辞；
- `docs/TROUBLESHOOTING.md` 里 user.config 路径写死 1.7.0.0 → 改为 `<版本号>` 占位。

### 5. 文档索引

末尾加一张表把 USERGUIDE / TROUBLESHOOTING / DEPENDENCIES / DEVELOPMENT / ROADMAP /
原作者 README 存档逐项说明用途，替代原来散落在各段的链接。

### 6. 美化：本地徽章 + 排版细节（2026-10-27）

- **徽章改为仓库内的 SVG**（`docs/images/badges/`）：shields.io 在国内和离线环境常被墙，
  首屏会留一片破图。现在 2 个主按钮 + 5 个信息徽章都是本地静态 SVG，
  不依赖外网、不受镜像缓存影响；`release v1.8.1` 是静态值，发版时手动改。
- **顶部结构**：`<h1>` 标题 → 一句定位标语 → 主按钮行 → 徽章行 → 一行目录锚点 →
  实机截图，全部 `align="center"`，首屏在 1000px 宽下不用滚动就能看到截图。
- **表格**：功能表按「读写 / 破解 / 魔改 / 救砖 / 通用」拆成五个小标题，
  卡型矩阵改用 `:---:` 居中列，勾叉符号不再参差。
- **emoji**：每条特性/问题保留一个 emoji 当视觉锚点，但「只保留简体中文」原来的
  🈶（日文「有料」按钮）语义错位、Windows 字体下渲染成彩色方块，换成 🧹。
- **文案与实机再对齐**：`界面主题` → 实机组标题是 **外观主题**；
  `终端外观` → 实机的标签是**终端文字颜色 / 终端文字大小**（都在「界面设置」组）。
  表格首列因此收成 3 字以内（`主题` / `终端`），UI 原名写进说明列——见下条。
- **表格首列被挤窄的坑**：GitHub 的 `table{width:max-content;max-width:100%}` 在整表超宽时
  会按比例压缩各列，说明列越长，首列越窄，4 个汉字就会折成「外观主 / 题」。
  对策是把长说明移出表格（表后一段话），首列控制在 3 字内、说明列不超过约 40 字。
  改完 9 张表首列全部单行。
- 字号默认值核对代码后确认是 **12 pt**（`DefaultTerminalFontPt`，且启动时
  `TerminalFontVersion < 2` 会把老配置统一提升到 12），不是 `Settings.settings`
  里那个没生效的 10。

### 校验

9 张表列数一致；16 个相对链接（LICENSE + 6 篇文档 + 2 张截图 + 7 个 SVG）全部可解析；
用无头 Edge 按 GitHub 样式渲染全页，逐屏核对徽章、截图、表格与末尾说明。

---

## 二十三、release.yml：Tag 触发的自动发版流水线（2026-10-27，v1.8.1）

需求：推完 tag 之后，自动构建并把软件本体挂到 Release。

### 1. 发现的关键阻塞：nfc-bin 不在版本库

`M1ToolPlus/nfc-bin/` 被 `.gitignore` 排除（`git ls-files` 为 0），而 `build.ps1` 打包时
发现缺依赖只是 `Write-Warning`。照搬现有 CI 的构建步骤去发布，runner 会 checkout 出一个
不含依赖的仓库 → 编译通过 → 打包成功 → Actions 全绿，但发出去的包**能安装能启动、
却读不了卡**。这种失败最难发现，所以流水线第一步必须是 `.\scripts\fetch-deps.ps1`
（从上游 v1.7.0 Release 还原 nfc-bin），并把"缺依赖"在 CI 侧升级为硬失败。

### 2. 三道护栏

1. **依赖还原**（上条）；
2. **tag 与版本号对齐**：tag `v1.8.1` 必须与 `AssemblyVersion` 前三位一致。
   踩到的坑——`AssemblyInfo.cs` 第 34 行有一行注释模板 `// [assembly: AssemblyVersion("1.0.*")]`，
   朴素正则会先匹配到它，正则必须锚定行首 `^\[assembly:`；
3. **产物在位校验**：exe / Newtonsoft.Json.dll / app.config / zh 卫星 / 9 个 nfc-bin 工具，逐个确认。

### 3. 其它设计取舍

- **不与 CI 合并**：`build.yml` 已是 `on: push` + `pull_request`，再塞 `if:` 判断可读性差；
  独立成 `release.yml`，职责清楚。推 tag 时两个 workflow 都会跑（CI 那份算双保险）。
- **零第三方 Action**：发布用 runner 自带的 `gh`（配 `GH_TOKEN`），不引入
  softprops/action-gh-release 这类社区依赖；已存在 Release 时走 `gh release upload --clobber`。
- **附件名稳定**：`-Pack` 输出带时间戳，CI 里重命名为 `M1ToolPlus-<tag>.zip` + `SHA256SUMS.txt`，
  便于脚本比对与自更新链路。
- 支持 `workflow_dispatch` 手动触发并可勾选 draft，用于首次上线试跑。

### 4. 遗留风险（已写入 DEVELOPMENT 技术债）

`fetch-deps.ps1` 依赖上游 `xcicode/MifareOneTool` 的 v1.7.0 Release 存续；
后续应把 `M1T-Release.zip` 作为长期资产挂到自家 Release，并让拉取顺序改为自家优先。
