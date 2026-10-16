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
| 分支/状态 | `dev` 分支，本地领先 origin/dev **8 个提交**，**全部未推送** |
| origin | `https://github.com/xcicode/MifareOneTool.git`（原作者仓库，**只作参考，切勿 push**） |
| 构建产物 | `M1ToolPlus\bin\Release\M1ToolPlus.exe`（+ en/ru/zh 卫星程序集 + nfc-bin） |
| 构建命令 | `.\scripts\build.ps1`（参数见 README / docs/DEVELOPMENT.md） |
| 用途声明 | 仅限对你有授权的卡片的安全研究（README 同样声明） |

## 二、已完成（8 个本地提交）

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

1. **在 GitHub 建仓库**（建议名 M1ToolPlus）→ `git remote add my <新仓库地址>` → `git push my dev`。
   **切勿推送到 origin**（原作者仓库）。
2. 复核两项暂缓决策（发布前定下来）：
   - `M1ToolPlus\app.config` 与 Settings 中 `GitHubR = xcicode/MifareOneTool`：目前为休眠配置（GitHubUpdate 类未启用）。若启用更新检查，需改为新仓库。
   - `M1ToolPlus\Form1.cs:1261` 硬编码原作者 releases 链接（下载最新版按钮），发布时决定改为新仓库或保留双链接。
3. 打开发布包：`.\scripts\build.ps1 -Pack` → `artifacts\M1ToolPlus-local-build.zip`；
   按 `docs/DEPENDENCIES.md` 的发布检查清单核对（重点：**不得打包 winhex.exe**；mff08.exe 来源不明，是否随包待定）。
4. 版本号策略：AssemblyInfo 仍为 1.7.0.0（延续原版），首次发布前决定新版本号。

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

## 五、接手检查清单（新会话/新开发者第一步）

1. 确认工作目录已是 `D:\repositories\M1ToolPlus`（若会话仍指向旧路径，先切换/移动会话）。
2. `git status --short` 应为空；当前分支应为 `dev`。
3. `git log --oneline -3` 应包含 `1b97708`（更名）与 `19d6094`（README 存档）。
4. 快速构建验证：`.\scripts\build.ps1 -Clean`，确认输出 `M1ToolPlus.exe` + en/ru/zh 卫星。
5. 阅读：README.md、docs/DEVELOPMENT.md、docs/DEPENDENCIES.md、docs/TROUBLESHOOTING.md，然后按「三、待办」从 A 开始推进。
