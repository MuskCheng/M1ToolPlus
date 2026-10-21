# build.ps1 - M1ToolPlus 构建脚本
# 用法:
#   .\scripts\build.ps1                 # Release 构建（默认）
#   .\scripts\build.ps1 -Configuration Debug
#   .\scripts\build.ps1 -Pack           # 构建并打包到 artifacts\package\
#
# 说明: 完整 MSBuild 构建需要 Windows SDK 的 al.exe 生成本地化卫星程序集(ru/zh)。
#       本脚本自动探测: 有 al.exe -> 走标准 msbuild; 没有 -> 走 csc 手工生成卫星程序集。

param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [switch]$Pack,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$repoRoot   = Split-Path -Parent $PSScriptRoot
$projDir    = Join-Path $repoRoot "M1ToolPlus"
$csproj     = Join-Path $projDir "M1ToolPlus.csproj"
$outDir     = Join-Path $projDir "bin\$Configuration"
$objDir     = Join-Path $projDir "obj\x86\$Configuration"
$platform   = "x86"

function Find-MSBuild {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $msb = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild `
               -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
        if ($msb -and (Test-Path $msb)) { return $msb }
    }
    $candidates = @(
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
    )
    foreach ($c in $candidates) { if (Test-Path $c) { return $c } }
    throw "未找到 MSBuild，请安装 VS Build Tools 或 .NET Framework"
}

function Find-AlExe {
    $roots = @(
        "${env:ProgramFiles(x86)}\Microsoft SDKs",
        "${env:ProgramFiles}\Microsoft SDKs",
        "${env:ProgramFiles(x86)}\Windows Kits",
        "${env:ProgramFiles}\Windows Kits"
    )
    foreach ($r in $roots) {
        if (Test-Path $r) {
            $al = Get-ChildItem $r -Filter "al.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($al) { return $al.FullName }
        }
    }
    return $null
}

$msbuild = Find-MSBuild
Write-Host "[build] MSBuild: $msbuild" -ForegroundColor Cyan

if ($Clean) {
    Write-Host "[build] Clean..." -ForegroundColor Cyan
    & $msbuild $csproj /t:Clean /p:Configuration=$Configuration /p:Platform=$platform /v:minimal /nologo | Out-Null
    Remove-Item $outDir -Recurse -Force -ErrorAction SilentlyContinue
}

$al = Find-AlExe
$fullBuildOk = $false

if ($al) {
    Write-Host "[build] 找到 al.exe ($al)，执行标准 MSBuild 构建" -ForegroundColor Green
    & $msbuild $repoRoot\M1ToolPlus.sln /t:Rebuild `
        /p:Configuration=$Configuration /p:Platform=$platform `
        /v:minimal /nologo
    if ($LASTEXITCODE -eq 0) { $fullBuildOk = $true }
    else { Write-Warning "标准构建失败，回退到 csc 模式" }
} else {
    Write-Host "[build] 未找到 al.exe（Windows SDK），使用 csc 回退模式" -ForegroundColor Yellow
}

if (-not $fullBuildOk) {
    # 1) 完整构建链（ResolveReferences + GenerateResource + CoreCompile）。
    #    注意: 必须用 /t:Build 而非 /t:CoreCompile —— 后者会跳过引用解析导致 CS0518/CS0246。
    #    预期在 GenerateSatelliteAssemblies 步骤因缺 al.exe 报 MSB3086，主 exe 此时已产出，视为可继续。
    $buildLog = & $msbuild $csproj /t:Build `
        /p:Configuration=$Configuration /p:Platform=$platform `
        /v:minimal /nologo 2>&1
    $mainExe = Join-Path $objDir "M1ToolPlus.exe"
    # 编译错误必须显式失败：MSBuild 出错时 obj 里可能残留上一次的 exe，
    # 若只判断「文件存在」，脚本会拿旧程序冒充本次构建结果（曾因此误判多轮）。
    $compileErrors = @($buildLog | Where-Object { $_ -match "error\s+(CS|MSB)\d+" -and $_ -notmatch "MSB3086" })
    if ($compileErrors.Count -gt 0) {
        $compileErrors | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        throw "[build] 编译失败（见上方错误）"
    }
    if (-not (Test-Path $mainExe)) {
        $buildLog | ForEach-Object { Write-Host $_ }
        throw "[build] 编译失败：未产出主程序（非 al.exe 问题，见上方 MSBuild 输出）"
    }
    $buildLog | Where-Object { $_ -match "error" -and $_ -notmatch "al\.exe|MSB3086" } | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    Write-Host "[build] 主程序编译成功（卫星程序集步骤因 al.exe 缺失跳过，下面手工补齐）" -ForegroundColor Yellow

    # 2) 布局输出目录
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    Copy-Item (Join-Path $objDir "M1ToolPlus.exe") $outDir -Force
    Copy-Item (Join-Path $projDir "Newtonsoft.Json.dll") $outDir -Force
    Copy-Item (Join-Path $projDir "app.config") (Join-Path $outDir "M1ToolPlus.exe.config") -Force

    # 3) 用 csc 生成卫星程序集 ru/zh（绕过缺失的 al.exe）
    $csc = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    if (-not (Test-Path $csc)) { throw "[build] 未找到 csc.exe" }

    $asmInfo = Get-Content (Join-Path $projDir "Properties\AssemblyInfo.cs") -Raw
    $ver = if ($asmInfo -match 'AssemblyFileVersion\("([\d\.]+)"\)') { $Matches[1] } else { "1.7.0.0" }

    foreach ($cult in @("en", "ru", "zh")) {
        $resFiles = Get-ChildItem $objDir -Filter "*.$cult.resources" -File -ErrorAction SilentlyContinue
        if (-not $resFiles) { Write-Warning "[$cult] 无卫星资源，跳过"; continue }

        $satSrc = Join-Path $objDir "sat_$cult.cs"
        # 卫星程序集必须命名为 <主程序集名>.resources.dll（程序集名 M1ToolPlus.resources），
        # CLR 的 ResourceManager 只按这个约定去 <culture>\ 目录探测；叫 M1ToolPlus.en.dll
        # 之类的名字不会被加载，结果是切换语言后界面永远回退到中性资源（简体中文）。
        $satDll = Join-Path $objDir "M1ToolPlus.resources.dll"
        @"
[assembly: System.Reflection.AssemblyCulture("$cult")]
[assembly: System.Reflection.AssemblyVersion("$ver")]
"@ | Set-Content $satSrc -Encoding UTF8

        $cscArgs = @("/nologo", "/target:library", "/out:`"$satDll`"")
        foreach ($r in $resFiles) {
            # M1ToolPlus.Form1.ru.resources -> 逻辑名 M1ToolPlus.Form1.resources
            $logical = $r.Name -replace "\.$cult\.resources$", ".resources"
            $cscArgs += "/resource:`"$($r.FullName)`",$logical"
            # 同时嵌入旧式名 <逻辑名>.<culture>.resources：
            # 实测 Windows 11 + .NET Framework 4.8.10008 的 ResourceManager 只认这种命名，
            # 只放现代名时卫星永远不会被采用（界面永远回退到中性资源=简体中文）。
            # 两种命名并存对标准运行时无副作用（它优先用现代名）。
            $cscArgs += "/resource:`"$($r.FullName)`",$($r.Name)"
        }
        $cscArgs += "`"$satSrc`""
        & $csc @cscArgs
        if ($LASTEXITCODE -ne 0) { throw "[$cult] 卫星程序集编译失败" }

        $cultDir = Join-Path $outDir $cult
        New-Item -ItemType Directory -Force -Path $cultDir | Out-Null
        Copy-Item $satDll (Join-Path $cultDir "M1ToolPlus.resources.dll") -Force
        # 清掉历史错误命名的残留，避免新旧文件共存造成误判
        $legacyDll = Join-Path $cultDir "M1ToolPlus.$cult.dll"
        if (Test-Path $legacyDll) { Remove-Item $legacyDll -Force }
        Write-Host "[build] 卫星程序集已生成: $cult\M1ToolPlus.resources.dll" -ForegroundColor Green
    }
}

# 4) 运行依赖: nfc-bin（从 artifacts 回收，若存在）
$nfcBinSrc = Join-Path $repoRoot "artifacts\M1T-Release\nfc-bin"
if ((Test-Path $nfcBinSrc) -and -not (Test-Path (Join-Path $outDir "nfc-bin"))) {
    Copy-Item $nfcBinSrc (Join-Path $outDir "nfc-bin") -Recurse -Force
    Write-Host "[build] 已复制 nfc-bin 依赖到输出目录" -ForegroundColor Green
} elseif (-not (Test-Path (Join-Path $outDir "nfc-bin"))) {
    Write-Warning "[build] 未找到 nfc-bin（运行需要），请先运行 .\scripts\fetch-deps.ps1"
}

# 合规：mff08.exe 来源不明（作者自编译、源码未公开，见 docs/DEPENDENCIES.md 第一节/第五节），
# 按既定策略我们的构建产物一律剔除，用户需该功能时请使用原作者 Release 包。
$outMff08 = Join-Path $outDir "nfc-bin\mff08.exe"
if (Test-Path $outMff08) {
    Remove-Item $outMff08 -Force
    Write-Host "[build] 合规剔除 nfc-bin\mff08.exe（来源不明，见 docs/DEPENDENCIES.md）" -ForegroundColor Yellow
}

# 清理上游包里的 *.bak 历史残留（libgcc_s_dw2-1 / libnfc / libwinpthread-1 / pcre3，
# 合计约 1 MB）：程序零引用，Windows 也不会加载 .bak，纯属官方包遗留。
# 注意排除 libnfc.dll.acr122u.bak —— 那是「启用 ACR122U 支持」运行期自己生成的还原备份，不能删。
$outNfcBin = Join-Path $outDir "nfc-bin"
if (Test-Path $outNfcBin) {
    $staleBaks = @(Get-ChildItem $outNfcBin -File -Filter "*.bak" |
                   Where-Object { $_.Name -ne "libnfc.dll.acr122u.bak" })
    if ($staleBaks.Count -gt 0) {
        $staleKb = ($staleBaks | Measure-Object Length -Sum).Sum / 1KB
        $staleBaks | Remove-Item -Force
        Write-Host ("[build] 清理 nfc-bin 内上游 .bak 残留 {0} 个（{1:N0} KB）" -f $staleBaks.Count, $staleKb) -ForegroundColor DarkGray
    }
}

# 5) 汇总
$exe = Join-Path $outDir "M1ToolPlus.exe"
if (Test-Path $exe) {
    $fi = Get-Item $exe
    # 产物必须比所有「手写」源文件新：增量构建偶尔会因为时间戳/并行写入而跳过重编，
    # 结果是拿旧 exe 继续测试（曾连续多轮验证的是旧程序）。这里做硬校验。
    # 排除构建过程自己生成的 sat_*.cs（卫星程序集源文件，生成时间必然晚于 exe）
    # 以及 obj/bin 中间目录。
    # 再排除语言专用 resx（Form1.ru.resx 等）：它们只进卫星程序集，不参与主程序编译，
    # 每次构建都会由下面的 csc 步骤重新生成卫星，不需要因此重编 exe。
    $newest = Get-ChildItem $projDir -Include *.cs, *.resx, *.csproj -Recurse -File |
              Where-Object { $_.Name -notlike 'sat_*.cs' -and $_.FullName -notmatch '\\(obj|bin)\\' -and $_.Name -notmatch '\.[a-z]{2}(-[A-Za-z]{2,4})?\.resx$' } |
              Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($newest -and $fi.LastWriteTime -lt $newest.LastWriteTime) {
        throw "[build] 产物($($fi.LastWriteTime.ToString('HH:mm:ss'))) 早于源文件 $($newest.Name)($($newest.LastWriteTime.ToString('HH:mm:ss')))，本次未真正重编"
    }
    Write-Host "[build] 成功: $($fi.FullName) ($([math]::Round($fi.Length/1KB,1)) KB) v$ver" -ForegroundColor Green
} else {
    throw "[build] 未产出 M1ToolPlus.exe"
}

if ($Pack) {
    $pkgDir = Join-Path $repoRoot "artifacts\package"
    if (Test-Path $pkgDir) { Remove-Item $pkgDir -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $pkgDir | Out-Null
    Copy-Item (Join-Path $outDir "*") $pkgDir -Recurse -Force
    # winhex 等专有软件不打包（合规）：nfc-bin 取自官方包，只带 nfc-bin 子目录，不含根目录的 winhex.exe
    # 合规：随包附带 GPL-3.0 许可证与依赖来源说明（docs/DEPENDENCIES.md 第五节要求）
    Copy-Item (Join-Path $repoRoot "LICENSE") $pkgDir -Force
    Copy-Item (Join-Path $repoRoot "README.md") $pkgDir -Force
    Copy-Item (Join-Path $repoRoot "docs\DEPENDENCIES.md") (Join-Path $pkgDir "DEPENDENCIES.md") -Force
    $zip = Join-Path $repoRoot "artifacts\M1ToolPlus-local-build.zip"
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path (Join-Path $pkgDir "*") -DestinationPath $zip
    Write-Host "[build] 打包完成: $zip" -ForegroundColor Green
}
