# ============================================================================
#  写卡/扫描实时日志端到端验证（Issue：写卡过程无日志，结束才一次性出现）
#
#  原理：
#    nfc-mfclassic 的进度条逐字符 printf、不换行，只在扇区边界 fflush。
#    本脚本用伪造的 nfc-scan-device.exe 复现这一行为（每 140ms 一个字符、
#    每 4 个字符 flush、整条进度直到最后才换行），点击「检测连接」后
#    通过 WM_GETTEXT 采样终端最后一行，验证：
#      1) 进度行在运行过程中逐格变长（不是结束后才出现）；
#      2) 结束后只剩一行完整进度条（ReadOnly 下删除临时行的坑不复发）。
#
#  用法（在仓库根目录）：
#    powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-livelog.ps1
#
#  注意：脚本按简体中文界面查找「检测连接」按钮；运行前会结束现有
#        M1ToolPlus 进程（单实例限制）。
# ============================================================================
param(
  [string]$Src = (Join-Path $PSScriptRoot "..\M1ToolPlus\bin\Release"),
  [string]$Out = (Join-Path $env:TEMP "m1tool-livelog-test")
)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System; using System.Text; using System.Runtime.InteropServices;
public class LiveLogNative {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr h, EnumProc cb, IntPtr l);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr SendMessage(IntPtr h, uint m, IntPtr w, StringBuilder l);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left,Top,Right,Bottom; }
}
"@

