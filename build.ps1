<#
.SYNOPSIS
    发布 FrpGUI 应用程序到不同平台。

.DESCRIPTION
    此脚本用于发布 FrpGUI 应用程序到 Windows、Linux、macOS 和浏览器平台，并根据当前平台自动启用AOT编译。

.PARAMETER w
    发布到 Windows 平台。

.PARAMETER l
    发布到 Linux 平台。

.PARAMETER m
    发布到 macOS 平台。

.PARAMETER c
    设置此标志以创建客户端的发布版本。

.PARAMETER s
    设置此标志以创建服务器的发布版本。

.EXAMPLE
    .\YourScript.ps1 -w -c -s
    发布到 Windows 平台，创建客户端和服务器的发布版本。

.EXAMPLE
    .\YourScript.ps1 -l
    发布到 Linux 平台。

.EXAMPLE
    .\YourScript.ps1 -m -c
    发布到 macOS 平台，仅创建客户端的发布版本。
#>

param(
    [Parameter()]
    [switch]$w, #Windows
    [switch]$l, #Linux
    [switch]$m, #MacOS

    [switch]$c, #Clients
    [switch]$s #Servers
)

# 如果 $w, $l, $m 都为 false，则全部设为 true
if (-not ($w -or $l -or $m)) {
    $w = $true
    $l = $true
    $m = $true
}

# 如果 $c, $s 都为 false，则全部设为 true
if (-not ($c -or $s)) {
    $c = $true
    $s = $true
}

if ($w) { Write-Host "发布Windows" }
if ($l) { Write-Host "发布Linux" }
if ($m) { Write-Host "发布MacOS" }
if ($c) { Write-Host "发布客户端" }
if ($s) { Write-Host "发布服务器端" }
pause


$ErrorActionPreference = 'Stop'

try {
    # 检查是否安装了.NET SDK
    try {
        dotnet
    }
    catch {
        throw "未安装.NET SDK"
    }
    
    # 获取当前平台
    $currentPlatform = if ($IsWindows) { "win-x64" } elseif ($IsLinux) { "linux-x64" } elseif ($IsMacOS) { "osx-x64" } else { "unknown" }

    
    function Publish-UI {
        param (
            [string]$runtime,
            [string]$outputDirectory
        )

        Write-Output "正在发布客户端：$runtime"
        
        # 如果目标平台与当前平台匹配，则启用AOT
        $aotFlag = if ($runtime -eq $currentPlatform) { "/p:PublishAot=true" } else { "/p:PublishAot=false" }
        $singleFileFlag = if ($runtime -eq $currentPlatform) { "/p:PublishSingleFile=false" } else { "/p:PublishSingleFile=true" }

        dotnet publish FrpGUI.Avalonia.Desktop -r $runtime -c Release -o $outputDirectory --self-contained $aotFlag $singleFileFlag

        $platform = switch ($runtime) {
            "win-x64" { "windows_amd64" }
            "linux-x64" { "linux_amd64" }
            "osx-x64" { "darwin_amd64" }
        }
        mkdir $outputDirectory/frp -ErrorAction SilentlyContinue
        Copy-Item "bin/frp_*_$platform/*" $outputDirectory/frp -Recurse

        if (Test-Path $outputDirectory/FrpGUI.Avalonia.Desktop.exe) {
            Move-Item $outputDirectory/FrpGUI.Avalonia.Desktop.exe $outputDirectory/FrpGUI.exe
        }

        if (Test-Path $outputDirectory/FrpGUI.Avalonia.Desktop) {
            Move-Item $outputDirectory/FrpGUI.Avalonia.Desktop $outputDirectory/FrpGUI
        }
    }
    
    function Publish-Service {
        param (
            [string]$runtime,
            [string]$outputDirectory
        )

        Write-Output "正在发布服务：$runtime"
        
        # 如果目标平台与当前平台匹配，则启用AOT
        $aotFlag = if ($runtime -eq $currentPlatform) { "/p:PublishAot=true" } else { "/p:PublishAot=false" }

        dotnet publish FrpGUI.WebAPI -r $runtime -c Release -o $outputDirectory --self-contained #$aotFlag

        $platform = switch ($runtime) {
            "win-x64" { "windows_amd64" }
            "linux-x64" { "linux_amd64" }
            "osx-x64" { "darwin_amd64" }
        }
        mkdir $outputDirectory/frp -ErrorAction SilentlyContinue
        Copy-Item "bin/frp_*_$platform/*" $outputDirectory/frp -Recurse
    }

    Clear-Host
    
     Write-Output "当前平台：$currentPlatform"

    # 如果Publish目录存在，则删除
    if (Test-Path "Publish") {
        Remove-Item "Publish" -Recurse -Force
    }

    if ($c) {
        if ($w) { Publish-UI -runtime "win-x64" -outputDirectory "Publish/client-win-x64" }
        if ($l) { Publish-UI -runtime "linux-x64" -outputDirectory "Publish/client-linux-x64" }
        if ($m) { Publish-UI -runtime "osx-x64" -outputDirectory "Publish/client-macos-x64" }
    }
    
    if ($s) {
        if ($w) { Publish-Service -runtime "win-x64" -outputDirectory "Publish/server-win-x64" }
        if ($l) { Publish-Service -runtime "linux-x64" -outputDirectory "Publish/server-linux-x64" }
        if ($m) { Publish-Service -runtime "osx-x64" -outputDirectory "Publish/server-macos-x64" }
    }
    
    Write-Output "操作完成"

    Invoke-Item Publish
    pause
}
catch {
    Write-Error $_
}