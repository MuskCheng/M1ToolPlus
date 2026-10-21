# 常见问题排障（FAQ）

整理自原仓库高频 Issue 的已验证解决方案。硬件相关问题的 workaround 均来自
社区实测反馈，标注了对应 Issue 号。仅用于你有授权的卡片。

## 设备连接类

### PN532 检测设备/扫描很慢（Win10 超 100 秒）#26

原因：勾选"NewScan 扫描选项"后每次检测都会删除 `libnfc.conf`，
触发 libnfc 全量串口轮询，Win10 下极慢。

解决方案（任选其一）：

1. **本仓库 v1.7.1+ 已内置修复**：启动时自动恢复上次设备，无需再全扫
2. 手动 workaround（对旧版有效）：在 M1ToolPlus.exe 同目录新建
   `libnfc.conf`，写入正确 COM 口后**直接点"扫描卡片"，不要点"检测设备"**：

   ```
   allow_autoscan = false
   allow_intrusive_scan = false
   device.name = "NFC-Device"
   device.connstring = "pn532_uart:COM4:115200"
   ```

   COM4 改为设备管理器里 PN532 串口号

### PN532 一直显示"运行中"，连接不上 #39

- 先确认驱动（CH340 串口驱动）与接线（波特率 115200）
- 按上一条手动写 `libnfc.conf` 指定 COM 口，跳过"检测设备"直接"扫描卡片"
- 检测卡在"运行中"通常是 libnfc 在轮询其他 COM 口，等它超时或
  用上面的方法固定端口

### ACR122U 无法识别 / 无法打开设备 #9 #31

按社区验证顺序排查：

1. 安装 **122-libusb 驱动**（libusb-win32 filter 驱动，v1.6.4+ 支持 ACR122U）
2. 若仍失败，改装**旧版官方驱动**再试
3. 部分**新固件 ACR122U 已封锁 raw 接口**，此类设备无法用于 M1 破解/写卡
4. 换回原版 `libnfc(ACR122U).dll`（当前发行包内改名交换方案见
   docs/DEVELOPMENT.md 设备切换一节）

## 读写卡类

### 写卡报 "Mifare Authentication Failed" / CUID 卡写不进 #35

CUID 卡不能像 UID 卡那样用后门随便写，必须用**卡内已有的密钥**写入：

1. 用 hardnested 解出目标卡数据（或你已有该卡 dump），拿到正确 key
2. "写M1"时在 key.mfd 中选择包含该密钥的数据文件再写
3. 或用 MFF08 的"CUID修复"功能：选择包含卡内密钥的数据文件修复 CUID 卡
4. 手环类虚拟卡写坏后，最简单是用官方 APP 删掉重建一张

### 清空 CUID 空白卡 #40

1. 用密钥读出此卡数据，存为 `.dump` 文件，改后缀为 `.mfd`
2. 进入"高级操作模式"，选择 key.mfd → 选择刚才的文件
3. 点"清M1"，完成后重启软件

### 还原卡后变空白 #42

原卡写回请确认：使用的是该卡自己的密钥（不是默认密钥），且写完后
用"手动扫描"验证 UID/SAK 未变化。仍异常请带 dump 文件开新 Issue。

## Hardnested 类

### hardnested 采集慢 / 只采集部分扇区 #37 #28

- hardnested 攻击本身需要大量 nonce 采集，耗时取决于卡片与读卡器，
  PN532 采集时避免移动卡片
- 采集完成会输出 txt/nonce 文件，可上传云计算节点计算（软件日志会提示）
- 对比 Proxmark3：`hf mf hardnested`（社区反馈 PM3 采集更快）

## 界面语言

### 切换语言 #41 #38

设置页第一个下拉框选择：简体中文 / Русский / English，选择后自动重启生效。
本版本起内置英语界面。