function TextOf($h) { $t = New-Object System.Text.StringBuilder 512; [LiveLogNative]::GetWindowText($h,$t,512) | Out-Null; $t.ToString() }
function ClassOf($h) { $c = New-Object System.Text.StringBuilder 256; [LiveLogNative]::GetClassName($h,$c,256) | Out-Null; $c.ToString() }
function Get-Children($h) {
  $list = New-Object System.Collections.ArrayList
  $cb = [LiveLogNative+EnumProc]{ param($c,$l)
    [void]$list.Add([pscustomobject]@{H=$c; Text=(TextOf $c); Class=(ClassOf $c)})
    return $true }
  [LiveLogNative]::EnumChildWindows($h,$cb,[IntPtr]::Zero) | Out-Null
  return $list
}
function ShotWindow($h, $file) {
  [LiveLogNative]::SetWindowPos($h, [IntPtr](-1), 0,0,0,0, 0x0003) | Out-Null
  Start-Sleep -Milliseconds 250
  $r = New-Object LiveLogNative+RECT
  [LiveLogNative]::GetWindowRect($h,[ref]$r) | Out-Null
  $w = $r.Right-$r.Left; $ht = $r.Bottom-$r.Top
  $bmp = New-Object System.Drawing.Bitmap $w,$ht
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($r.Left,$r.Top,0,0,(New-Object System.Drawing.Size $w,$ht))
  $g.Dispose(); $bmp.Save($file,[System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
}
function Get-TerminalText($main) {
  try {
    $rtc = Get-Children $main | Where-Object { $_.Class -like "*RICHEDIT*" } | Select-Object -First 1
    if (-not $rtc) { return $null }
    $sb = New-Object System.Text.StringBuilder 65536
    [LiveLogNative]::SendMessage($rtc.H, 0x000D, [IntPtr]65536, $sb) | Out-Null
    return $sb.ToString()
  } catch { return $null }
}

# ---------- 1. 准备测试副本 ----------
Get-Process M1ToolPlus -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1
if (Test-Path $Out) { Remove-Item $Out -Recurse -Force }
New-Item -ItemType Directory -Path $Out | Out-Null
Copy-Item (Join-Path $Src "*") $Out -Recurse -Force
Remove-Item (Join-Path $Out "libnfc.conf") -ErrorAction SilentlyContinue
Remove-Item (Join-Path $Out "m1t.log") -ErrorAction SilentlyContinue
$binDir = Join-Path $Out "nfc-bin"
if (-not (Test-Path $binDir)) { New-Item -ItemType Directory -Path $binDir | Out-Null }

$fakeCs = Join-Path $Out "fake-scan.cs"
$fakeSource = @'
using System;
using System.Text;
using System.Threading;

// 模拟 nfc-mfclassic 的进度输出：逐字符、每 4 字符 flush、最后才换行。
class FakeScan
{
    static void Main()
    {
        try { Console.OutputEncoding = Encoding.GetEncoding(936); } catch (Exception) { }
        Console.WriteLine("\u4f7f\u7528libnfc 1.7.1");
        Console.Out.Flush();
        Console.WriteLine("pn532_uart:COM6:115200");
        Console.Out.Flush();

        Console.Write("|");
        Console.Out.Flush();
        string seq = "x..............................................................";
        for (int i = 0; i < seq.Length; i++)
        {
            Console.Write(seq[i]);
            if ((i % 4) == 3) { Console.Out.Flush(); }
            Thread.Sleep(140);
        }
        Console.Write("|\n");
        Console.Out.Flush();
        Console.WriteLine("\u5b8c\u6210\uff01");
        Console.Out.Flush();
    }
}
'@
[System.IO.File]::WriteAllText($fakeCs, $fakeSource, (New-Object System.Text.UTF8Encoding $false))

$csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
& $csc /nologo /target:exe /out:"$binDir\nfc-scan-device.exe" $fakeCs | Out-Null
if ($LASTEXITCODE -ne 0) { throw "csc failed" }

# ---------- 2. 启动程序并点击「检测连接」 ----------
$env:M1TOOL_THEME = "dark"
$p = Start-Process -FilePath (Join-Path $Out "M1ToolPlus.exe") -WorkingDirectory $Out -PassThru
$main = [IntPtr]::Zero
for ($k = 0; $k -lt 30 -and $main -eq [IntPtr]::Zero; $k++) {
  Start-Sleep -Milliseconds 500
  $p.Refresh()
  $main = $p.MainWindowHandle
}
if ($main -eq [IntPtr]::Zero) { throw "main window not found" }
Start-Sleep -Seconds 2

$btnText = [string]::Join("", [char[]](0x68C0, 0x6D4B, 0x8FDE, 0x63A5))   # 检测连接
$btn = $null
for ($k = 0; $k -lt 30 -and -not $btn; $k++) {
  $btn = Get-Children $main | Where-Object { $_.Class -like "*BUTTON*" -and $_.Text -eq $btnText } | Select-Object -First 1
  if (-not $btn) { Start-Sleep -Milliseconds 500 }
}
if (-not $btn) {
  Stop-Process -Id $p.Id -Force
  throw "button not found (需要简体中文界面)"
}
[LiveLogNative]::PostMessage($btn.H, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null

# ---------- 3. 采样终端最后一行长度 ----------
$samples = New-Object System.Collections.ArrayList
$shotAt = @(2, 5)
$shots = @()
$start = Get-Date
for ($i = 0; $i -lt 40; $i++) {
  Start-Sleep -Milliseconds 400
  $elapsed = ((Get-Date) - $start).TotalSeconds
  $txt = Get-TerminalText $main
  if ($txt -ne $null) {
    $lines = $txt -split "`n"
    $last = $lines[$lines.Length - 1]
    if ($last -eq "" -and $lines.Length -gt 1) { $last = $lines[$lines.Length - 2] }
    [void]$samples.Add([pscustomobject]@{ T=[math]::Round($elapsed,1); Len=$last.Length; Last=$last })
  }
  foreach ($s in $shotAt) {
    if (-not $shots.Contains($s) -and $elapsed -ge $s) {
      $shots += $s
      ShotWindow $main (Join-Path $Out ("livewrite_{0}s.png" -f $s))
    }
  }
  if ($p.HasExited) { break }
}
Start-Sleep -Seconds 2
ShotWindow $main (Join-Path $Out "livewrite_final.png")
$finalText = Get-TerminalText $main

"---- 采样（终端最后一行长度）----"
$samples | ForEach-Object { "{0,5}s  len={1,4}  {2}" -f $_.T, $_.Len, ($_.Last -replace "`r", "") }
"---- 最终终端文本 ----"
if ($finalText -ne $null) { $finalText } else { "<无法读取>" }
Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue

# ---------- 4. 判定 ----------
$growing = @($samples | Where-Object { $_.Len -gt 1 -and $_.Last -like "|*" } | ForEach-Object { $_.Len })
$grew = ($growing.Count -ge 3) -and (($growing[-1] - $growing[0]) -ge 30)
$progressLines = @()
if ($finalText -ne $null) {
  $progressLines = @($finalText -split "`n" | Where-Object { $_ -like "|x*" })
}
$singleLine = ($progressLines.Count -eq 1)

""
"RESULT: growing=$grew  singleLine=$singleLine  (progressSamples=$($growing.Count))"
if ($grew -and $singleLine) {
  "PASS"
  exit 0
} else {
  "FAIL"
  exit 1
}
