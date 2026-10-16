# fetch-deps.ps1 - 回收运行期二进制依赖 nfc-bin
# 来源优先级:
#   1. 本地 artifacts\M1T-Release.zip（已下载过则离线解压）
#   2. 本地 artifacts\M1T-Release\ 目录
#   3. 从 GitHub Releases 下载 v1.7.0 官方包
# 用法: .\scripts\fetch-deps.ps1 [-TargetDir <输出目录>]

param(
    [string]$TargetDir = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $TargetDir) { $TargetDir = Join-Path $repoRoot "M1ToolPlus\bin\Release" }

$zipPath   = Join-Path $repoRoot "artifacts\M1T-Release.zip"
$relDir    = Join-Path $repoRoot "artifacts\M1T-Release"
$remoteUrl = "https://github.com/xcicode/MifareOneTool/releases/download/v1.7.0/M1T-Release.zip"

$nfcBinDst = Join-Path $TargetDir "nfc-bin"
if (Test-Path $nfcBinDst) {
    Write-Host "[deps] nfc-bin 已存在: $nfcBinDst" -ForegroundColor Green
    exit 0
}

# 1/2) 本地已有
$srcNfcBin = $null
if (Test-Path (Join-Path $relDir "nfc-bin")) {
    $srcNfcBin = Join-Path $relDir "nfc-bin"
} elseif (-not (Test-Path $zipPath)) {
    # 3) 下载
    Write-Host "[deps] 本地无 release 包，下载 $remoteUrl ..."
    $ProgressPreference = "SilentlyContinue"
    New-Item -ItemType Directory -Force -Path (Split-Path $zipPath) | Out-Null
    Invoke-WebRequest -Uri $remoteUrl -OutFile $zipPath
}
if (-not $srcNfcBin) {
    Write-Host "[deps] 解压 $zipPath ..."
    $tmp = Join-Path $env:TEMP ("m1t_deps_" + [guid]::NewGuid().ToString("N"))
    Expand-Archive -Path $zipPath -DestinationPath $tmp -Force
    $srcNfcBin = Join-Path $tmp "nfc-bin"
    if (-not (Test-Path $srcNfcBin)) { throw "[deps] 压缩包内未找到 nfc-bin" }
    # 同时落一份到 artifacts 供离线复用
    New-Item -ItemType Directory -Force -Path $relDir | Out-Null
    Copy-Item $srcNfcBin (Join-Path $relDir "nfc-bin") -Recurse -Force
    Copy-Item (Join-Path $tmp "*") $relDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $tmp -Recurse -Force
    $srcNfcBin = Join-Path $relDir "nfc-bin"
}

New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null
Copy-Item $srcNfcBin $nfcBinDst -Recurse -Force

# 健康检查: build.ps1/代码依赖的关键工具
$required = @("nfc-list.exe", "nfc-scan-device.exe", "nfc-mfclassic.exe", "nfc-mfsetuid.exe",
              "mfoc.exe", "mfcuk.exe", "libnfc.dll", "libnfc_hardnested.exe", "mfdetect.exe")
$missing = $required | Where-Object { -not (Test-Path (Join-Path $nfcBinDst $_)) }
if ($missing) { Write-Warning "[deps] 缺少关键工具: $($missing -join ', ')" }
else { Write-Host "[deps] nfc-bin 就绪（$($required.Count) 个关键工具齐全）: $nfcBinDst" -ForegroundColor Green }

# winhex.exe 合规提示（专有软件，不随我们的构建分发，见 docs/DEPENDENCIES.md）
if (Test-Path (Join-Path $nfcBinDst "..\winhex.exe")) { Write-Host "[deps] 提示: release 包内含 winhex.exe（专有），打包分发时将剔除" -ForegroundColor Yellow }
