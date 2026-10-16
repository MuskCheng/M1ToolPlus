# gen-en-resx.ps1 - 从英文 fork (xavave/MifareOneTool-English) 提取英文文案,
# 生成 .en.resx 卫星资源（一次性迁移工具，保留以便后续翻译同步）
# 键基准 = 本仓库中性 resx；英文值 = fork 中性 resx（其默认即英文）
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$projDir  = Join-Path $repoRoot "M1ToolPlus"
$srcDir   = Join-Path $env:TEMP "m1t_en"   # 由调用者预先下载

if (-not (Test-Path $srcDir)) { throw "缺少英文资源源目录: $srcDir" }

# (我方resx路径, fork文件名, 输出en文件路径)
$targets = @(
    @{ mine = "Form1.resx";           out = "Form1.en.resx" },
    @{ mine = "FormDiff.resx";        out = "FormDiff.en.resx" },
    @{ mine = "FormHardNes.resx";     out = "FormHardNes.en.resx" },
    @{ mine = "FormHTool.resx";       out = "FormHTool.en.resx" },
    @{ mine = "FormMFF08.resx";       out = "FormMFF08.en.resx" },
    @{ mine = "Properties\Resources.resx"; out = "Properties\Resources.en.resx" }
)

$allStats = @()
foreach ($t in $targets) {
    $minePath = Join-Path $projDir $t.mine
    $forkPath = Join-Path $srcDir (Split-Path $t.mine -Leaf)
    if (-not (Test-Path $forkPath)) { Write-Warning "跳过(无fork文件): $($t.mine)"; continue }

    $mine = [xml](Get-Content $minePath -Raw -Encoding UTF8)
    $fork = [xml](Get-Content $forkPath -Raw -Encoding UTF8)

    # fork 的字符串条目 => name->value
    $enMap = @{}
    foreach ($d in $fork.root.data) {
        if (-not $d.type -and -not $d.mimetype -and $d.value) { $enMap[$d.name] = $d.value }
    }

    # 以我方中性 resx 为骨架，仅保留字符串条目并替换为英文值
    $out = New-Object System.Xml.XmlDocument
    $out.LoadXml($mine.OuterXml)
    $root = $out.root
    $added = 0; $fallback = 0
    $nodes = @($root.SelectNodes("data"))
    foreach ($n in $nodes) {
        $hasType = $n.HasAttribute("type") -or $n.HasAttribute("mimetype")
        if ($hasType) { $root.RemoveChild($n) | Out-Null; continue }   # 布局/二进制条目不进卫星
        if ($enMap.ContainsKey($n.name)) {
            $n.value = $enMap[$n.name]; $added++
        } else {
            $fallback++   # 无英文则保留原值(中文)作为回退
        }
    }
    $outPath = Join-Path $projDir $t.out
    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)
    $w = [System.Xml.XmlWriter]::Create($outPath, $settings)
    $out.Save($w); $w.Close()
    $allStats += [pscustomobject]@{ File = $t.out; English = $added; Fallback = $fallback }
}

$allStats | Format-Table -AutoSize | Out-String -Width 120
Write-Host "en resx 生成完毕 -> $projDir" -ForegroundColor Green
