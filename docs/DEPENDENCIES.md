# 第三方依赖与合规说明

M1ToolPlus 主程序（GPL-3.0）之外还依赖一批二进制工具。本文档记录来源、
许可与分发策略。**分发任何打包产物前先看这里。**

## 一、nfc-bin\（运行核心，随包分发）

GUI 通过子进程调用这些工具完成全部 NFC 操作。二进制来自官方
`M1T-Release.zip`（v1.7.0），均可溯源到开源项目：

| 文件 | 来源项目 | 许可 | 备注 |
|---|---|---|---|
| `nfc-*.exe`（nfc-list、nfc-mfclassic、nfc-mfsetuid 等 20+） | [libnfc](https://github.com/libnfc/libnfc) examples（版本信息标注 libnfc 1.7.1） | LGPL-3.0 | `mff08.exe` 版本信息同为 libnfc release，应为作者基于 libnfc examples 自行编译的定制工具，**源码未公开** |
| `libnfc.dll` / `libnfc(532).dll` / `libnfc(ACR122Uslow).dll` | libnfc 1.7.x + Windows 移植 | LGPL-3.0 | 变体用于 PN532/ACR122U 切换（改名交换法） |
| `mfoc.exe` / `mfoc2.exe` | [mfoc](https://github.com/eubuntu/mfoc) | BSD-3-Clause | 暴力破解已知/未知扇区 Key |
| `mfcuk.exe` / `mfcuk2.exe` | [mfcuk](https://github.com/xxmagic/mfcuk) | BSD-3-Clause | 恢复 Key 攻击 |
| `libnfc_hardnested.exe` / `libnfc_hardnestedlc.exe` | [libnfc-hardnested](https://github.com/nlpub/libnfc-hardnested)（Proxmark3 算法移植） | GPL-3.0 | 与主程序同许可，无冲突 |
| `collect.exe`（Hardnested nonce 采集） | 上游为 [aczid/crypto1_bs](https://github.com/aczid/crypto1_bs) 内附的 libnfc 采集程序（"libnfc app + bitsliced cracker"），即 miLazyCracker 生态 | 源于 crypto1_bs（GPL 类，待最终确认） | 已定位到可重建源码分支：[GSWXXN/crypto1_bs `libnfc_collect`](https://github.com/GSWXXN/crypto1_bs/tree/libnfc_collect)（NFCToolsGUI 同款），可据此重新编译替换无源码二进制 |
| `mfdetect.exe` | **mfoc 修改版**（代码注释 Form1.cs:1168 原话："其实mfdetect就是个mfoc的修改版"），支持自定义 `-f -k` 参数 | BSD-3-Clause | 源码分支已定位：[GSWXXN/mfoc `nfc-mfdetect`](https://github.com/GSWXXN/mfoc/tree/nfc-mfdetect)，可重建 |
| `mff08.exe` | 作者基于 libnfc examples 自编译的定制工具（版本信息 libnfc.org 1.7.1），功能：修复写卡失败导致 0 块损坏的卡（`c`/`u` 命令写 block0，见 FormMFF08.cs） | 未公开源码 | **仍不明**：无公开对应项目，作者未提供源码。按 GPL-3.0 第 4 条，若作者曾分发本工具二进制，可向作者索取对应源码 |
| `libusb0.dll` | [libusb-win32](https://sourceforge.net/projects/libusb-win32/) | LGPL-2.1+ | |
| `data\`（mfcuk 模板指纹、proxmark3 日志） | 随 mfcuk / libnfc 数据分发 | 同上游 | |

**合规结论（nfc-bin）**：LGPL/BSD/GPL 二进制允许再分发（GPL/LGPL 附源码
获取指引即可；我们链接的都是子进程调用，非静态链接）。要求：

1. 分发时保留本文件 + 各上游项目声明
2. 提供对应开源项目源码获取地址（见上表链接）
3. `collect.exe` / `mfdetect.exe` 已定位到可重建的源码分支（见表），后续
   优先替换为自编译版本；`mff08.exe` 来源仍不明，查清前我们自己的
   Release **不重打包该文件**，只引导用户从原作者 Release 获取
   （`scripts/fetch-deps.ps1` 即为此设计，nfc-bin 不入 git）

## 二、winhex.exe（⚠️ 不分发）

- **X-Ways WinHex 17.2，Company=X-Ways Software Technology AG**
- 专有共享软件（非免费，个人/商用均需授权），原作者 Release 包里附带属灰色行为
- 主程序源码 **零引用** winhex（已全局搜索确认），仅为官方包的附赠品
- **策略：我们的构建/打包一律剔除 winhex.exe**（`build.ps1` 从 artifacts 回收
  时只取 `nfc-bin\` 子目录，不带根目录的 winhex.exe）。用户需要十六进制编辑
  用程序内置 HEX 编辑器（FormHTool）

## 三、Newtonsoft.Json.dll

- 仓库内直接附带二进制（13.0.x 时代产物），MIT 许可，允许再分发
- 现代化阶段应改为 NuGet `PackageReference`（build.ps1 已能处理现状）

## 四、CH341SER.ZIP / 驱动

- 历史 Release（v1.0.0）附带 CH340/CH341 串口驱动，南京沁恒专有
- 策略：驱动由用户从厂商官网获取，我们不重分发

## 五、发布检查清单

打包自己的 Release 前逐项确认：

- [x] 不含 `winhex.exe`（`build.ps1` 只取 `nfc-bin\` 子目录，已验证）
- [x] 不含 CH341 等厂商驱动（从未打包）
- [x] nfc-bin 若整体重打包：附上游源码链接声明（本文件随包附带）；
      `mff08.exe` 来源不明，查清/取得源码前不重打包该文件（`build.ps1` 自动剔除，
      已验证；GUI 缺失时提示并关闭 MFF08 对话框）
- [x] 保留 GPL-3.0 LICENSE + 本依赖文档（`build.ps1 -Pack` 自动附带，已验证）
- [x] 不含上游包里的 `*.bak` 历史残留（`libgcc_s_dw2-1.dll.bak` / `libnfc.dll.bak` /
      `libwinpthread-1.dll.bak` / `pcre3.dll.bak`，合计约 1.0 MB）：程序零引用、Windows 也不会加载，
      `build.ps1` 每次构建自动清理（保留运行期生成的 `libnfc.dll.acr122u.bak`，那是 ACR122U 切换的还原备份）
- [ ] `docs\` 与 README 版本号、AssemblyInfo 版本一致（待版本号决策后确认）
