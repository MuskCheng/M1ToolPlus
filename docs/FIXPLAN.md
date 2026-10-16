# 问题驱动审计与修复计划

> 对照原作者仓库的 16 个 open Issue，对本项目代码做了一轮问题驱动审计（2026-10-16），
> 结论与分步修复方案合并在本文件。执行进度见第六节；每步完成后回填状态。

## 一、审计范围与方法

- **输入**：xcicode/MifareOneTool 的 16 个 open Issue（#9 #26 #28 #29 #30 #31 #32 #33 #34 #35 #37 #38 #39 #40 #41 #42），用 GitHub API 于 2026-10-16 抓取
- **方法**：对每个 Issue 的现象，在代码中定位对应处理路径，检查问题是否仍然存在、根因在哪
- **手段**：静态阅读 + 二进制符号检查（`findstr` 扫描 dll 内的 PCSC/pn53x/acr122 符号）+ 资源键覆盖比对 + 构建/冒烟验证；**无真机**，涉及硬件的结论标注为待验证

## 二、审计结论总表

| Issue | 现象 | 代码结论 | 状态 |
|---|---|---|---|
| #42 | win10 点「一键解原卡」蓝屏 | 路径已定位：`buttonEMfoc_Click` → `buttonMfoc_Click` → `mfoc` worker → `nfc-bin/mfoc.exe`，输出 `Mfoc.tmp`；蓝屏在子进程内，需崩溃转储 | 待真机 |
| #41 #38 | 要英语界面 | 8e4df10 已实现，en 文案覆盖 84/84 | ✅ |
| #40 | CUID 加密卡（已知密钥）如何清卡 | `清 M1 卡` 走 `nfc-mfclassic f A/B u <key.mfd>`（mf_format），对 CUID 无效（block0 受保护）；可答复路径：mff08 `c` 命令或 nfc-mfsetuid | 已定位，待答复 |
| #39 | PN532 检测一直「运行中」 | **真 bug（见 P2）**：`list_dev` 的 `WaitForExit()` 无超时，子进程阻塞时 UI 永远停在"运行中" | 待修 |
| #37 | hardnested 只采集不生成 .txt | 已定位：勾选「只采集」→ `CollectNonce` → `nfc-bin/collect.exe`，参数不含文件名，输出由 collect.exe 默认命名；`Form1.cs:1396` 的改名逻辑被注释 | 已定位 |
| #35 | 补写 dump 到小米手环 6 认证失败 | `buttonECmfoc_Click` 走 mfoc2 `/T:0A` + 已知 Key；SaferKey/随机密钥卡不适用 | 已定位，待答复 |
| #34 | 非 Classic 卡（SAK 00）读取报错 | 非目标支持场景 | 已定位 |
| #33 | 支持滚动码卡 | 不支持（滚动码防复制） | 已定位 |
| #32 | nfc-mfsetuid 是否开源 | 合规问题，已在 DEPENDENCIES.md 记录来源（libnfc examples + 作者定制） | ✅ |
| #31 | ACR122U 支持不完善、响应慢 | **真 bug（见 P1）** | 待修 |
| #30 #29 | Hardnested 云计算服务端 / GCP 部署脚本 | 代码内无云服务地址（仅文案引导）；原作者的第三方服务已失效，自建成本高 | 低优先 |
| #28 | hardnested 移动窗口报「无法创建堆栈防护页面」 | nfc-bin 无 stackguard，三个 hardnested exe 无该符号引用 → 离线无法定位，需真机 + 用户环境信息 | 待真机 |
| #26 | win10 扫描设备巨慢 | f5f5771 已修启动恢复设备；**仍有 P2 超时问题** | 部分修复 |
| #9 | 无法连接 ACR122U | **真 bug（见 P1）** | 待修 |

## 三、三个待修问题（审计确认）

### P1 · ACR122U 开关完全失效

- **代码期望的文件**（`Form1.cs:1159-1167`）：`libnfc(PN532Only).dll`、`libnfc(ACR122U).dll`
- **发布包实际文件**：`libnfc.dll`(269 KB, PCSC✓)、`libnfc(532).dll`(188 KB, 仅 pn53x)、`libnfc(ACR122Uslow).dll`(264 KB, PCSC✓)、`libnfc.dll.bak`(269 KB)
- **根因**：代码里两个 `File.Exists` 永远为 false，点「启用 ACR122U 支持」什么都不做，只打印"运行完毕"
- **影响**：#9 / #31 / #26 的 ACR122U 部分
- **修复方向**：代码改用包内实际文件名（备份 = `libnfc(532).dll`，启用目标 = `libnfc(ACR122Uslow).dll`），双向开关 + 明确日志
- **风险**：⚠️ 无法离线确认"慢速版"是否确实是作者意图的 ACR122U 目标，需真机确认 ACR122U 连接与速度

### P2 · 设备扫描无超时

