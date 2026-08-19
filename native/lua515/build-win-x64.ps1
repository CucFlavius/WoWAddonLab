$ErrorActionPreference = "Stop"

$workRoot = Join-Path ([IO.Path]::GetTempPath()) ("wow-addon-lab-lua-" + [guid]::NewGuid())
$archive = Join-Path $workRoot "lua-5.1.5.tar.gz"
$sourceRoot = Join-Path $workRoot "lua-5.1.5\src"
$outputRoot = Join-Path $workRoot "output"

New-Item -ItemType Directory -Path $workRoot, $outputRoot | Out-Null
Invoke-WebRequest -Uri "https://www.lua.org/ftp/lua-5.1.5.tar.gz" -OutFile $archive
tar -xzf $archive -C $workRoot

$configuration = Join-Path $sourceRoot "luaconf.h"
$text = [IO.File]::ReadAllText($configuration)
$text = $text.Replace("#define LUA_COMPAT_VARARG", "#undef LUA_COMPAT_VARARG")
[IO.File]::WriteAllText($configuration, $text)

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$visualStudio = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
$developerShell = Join-Path $visualStudio "Common7\Tools\VsDevCmd.bat"
$environment = & cmd.exe /d /s /c "`"$developerShell`" -arch=x64 -host_arch=x64 >nul && set"
foreach ($line in $environment) {
    $separator = $line.IndexOf('=')
    if ($separator -gt 0) {
        [Environment]::SetEnvironmentVariable(
            $line.Substring(0, $separator),
            $line.Substring($separator + 1),
            "Process")
    }
}

$sources = Get-ChildItem -LiteralPath $sourceRoot -Filter "*.c" -File |
    Where-Object { $_.Name -notin @("lua.c", "luac.c") } |
    ForEach-Object { $_.FullName }

Push-Location $outputRoot
try {
    & cl.exe /nologo /MD /O2 /LD /DLUA_BUILD_AS_DLL "/I$sourceRoot" $sources /link "/OUT:$outputRoot\lua515.dll"
}
finally {
    Pop-Location
}

Copy-Item -LiteralPath (Join-Path $outputRoot "lua515.dll") -Destination (Join-Path $PSScriptRoot "win-x64\lua515.dll") -Force
Write-Host "Built $PSScriptRoot\win-x64\lua515.dll"
