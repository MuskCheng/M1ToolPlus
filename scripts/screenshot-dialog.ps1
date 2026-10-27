param(
    [string]$ExePath = "M1ToolPlus\bin\Release\M1ToolPlus.exe",
    [string]$AppArgs = "",
    [string]$Title = "",
    [string]$Out = "$env:TEMP\m1tool-dialog.png",
    [int]$WaitSeconds = 16
)
# Screenshot a dialog window (e.g. the update dialog opened by --update-dialog).
#
# Why the C# helper: PowerShell 5.1 keeps failing to resolve System.Drawing /
# System.Windows.Forms types in this script context, so all window enumeration,
# rect query and capture are done in one compiled helper. Note that
# FindWindow(null, title) does NOT reliably find these WinForms dialogs --
# enumerate the process's own top-level windows instead.
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class DlgSnap {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern int GetWindowThreadProcessId(IntPtr h, out int pid);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowTextW(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }

  static List<IntPtr> WindowsOf(int pid) {
    var list = new List<IntPtr>();
    EnumWindows(delegate(IntPtr h, IntPtr l) {
      int p; GetWindowThreadProcessId(h, out p);
      if (p == pid) { list.Add(h); }
      return true;
    }, IntPtr.Zero);
    return list;
  }

  static string TitleOf(IntPtr h) {
    var sb = new StringBuilder(512);
    GetWindowTextW(h, sb, 512);
    return sb.ToString();
  }

  /// Polls the process for a window whose title equals <title>; returns its handle or IntPtr.Zero.
  public static IntPtr WaitFor(int pid, string title, int timeoutMs, out string found) {
    found = null;
    var deadline = Environment.TickCount + timeoutMs;
    while (Environment.TickCount < deadline) {
      foreach (IntPtr h in WindowsOf(pid)) {
        string t = TitleOf(h);
        if (t != null && t.Length > 0 && (title == null || title.Length == 0 || t == title)) {
          found = t;
          return h;
        }
      }
      System.Threading.Thread.Sleep(120);
    }
    return IntPtr.Zero;
  }

  /// Captures the window rect into <outPath>; returns "WxH" or an error string.
  public static string Shot(IntPtr h, string outPath) {
    SetProcessDPIAware();
    RECT r;
    if (!GetWindowRect(h, out r)) { return "GetWindowRect failed"; }
    int w = r.Right - r.Left, ht = r.Bottom - r.Top;
    if (w <= 0 || ht <= 0) { return "empty rect"; }
    using (var bmp = new Bitmap(w, ht))
    using (var g = Graphics.FromImage(bmp)) {
      g.CopyFromScreen(r.Left, r.Top, 0, 0, new Size(w, ht));
      bmp.Save(outPath, ImageFormat.Png);
    }
    return w + "x" + ht;
  }
}
"@

$argList = @()
if ($AppArgs) { $argList = $AppArgs -split " " }
$p = Start-Process -FilePath (Resolve-Path $ExePath) -ArgumentList $argList -PassThru

$found = $null
$hwnd = [DlgSnap]::WaitFor($p.Id, $Title, $WaitSeconds * 1000, [ref]$found)
if ($hwnd -eq [IntPtr]::Zero) {
    Write-Host ("dialog not found within {0}s: {1}" -f $WaitSeconds, $Title)
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
    exit 1
}
Write-Host ("dialog: " + $found)
# 刚创建出来的窗口可能还没画完，抓早了是一张空白块；置前 + 等 250ms 再抓，
# 「已是最新」态 1.6 秒才自动关，这点等待是安全的。
[DlgSnap]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 250
$dir = Split-Path -Parent $Out
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
Write-Host ("saved: " + $Out + " (" + [DlgSnap]::Shot($hwnd, $Out) + ")")
Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
