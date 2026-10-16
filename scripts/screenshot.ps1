param([string]$ExePath = "M1ToolPlus\bin\Release\M1ToolPlus.exe", [string]$Out = "$env:TEMP\m1t_shot.png", [int]$SettleSeconds = 4)
# 启动程序 -> 等待主窗体 -> 用 PrintWindow 截图（32 位 DWM 兼容）
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win {
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
$p = Start-Process -FilePath (Resolve-Path $ExePath) -PassThru
Start-Sleep -Seconds $SettleSeconds
$p.Refresh()
if ($p.MainWindowHandle -eq [IntPtr]::Zero) { Write-Host "未找到主窗口"; Stop-Process -Id $p.Id -Force; exit 1 }
[Win]::ShowWindow($p.MainWindowHandle, 3) | Out-Null   # SW_MAXIMIZE 以看到完整布局
[Win]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
Start-Sleep -Seconds 2
$r = New-Object Win+RECT
[Win]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
$w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[Win]::PrintWindow($p.MainWindowHandle, $hdc, 0) | Out-Null
$g.ReleaseHdc($hdc); $g.Dispose()
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Stop-Process -Id $p.Id -Force
Write-Host "已截图: $Out ($w x $h)"