下拉框会用**当前界面语言**回显所选项（`0=简体中文`、`1=Русский`、`2=English`），
首次运行或配置里没有 `Language` 时按简体中文处理并选中第 0 项。
注意：从旧版本升级时，若配置里仍是空的 `Language`，旧版会把它显示成「标准」；
本版本起该选项统一叫「简体中文」（英语界面为 `Chinese`、俄语界面为 `Китайский`），
与 `zh-cn` 的语义一致，避免被误认为「跟随系统」。

**2026-10-20 修复：切换后界面仍是中文（1.7.0.0 及更早）**

- 现象：下拉框选 Русский / English，程序确实自动重启，但界面依旧全中文；
  此时 `%LOCALAPPDATA%\XciCode\M1ToolPlus.exe_Url_*\1.7.0.0\user.config` 里的
  `Language` 已正确写入 `ru-RU` / `en-US`——说明切换、保存、重启都没问题。
- 根因（两个都在**构建侧**，与切换逻辑无关）：
  1. `build.ps1` 手工生成的卫星程序集叫 `en\M1ToolPlus.en.dll`；
     CLR 只按 `<程序集名>.resources.dll` 探测，这个文件永远不会被加载。
  2. 改名成 `M1ToolPlus.resources.dll` 后**仍然无效**：本机
     （Windows 11 + .NET Framework 4.8.10008）的 `ResourceManager`
     只按**旧式资源名** `<基名>.<文化>.resources` 在卫星里查找；
     卫星里只嵌现代名 `M1ToolPlus.Form1.resources` 时同样找不到。
- 修复：卫星输出为 `<文化>\M1ToolPlus.resources.dll`，并且每个资源
  **同时**嵌入现代名与旧式名（见 `scripts/build.ps1` 第 3 步）。
- 自检：`M1ToolPlus.exe --ui-info` 会把 `SettingLanguage` /
  `CurrentUICulture` / `TabPage1Text` / `ButtonListDevText` / `LanguageCombo`
  写进 `%TEMP%\m1tool-ui-info.txt`；语言生效时前几行应与所选语言一致，
  `LanguageCombo` 应形如 `0:简体中文` / `1:Русский` / `2:English`。

**2026-10-21 修复：下拉框空白 / 不回显当前语言**

- 现象：设置页语言下拉框是空的，用户无法判断当前是哪种语言。
- 根因：下拉框只在 `SelectedIndexChanged` 里写配置并重启，**构造时从未设置
  `SelectedIndex`**，所以界面上始终没有选中项。
- 修复：构造期调用 `ApplyLanguageSelection()` 按
  `CultureInfo.TwoLetterISOLanguageName` 选中对应项，并用
  `suppressLanguageChanged` 屏蔽这次赋值触发的事件——否则会在启动时
  立刻 `Settings.Save()` + `Application.Restart()`，形成重启死循环
  （实测 `--ui-info` 每次启动进程数恒为 1，无循环）。

**2026-10-21 修复：俄语窗口比中文更大、卡片被拉宽**

- 现象：切到俄语后主窗口变成 1348×693（中文为 900×839），
  复制模式的三张卡片被拉到两倍宽，卡片之间的箭头错位。
- 根因：`Form1.ru.resx` 是原项目用俄语设计器保存的，里面残留了整套
  **旧版俄语布局**：`$this.ClientSize = 1098, 456` 加上 122 个 `Size`、
  36 个 `Location`。这些值会在 `ApplyResources` 时覆盖新布局。
- 修复：清空 `Form1.ru.resx` 里的全部几何条目，只保留文案，使其与
  `Form1.zh.resx` / `Form1.en.resx` 一致；布局统一由设计器基准值 +
  运行期 `UiTheme.ApplyAdaptiveLayout` / 流式布局计算。
- 附带修正：`build.ps1` 的新鲜度检查不再把语言专用 resx
  （`Form1.ru.resx` 等）算作主程序输入——它们只影响卫星程序集，
  以前只改俄语文案会误报「产物早于源文件」并中止构建。

## 构建相关

### 报 MSB3086 / 缺少 al.exe

不影响使用——用 `scripts\build.ps1` 构建，会自动用 csc 绕过生成
本地化程序集。详见 docs/DEVELOPMENT.md。
