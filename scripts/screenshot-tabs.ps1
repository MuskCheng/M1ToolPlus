param([string]$ExePath = "M1ToolPlus\bin\Release\M1ToolPlus.exe", [string]$OutDir = "$env:TEMP", [int]$TabCount = 3, [int]$MaxWidth = 0)
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
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
# 截图脚本自身也要声明 DPI 感知：M1ToolPlus 现在是 DPI 感知进程，
# 若脚本不感知，GetWindowRect 返回的是被系统换算过的虚拟坐标，
# 抓图尺寸/裁剪范围都会对不上（120/144 DPI 下尤其明显）。
try { [Win]::SetProcessDPIAware() | Out-Null } catch { }
# 输出目录必须存在，否则 GDI+ Save 抛"A generic error occurred in GDI+."
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }
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
    # 留足重绘时间：主题在 Load 阶段才应用，抓早了会拍到旧帧/半帧（文字重影）
    Start-Sleep -Milliseconds 1800

    if ($MaxWidth -gt 0) {
        # 放大到指定宽度，便于检查宽屏下的排版
        $rr0 = New-Object Win+RECT
        [Win]::GetWindowRect($p.MainWindowHandle, [ref]$rr0) | Out-Null
        Add-Type @"
using System; using System.Runtime.InteropServices;
public class Resize { [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f); }
"@ -ErrorAction SilentlyContinue
        [Resize]::SetWindowPos($p.MainWindowHandle, [IntPtr]::Zero, 20, 20, $MaxWidth, 900, 0x004) | Out-Null
        Start-Sleep -Milliseconds 800
    }

    $r = New-Object Win+RECT
    [Win]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
    $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
    # 直接抓屏幕：PrintWindow 对 DWM 合成窗口经常返回上一帧或把其他窗口叠进来，
    # 截图会“看起来正常但内容是旧的”。窗口已置前，用 CopyFromScreen 才是所见即所得。
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.Left, $r.Top, 0, 0, (New-Object System.Drawing.Size $w, $h))
    $g.Dispose()

    $f = Join-Path $OutDir ("tab_{0}.png" -f ($i + 1))
    $bmp.Save($f, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Stop-Process -Id $p.Id -Force
    Start-Sleep -Milliseconds 400
    Write-Host ("tab_{0}.png  ({1} x {2})" -f ($i + 1), $w, $h)
}