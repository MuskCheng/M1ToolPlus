# M1ToolPlus

**Windows 平台的 Mifare Classic（M1 卡）图形化工具**
A GUI Mifare Classic tool on Windows

[![License](https://img.shields.io/badge/license-GPL--3.0-blue.svg)](LICENSE) [![Release](https://img.shields.io/github/release/MuskCheng/M1ToolPlus.svg)](https://github.com/MuskCheng/M1ToolPlus/releases/latest)

---

## 功能一览

### 读卡 / 写卡 / 管理

| 功能 | 说明 |
|---|---|
| **检测设备** | 检测 NFC 读卡器连接状态，启动后第一步 |
| **手动扫描** | 扫描读卡器上的卡片并显示基本信息（UID、SAK、ATQA 等） |
| **读 M1** | 读取普通 M1 卡数据。默认密钥卡/空白卡可直接读，非默认密钥需指定密钥文件 |
| **写 M1** | 写入数据到普通卡片，配合「数据写入保护」防止写入失败损坏数据 |
| **清 M1** | 使用选中的密钥文件清空卡内全部数据 |
| **选择 key.mfd** | 指定密钥文件；也可启用「自动加载 uid.Key 文件」按卡号自动匹配 |
| **检加密** | 查看卡片各扇区的加密情况，判断用哪种方案 |
| **差异比较** | 对比两个 dump 文件的扇区差异，快速定位修改点 |
| **Hex 编辑器** | 内置 S50HTool，查看/编辑卡数据（dump），所见即所得 |

### 密钥破解与恢复

| 功能 | 说明 |
|---|---|
| **MFOC** | 对半加密卡（常见 SAK=08）执行 Nested 攻击恢复密钥；**按住 Ctrl 点击**可添加已知密钥 |
| **知 n 密 / 字典测试** | 已知部分密钥时定向补全其余扇区；字典方式批量测试 |
| **全加密爆破** | 调用 MFCUK 对全加密卡执行 Darkside 爆破 |
| **HardNested** | 专用窗口执行 Hardnested 攻击（含低成本/单线程计算开关），破解强加密卡 |

### 魔改卡（UID / CUID / FUID / UFUID）专用

| 功能 | 说明 |
|---|---|
| **UID 读 / UID 写** | 后门指令直读/直写，无视扇区密钥（需魔改卡） |
| **UID 写号** | 将魔改卡 UID 设置为指定号码 |
| **UID 重置** | 将 UID 重置为随机数 |
| **UID 全格** | 重置 UID 并清空全部扇区数据、恢复出厂访问控制位 |
| **CUID 写** | 用普通指令写 0 块（CUID 卡），配合「CUID 写空卡补丁」处理空卡 |
| **锁 Ufuid** | 锁定 UFUID 卡 0 扇区，锁定后不可再写，作为普通使用的定版操作 |

### 救砖：MFF08 CUID 修复

写卡失败把卡片 **0 块（UID 块）写坏**后，卡会变砖（读卡器认不出、密钥校验全失效）。
「MFF08」对话框可将全空的出厂数据**直写回 0 块**救活卡片，支持两种修复方式：
**A** = 卡仍是全 F 出厂密钥时直接写回；**C** = 用「最后一次写坏这张卡时保存的 dump」里的密钥写回。
（仅对可写 0 块的 CUID/Gen1a 类魔改卡有效；正版卡 0 块只读，不会被此操作碰到。）

### 通用工具

| 功能 | 说明 |
|---|---|
| **手动 CLI** | 打开命令行并定位到 `nfc-bin\`，可手工调用 libnfc 等工具（进阶玩法） |
| **清终端 / 存日志** | 清空终端视图；将终端完整内容保存为 `m1t.log`（UTF-8） |
| **停运行** | 强行终止正在执行的操作 |

### 界面与终端体验

| 项目 | 说明 |
|---|---|
| **界面主题** | 跟随系统 / 深色 / 浅色，切换立即生效；配置、对比、Hex 编辑、修复等子窗口全部同步换色 |
| **界面语言** | 简体中文（默认）/ Русский / English，含操作提示与错误信息 |
| **终端字体** | 字体族/字号在设置页统一管理（默认 12pt，比界面字号大一号、更易读）；终端内 **Ctrl+滚轮** 缩放并与设置页联动 |
| **终端配色** | 可自定义终端配色（`#M1T#`） |
| **高 DPI 支持** | 100% / 125% / 150% 缩放下窗口 1:1 显示、文字清晰不发虚；主窗口固定大小，无内容拉伸 |

---

## 兼容设备

| 读卡器 | 说明 |
|---|---|
| **PN532-UART（USB-TTL）** | 推荐方案，连接与兼容性最好 |
| **ACR122U** | 支持但默认关闭，需在设置中手动启用（启用后部分操作速度会变慢） |

> PN532 板常见搭载 CH340/CH341 串口芯片，驱动请自行到沁恒官网安装（不随包分发）。

## 下载与运行

1. 从 [Releases](https://github.com/MuskCheng/M1ToolPlus/releases/latest) 下载最新压缩包
2. 解压到独立目录（建议路径不含空格）
3. 双击 `M1ToolPlus.exe` —— 插入读卡器后点「检测设备」确认即可

无附加安装、无联网依赖：完整界面 + 第三方命令行工具（libnfc 组、MFOC、MFCUK、Hardnested、mfdetect、MFF08 修复）都在包内。
更多完整使用说明可参考原作者的 [Wiki](https://github.com/xcicode/MifareOneTool/wiki)（内容对两版基本适用）。

## 使用须知

- 本工具**仅用于你有授权的卡片**（自己的门卡、可复制的白卡、合法获取的 UID 测试卡等），滥用与本仓库无关。
- **写卡有风险**：写错密钥或写入半途失败可能把 0 块写坏导致卡片变砖；写卡前先读出完整 dump 备份，损坏后可尝试 MFF08 修复（仅魔改卡）。
- 开启「**数据写入保护（建议）**」可显著降低写坏卡片的风险，非批量任务请保持开启。

## 第三方依赖与合规

主程序为 GPL-3.0 开源。运行依赖的 `nfc-bin\` 来自原作者官方发布（[libnfc](https://github.com/libnfc/libnfc)、
[mfoc](https://github.com/eubuntu/mfoc)、[mfcuk](https://github.com/xxmagic/mfcuk)、
[libnfc-hardnested](https://github.com/nlpub/libnfc-hardnested) 等，均为开源项目产物），逐项来源与许可见
[docs/DEPENDENCIES.md](docs/DEPENDENCIES.md)；其中 `mff08.exe` 为原作者自编译、源码未公开的救砖工具，
按其官方 v1.7.0 发行注记原样附带，如需源码可依 GPL-3.0 第 4 条向原作者索取。
包内**不含** winhex.exe（X-Ways 专有软件），Hex 编辑使用内置 S50HTool。

---

## 许可证与衍生版本声明

本项目以 **[GPL-3.0](LICENSE)** 发布；感谢原作者与各上游开源项目，第三方依赖的逐项来源与许可见
[docs/DEPENDENCIES.md](docs/DEPENDENCIES.md)。

本仓库是 [xcicode/MifareOneTool](https://github.com/xcicode/MifareOneTool) 的**非官方衍生版本**（modified version），
自 2026 年 10 月起由 M1ToolPlus 项目独立修改与维护，与原作者无隶属关系、未获其背书
（not affiliated with or endorsed by the original author）。原始项目 Copyright © 2018-2019 XciCode 及贡献者，
GPL-3.0；本仓库的全部修改同样以 GPL-3.0 发布，修改明细见 git 提交历史。原作者已停止更新
（最后一版 v1.7.0，2019），本仓库为其独立社区延续版（an independent community continuation under GPL-3.0）；
原作者最后一版完整 README 已存档：[docs/README.original.md](docs/README.original.md)。 
