# MifareOneTool

A GUI Mifare Classic tool on Windows（Windows 平台 Mifare Classic 图形化工具）

> 原作者已停止维护（v1.7.0, 2019）。本仓库为接手续开发版本，遵循原 GPL-3.0 协议。
> The original author discontinued development (v1.7.0, 2019). This is a continuation under GPL-3.0.

## 功能

- 支持 PN532（推荐）/ ACR122U 读卡器（libnfc）
- 读/写/格式化 M1 卡，MFOC 暴力破解、MFCUK 恢复 Key
- Hardnested 攻击、UID/CUID/FUID/UFUID 写卡、卡对比、HEX 编辑器
- 界面语言：简体中文（默认）/ 俄语 / 英语（开发中）

## 快速开始（用户）

1. 从 [Releases](https://github.com/xcicode/MifareOneTool/releases) 下载最新 `M1T-Release.zip`
2. 解压后双击 `MifareOneTool.exe`
3. 详细使用见 [Wiki](https://github.com/xcicode/MifareOneTool/wiki)

## 构建（开发者）

```powershell
git clone <本仓库> && cd MifareOneTool
.\scripts\build.ps1              # Release 构建 -> MifareOneTool\bin\Release\
.\scripts\build.ps1 -Pack        # 构建 + 打包 -> artifacts\M1T-local-build.zip
.\scripts\fetch-deps.ps1         # 回收 nfc-bin 运行依赖（首次需要）
.\scripts\build.ps1 -Clean       # 清理重建
```

要求：Windows + .NET Framework 4.0 目标包 + MSBuild（VS 2022 Build Tools 即可）。
无需 Windows SDK：`build.ps1` 会自动用 `csc` 绕过缺失的 `al.exe` 生成中/俄文卫星程序集。

完整开发文档见 [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)，第三方依赖与合规见 [docs/DEPENDENCIES.md](docs/DEPENDENCIES.md)。

## 目录结构

| 路径 | 说明 |
|---|---|
| `MifareOneTool\` | C# WinForms 源码工程（.NET Framework 4.0 Client, x86） |
| `scripts\` | 构建 / 依赖回收 / 打包脚本 |
| `docs\` | 开发文档、依赖合规说明 |
| `wiki\` | GitHub Wiki 镜像（独立仓库，不入主库） |
| `artifacts\` | 发布产物与第三方二进制（gitignore） |

## License

GPL-3.0（见 [LICENSE](LICENSE)）。工具仅限用于你有授权的卡片。
