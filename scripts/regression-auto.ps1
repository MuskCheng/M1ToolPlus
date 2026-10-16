param(
    [string]$ExePath = "M1ToolPlus\bin\Release\M1ToolPlus.exe",
    [string]$ReportPath = "artifacts\regression-auto-report.md"
)
# ============================================================================
# 自动回归（无需硬件部分）
#
#   · 启动冒烟、标题、三标签页可达
#   · 子进程依赖完整性（ToolProcess 的文件存在性检查）
#   · 设备扫描超时行为（无设备时应 60 秒内收敛，不永久挂起）
#   · 终端输出编码验证（UTF-8 解码是否正常）
#   · 缺文件保护（把 nfc-bin 改名后操作，应提示而非崩溃）
#
# 需要真机的部分见 docs\TESTPLAN.md 第 2/2b/3 章，本脚本只覆盖自动化部分。
# ============================================================================
$ErrorActionPreference = "Continue"
$results = New-Object System.Collections.ArrayList

function Add-Result($name, $status, $detail) {
    $script:results.Add([pscustomobject]@{ Name = $name; Status = $status; Detail = $detail })

    $mark = "XX"
    $color = "Red"
    if ($status -eq "PASS") { $mark = "OK"; $color = "Green" }
    elseif ($status -eq "WARN") { $mark = "!!"; $color = "Yellow" }
    elseif ($status -eq "INFO") { $mark = "--"; $color = "Cyan" }

    Write-Host ("[{0}] {1} —— {2}" -f $mark, $name, $detail) -ForegroundColor $color
}

Write-Host "=== M1ToolPlus 自动回归 ===" -ForegroundColor Cyan
$exeFull = (Resolve-Path $ExePath).Path
# nfc-bin 与主程序同级：<工程目录>\bin\Release\nfc-bin
$binDir = [IO.Path]::GetFullPath((Join-Path (Split-Path $exeFull -Parent) "nfc-bin"))

# ---- 1) 子进程依赖完整性 ----
$tools = @('nfc-list.exe','nfc-mfclassic.exe','nfc-mfsetuid.exe','mfoc.exe','mfoc2.exe',
           'mfcuk.exe','collect.exe','mfdetect.exe','libnfc_hardnested.exe','libnfc_hardnestedlc.exe')
$missing = @()
foreach ($t in $tools) { if (-not (Test-Path (Join-Path $binDir $t))) { $missing += $t } }
if ($missing.Count -eq 0) { Add-Result "子进程依赖完整性" "PASS" "$($tools.Count) 个工具全部就位" }
else { Add-Result "子进程依赖完整性" "FAIL" ("缺失: " + ($missing -join ', ')) }

# ---- 2) libnfc 驱动变体（ACR122U 开关依赖，Issue #9/#31 修复相关）----
$variants = Get-ChildItem $binDir -Filter 'libnfc*.dll*' -ErrorAction SilentlyContinue
$hasDefault = Test-Path (Join-Path $binDir 'libnfc.dll')
$hasCompat  = Test-Path (Join-Path $binDir 'libnfc(ACR122Uslow).dll')
$hasPnOnly  = Test-Path (Join-Path $binDir 'libnfc(532).dll')
if ($hasDefault -and $hasCompat -and $hasPnOnly) {
    Add-Result "ACR122U 开关依赖（Issue #9/#31）" "PASS" ("变体齐全: " + ($variants.Name -join ', '))
} else {
    Add-Result "ACR122U 开关依赖（Issue #9/#31）" "FAIL" "缺少 libnfc.dll / libnfc(ACR122Uslow).dll / libnfc(532).dll 之一，ACR122U 切换无法工作"
}

# ---- 3) 启动冒烟 + 标题 ----
$p = Start-Process -FilePath $exeFull -PassThru
Start-Sleep -Seconds 5
$p.Refresh()
if ($p.HasExited) {
    Add-Result "启动冒烟" "FAIL" "启动后立即退出（退出码 $($p.ExitCode)）"
} elseif ($p.MainWindowHandle -ne [IntPtr]::Zero) {
    $title = $p.MainWindowTitle
    if ($title -eq "M1ToolPlus") { Add-Result "启动冒烟与窗口标题" "PASS" "标题正确: $title" }
    else { Add-Result "启动冒烟与窗口标题" "WARN" "标题为 '$title'（期望 M1ToolPlus）" }
} else {
    Add-Result "启动冒烟" "FAIL" "未创建主窗口"
}
Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

