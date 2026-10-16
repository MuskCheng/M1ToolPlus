param([string]$ExePath = "M1ToolPlus\bin\Release\M1ToolPlus.exe", [string]$OutDir = "$env:TEMP", [int]$TabCount = 3)
# 逐个标签页截图（用于检查各页排版）
# 依赖程序支持的命令行参数 --tab N（见 Form1.ApplyInitialTab）
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win {
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
for ($i = 0; $i -lt $TabCount; $i++) {
    $p = Start-Process -FilePath (Resolve-Path $ExePath) -ArgumentList "--tab", "$i" -PassThru
    Start-Sleep -Seconds 5
    $p.Refresh()
    if ($p.MainWindowHandle -eq [IntPtr]::Zero) {
        Write-Host "第 $i 页：未找到主窗口"
        Stop-Process -Id $p.Id -Force
        continue
    }
    [Win]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 700

    $r = New-Object Win+RECT
    [Win]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
    $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [Win]::PrintWindow($p.MainWindowHandle, $hdc, 0) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()

    $f = Join-Path $OutDir ("tab_{0}.png" -f ($i + 1))
    $bmp.Save($f, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Stop-Process -Id $p.Id -Force
    Start-Sleep -Milliseconds 400
    Write-Host ("tab_{0}.png  ({1} x {2})" -f ($i + 1), $w, $h)
}