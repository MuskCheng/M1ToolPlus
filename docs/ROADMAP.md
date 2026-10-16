# 后续开发路线图（交接备忘）

> 面向接续开发的交接文档：记录项目现状、已完成工作、待办事项与开发环境要点。
> 最后更新：2026-10-16（项目更名为 M1ToolPlus 时）。
> 注意：本仓库原位于 `D:\repositories\MifareOneTool`，已更名为 **`D:\repositories\M1ToolPlus`**（纯文件夹改名，git 历史与远程配置不受影响）。

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

## 三、待办（按优先级）

### A. 发布准备（推送前必做/必决策）

1. ~~**在 GitHub 建仓库并推送**~~ ✅ 已完成（2026-10-16）：`https://github.com/MuskCheng/M1ToolPlus`（public，默认分支 `dev`，GitHub 已识别 GPL-3.0，已设置简介与 10 个 topics）。远程布局：`origin` = 本人仓库，`upstream` = 原作者仓库（只读）。
2. 复核两项暂缓决策（发布前定下来）：
   - `M1ToolPlus\app.config` 与 Settings 中 `GitHubR = xcicode/MifareOneTool`：目前为休眠配置（GitHubUpdate 类未启用）。若启用更新检查，需改为新仓库。
   - `M1ToolPlus\Form1.cs:1261` 硬编码原作者 releases 链接（下载最新版按钮），发布时决定改为新仓库或保留双链接。
3. 打开发布包：`.\scripts\build.ps1 -Pack` → `artifacts\M1ToolPlus-local-build.zip`；
   按 `docs/DEPENDENCIES.md` 的发布检查清单核对（重点：**不得打包 winhex.exe**；mff08.exe 来源不明，是否随包待定）。
4. 版本号策略：AssemblyInfo 仍为 1.7.0.0（延续原版），首次发布前决定新版本号。

### A' 发布包合规核对（2026-10-16 实测：`artifacts\M1ToolPlus-local-build.zip`，53 条目 / 3.0 MB）

| 检查项 | 结果 |
|---|---|
| 主程序为 `M1ToolPlus.exe` + `M1ToolPlus.exe.config` | ✅ |
| en/ru/zh 三套卫星程序集齐全 | ✅ |
| winhex.exe 未打包（专有软件） | ✅ |
| **mff08.exe 仍在包内** | ❌ 与既定策略冲突（来源不明），**待决策**：剔除 + GUI 加"文件缺失"友好提示，或保留并在发布说明中标注来源不明 |
| nfc-bin 内 `*.bak`（libnfc.dll.bak、libnfc(532).dll 等） | 必须保留：Form1 的设备切换（ACR122U/PN532  dll 三交换）依赖这些备份 |
| nfc-bin\data\logs_proxmark3\trace*.txt | 官方包自带的日志残留，无害，可选清理 |

### B. 真机回归（本开发环境无硬件，必须待真机）

- Issue #26 修复实测：PN532 / ACR122U 启动与设备恢复。
- en/ru/zh 界面切换实测（下拉索引 0=zh / 1=ru / 2=en）。
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
- **语言资源**：切换按“下拉索引”映射（0=zh-cn 1=ru-RU 2=en-US）；新增语言三步：加 `.xx.resx` 并注册 csproj、combo 加一项、switch 加分支。
- 依赖再获取：`.\scripts\fetch-deps.ps1`（默认从原作者 releases v1.7.0 的 M1T-Release.zip 回收 nfc-bin）。
- **浅克隆教训（重要）**：本仓库最初从原作者克隆时使用了 `--depth`（浅克隆），推送到空仓库时报 `remote unpack failed: did not receive expected object`，且该对象在本地也不存在（`git cat-file` 失败）；已用 `git fetch --unshallow upstream` 修复（现 109 个提交、非浅克隆）。今后重建克隆**不要加 `--depth`**。

## 五、接手检查清单（新会话/新开发者第一步）

1. 确认工作目录已是 `D:\repositories\M1ToolPlus`（若会话仍指向旧路径，先切换/移动会话）。
2. `git status --short` 应为空；当前分支应为 `dev`。
3. `git log --oneline -3` 应包含 `1b97708`（更名）与 `19d6094`（README 存档）。
4. 快速构建验证：`.\scripts\build.ps1 -Clean`，确认输出 `M1ToolPlus.exe` + en/ru/zh 卫星。
5. 阅读：README.md、docs/DEVELOPMENT.md、docs/DEPENDENCIES.md、docs/TROUBLESHOOTING.md，然后按「三、待办」从 A 开始推进。

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
