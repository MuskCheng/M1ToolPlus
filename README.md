# M1ToolPlus

A GUI Mifare Classic tool on Windows（Windows 平台 Mifare Classic 图形化工具）

> **衍生版本说明 / Derivative work notice**
> 本仓库是 [xcicode/MifareOneTool](https://github.com/xcicode/MifareOneTool) 的**非官方衍生版本**（modified version），
> 自 2026 年 10 月起由 M1ToolPlus 项目独立修改与维护，与原作者无隶属关系、未获其背书
> （not affiliated with or endorsed by the original author）。
> 原始项目 Copyright © 2018-2019 XciCode 及贡献者，GPL-3.0；本仓库的全部修改同样以 GPL-3.0 发布，修改明细见 git 提交历史。
> The original author discontinued development (last release v1.7.0, 2019). This is an independent community continuation under GPL-3.0.
> 原作者最后一版完整 README 已存档：[docs/README.original.md](docs/README.original.md)。

## 功能

- 支持 PN532（推荐）/ ACR122U 读卡器（libnfc）
- 读/写/格式化 M1 卡，MFOC 暴力破解、MFCUK 恢复 Key
- Hardnested 攻击、UID/CUID/FUID/UFUID 写卡、卡对比、HEX 编辑器
- 界面语言：简体中文（默认）/ 英语 / 俄语

## 快速开始（用户）

1. 从 [Releases](https://github.com/xcicode/MifareOneTool/releases) 下载原作者最新 `M1T-Release.zip`
2. 解压后双击主程序（原作者包内为 `MifareOneTool.exe`；本仓库构建产物为 `M1ToolPlus.exe`）
3. 详细使用见 [Wiki](https://github.com/xcicode/MifareOneTool/wiki)

## 构建（开发者）

```powershell
git clone <本仓库> && cd M1ToolPlus
.\scripts\build.ps1              # Release 构建 -> M1ToolPlus\bin\Release\
.\scripts\build.ps1 -Pack        # 构建 + 打包 -> artifacts\M1ToolPlus-local-build.zip
.\scripts\fetch-deps.ps1         # 回收 nfc-bin 运行依赖（首次需要）
.\scripts\build.ps1 -Clean       # 清理重建
```

要求：Windows + .NET Framework 4.0 目标包 + MSBuild（VS 2022 Build Tools 即可）。
无需 Windows SDK：`build.ps1` 会自动用 `csc` 绕过缺失的 `al.exe` 生成中/俄文卫星程序集。

完整开发文档见 [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)，第三方依赖与合规见 [docs/DEPENDENCIES.md](docs/DEPENDENCIES.md)，常见问题见 [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)。

## 目录结构

| 路径 | 说明 |
|---|---|
| `M1ToolPlus\` | C# WinForms 源码工程（.NET Framework 4.0 Client, x86） |
| `scripts\` | 构建 / 依赖回收 / 打包脚本 |
| `docs\` | 开发文档、依赖合规说明 |
| `wiki\` | GitHub Wiki 镜像（独立仓库，不入主库） |
| `artifacts\` | 发布产物与第三方二进制（gitignore） |

## License

GPL-3.0（见 [LICENSE](LICENSE)）。工具仅限用于你有授权的卡片。