- **代码**：`Form1.cs:170-205`，`Process.Start("nfc-bin/nfc-scan-device.exe")` + `process.WaitForExit()` 无超时
- **根因**：Win10 下 libnfc 设备扫描可阻塞 100+ 秒（与 #26 报告的"100 多s"一致），期间 UI 永久"运行中"（#39 现象）
- **修复方向**：`WaitForExit(60000)` + 超时 Kill + 提示；并用两步 `WaitForExit()` 保证异步输出刷完
- **风险**：低。注意不要给 hardnested/mfcuk 等长任务加超时（它们可能跑几十分钟）

### P3 · mfocCMD 经 cmd.exe 拼接命令

- **代码**：`Form1.cs:1345-1348`，`psi.FileName = "cmd.exe"`，`psi.Arguments = "/T:0A " + args[2] + " nfc-bin\\mfoc.exe " + args[1] + " -O \"" + args[0] + "\""`
- **根因**：为显示独立控制台而绕道 cmd.exe，密钥字典路径以字符串拼接，存在引号注入面，且与 mfoc worker（直调 exe）实现重复
- **修复方向**：直调 `nfc-bin/mfoc.exe`；但 `UseShellExecute` 默认 true 会导致无窗口 → 需要设 `false` 并显式接 OutputDataReceived，或保留 cmd.exe 仅作显示
- **风险**：中。需确认控制台窗口行为与 #35 复现条件

## 四、分步执行计划

| 步 | 内容 | 变更点 | 验证方式 | 风险 |
|---|---|---|---|---|
| 1 | P1 ACR122U 文件名修正 | `Form1.cs:1153-1173` | 构建 + 冒烟；**需真机** ACR122U 连接 | 低（逻辑简单） |
| 2 | P2 设备扫描超时 | `Form1.cs:170-205` | 构建 + 冒烟；**需真机** 无设备时的响应 | 低 |
| 3 | P3 mfocCMD 直调 exe | `Form1.cs:1342-1364` | 构建 + 冒烟 | 中 |
| 4 | 框架升级 v4.0 → v4.8 | `M1ToolPlus.csproj:13-14`（含 `<TargetFrameworkProfile>Client</TargetFrameworkProfile>` 需改） | 构建 + 冒烟；需确认本机有 4.8 参考程序集 | 中（影响全局，需确认 build.ps1 兼容） |
| 5 | 硬编码中文 + 英文 typo | `Form1.cs:704,752,756`、`Form1.en.resx:1246`（Cropto→Crypto） | 构建 + 冒烟 | 低 |
| 6 | Process.Start 统一保护 | 22 处调用点 | 构建 + 冒烟 | 中 |

每步一个提交，推送后回填本文件第六节。

## 五、需要真机验证的项（无法在本环境完成）

- [ ] ACR122U 连接是否恢复、启用慢速版后速度变化（P1）
- [ ] 无读卡器时扫描超时的表现与提示（P2）
- [ ] 切换 ACR122U 支持后的开关双向行为（P1）
- [ ] 终端输出乱码（#28/#34/#35 日志）修复后的效果（依赖框架升级到 4.8 后用 `StandardOutputEncoding = UTF8`，需 `psi.StandardOutputEncoding` 属性，该属性 .NET 4.5+ 才有）

## 六、执行进度

| 步 | 状态 | 提交 |
|---|---|---|
| 1 P1 ACR122U | ✅ 已完成（待真机） | 与步 2 同一提交 |
| 2 P2 扫描超时 | ✅ 已完成（待真机） | 与步 2 同一提交 |
| 3 P3 mfocCMD | ⬜ 未开始 | — |
| 4 框架 v4.8 | ⬜ 未开始 | — |
| 5 硬编码+typo | ⬜ 未开始 | — |
| 6 StartTool 保护 | ⬜ 未开始 | — |

### 步 1 实施要点（已完成）

- `Form1.cs:1153-1194` 重写 `buttonEnAcr122u_Click`：按包内实际文件名（`libnfc(ACR122Uslow).dll` / `libnfc(532).dll`）做**可逆**切换，切换前把当前 `libnfc.dll` 备份为 `libnfc.dll.acr122u.bak`
- 新增 `SameFileContent()` 辅助方法（先比长度再逐字节比较），用于判断当前生效的驱动变体，避免同尺寸误判
- 整段包在 `try/catch` 中，异常写日志不再崩溃
- 新增 3 条本地化文案（`ACR122U兼容模式_已切换` / `ACR122U兼容模式_已切回` / `默认驱动已含ACR122U支持`），zh/en/ru 四语言齐全
- 提示文案沿用原有「打开后可能变慢」提示，不改变交互习惯

### 步 2 实施要点（已完成）

- `Form1.cs` `list_dev`：`process.WaitForExit()` → `WaitForExit(60000)`，超时则 `Kill()` + 提示「程序已被强制停止」并正常收尾（复位 `lprocess`/`running`）
- 采用两步等待（先带超时、再无参）确保异步输出事件处理完毕，避免丢失最后的设备列表行
- 只给**设备扫描**加超时；hardnested / mfcuk 等长任务未受影响