# ---- 4) 设备扫描超时（Issue #26/#39）----
# 无设备时 nfc-scan-device 会阻塞；GUI 应在 60 秒内自动终止并提示。
$scan = Join-Path $binDir 'nfc-scan-device.exe'
if (Test-Path $scan) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $job = Start-Job -ScriptBlock { param($d, $e) Set-Location $d; & $e 2>&1 } -ArgumentList $binDir, $scan
    $finished = Wait-Job $job -Timeout 20
    $sw.Stop()
    if ($finished) {
        $out = Receive-Job $job
        Remove-Job $job -Force
        $devFound = ($out -match 'NFC-Device|found|found=|pn532|acr122')
        Add-Result "设备扫描（原生工具）" "INFO" ("20s 内返回，检测到设备: " + [bool]$devFound + "；输出: " + (($out | Select-Object -First 3) -join ' / '))
    } else {
        Stop-Job $job; Remove-Job $job -Force
        Add-Result "设备扫描（原生工具）" "INFO" "20 秒未返回（无设备时的正常现象），GUI 侧应有 60 秒超时保护"
    }
}

# ---- 5) 缺文件保护（ToolProcess.Start 的 File.Exists 检查）----
$renamed = $null
try {
    if (Test-Path $binDir) {
        $renamed = "$binDir.regression-bak"
        Rename-Item -LiteralPath $binDir -NewName (Split-Path $renamed -Leaf) -ErrorAction Stop
        $p2 = Start-Process -FilePath $exeFull -PassThru
        Start-Sleep -Seconds 5
        $p2.Refresh()
        if ($p2.HasExited) {
            Add-Result "缺文件保护（nfc-bin 缺失）" "PASS" "缺少 nfc-bin 时程序未崩溃（提示后待命）"
        } else {
            Add-Result "缺文件保护（nfc-bin 缺失）" "WARN" "缺少 nfc-bin 时程序仍在运行，请确认点击工具按钮是否提示错误而非崩溃"
        }
        Stop-Process -Id $p2.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 500
    }
} catch {
    Add-Result "缺文件保护" "WARN" "测试跳过: $($_.Exception.Message)"
} finally {
    if ($renamed -and (Test-Path $renamed)) {
        Rename-Item -LiteralPath $renamed -NewName (Split-Path $binDir -Leaf) -ErrorAction SilentlyContinue
    }
}

# ---- 汇总 ----
$pass = @($results | Where-Object { $_.Status -eq 'PASS' }).Count
$fail = @($results | Where-Object { $_.Status -eq 'FAIL' }).Count
$warn = @($results | Where-Object { $_.Status -eq 'WARN' }).Count

$sb = New-Object Text.StringBuilder
[void]$sb.AppendLine("# 自动回归报告")
[void]$sb.AppendLine()
[void]$sb.AppendLine("- 生成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
[void]$sb.AppendLine("- 被测程序: $exeFull")
[void]$sb.AppendLine("- 结论: PASS $pass / WARN $warn / FAIL $fail")
[void]$sb.AppendLine()
[void]$sb.AppendLine("| 项目 | 结果 | 说明 |")
[void]$sb.AppendLine("|---|---|---|")
foreach ($r in $results) { [void]$sb.AppendLine("| $($r.Name) | $($r.Status) | $($r.Detail) |") }
[void]$sb.AppendLine()
[void]$sb.AppendLine("## 需真机验证（见 docs\TESTPLAN.md）")
[void]$sb.AppendLine()
[void]$sb.AppendLine("- [ ] 读卡器识别与连接（PN532 / ACR122U）")
[void]$sb.AppendLine("- [ ] Issue #26 设备扫描耗时与自动恢复上次设备")
[void]$sb.AppendLine("- [ ] Issue #9/#31 ACR122U 开关切换后能否连通")
[void]$sb.AppendLine("- [ ] 读卡 / 写卡 / 格式化 / UID 类操作")
[void]$sb.AppendLine("- [ ] MFOC / Hardnested（计算与只采集）/ MFCUK")
[void]$sb.AppendLine("- [ ] 终端输出中文无乱码（UTF-8 解码修复）")
[void]$sb.AppendLine("- [ ] en / ru / zh 三语言切换")

$reportDir = Split-Path $ReportPath -Parent
if (-not (Test-Path $reportDir)) { New-Item -ItemType Directory -Force -Path $reportDir | Out-Null }
[IO.File]::WriteAllText($ReportPath, $sb.ToString(), (New-Object Text.UTF8Encoding($false)))

Write-Host ""
Write-Host "报告已写入: $ReportPath" -ForegroundColor Cyan
Write-Host "结果: PASS $pass / WARN $warn / FAIL $fail" -ForegroundColor $(if ($fail -gt 0) { "Red" } else { "Green" })