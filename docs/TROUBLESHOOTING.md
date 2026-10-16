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

设置页第一个下拉框选择：中文 / Русский / English，选择后自动重启生效。
本版本起内置英语界面。

## 构建相关

### 报 MSB3086 / 缺少 al.exe

不影响使用——用 `scripts\build.ps1` 构建，会自动用 csc 绕过生成
本地化程序集。详见 docs/DEVELOPMENT.md。
