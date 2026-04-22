# SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
# SPDX-License-Identifier: GPL-3.0-or-later

<#
.SYNOPSIS
    Capture screenshots of each Vauchi screen for visual regression testing.

.DESCRIPTION
    Launches the app with --reset-for-testing (Debug build), navigates through
    each screen via UI Automation, and captures window screenshots as PNG images.

    Requires: Debug build with vauchi_cabi.dll in the output directory.

.PARAMETER AppPath
    Path to Vauchi.exe (Debug build). Defaults to standard Debug output path.

.PARAMETER OutputDir
    Directory for captured PNG images. Defaults to tests/snapshots/actual.

.PARAMETER UpdateBaselines
    If set, copies captures to tests/snapshots/baseline instead.
#>

param(
    [string]$AppPath = "",
    [string]$OutputDir = "",
    [switch]$UpdateBaselines
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# -- Paths --

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir

if (-not $AppPath) {
    $AppPath = Join-Path $RepoRoot "Vauchi\bin\x64\Debug\net8.0-windows10.0.22621.0\win-x64\Vauchi.exe"
}

$BaselineDir = Join-Path $ScriptDir "snapshots\baseline"
$ActualDir = Join-Path $ScriptDir "snapshots\actual"

if ($UpdateBaselines) {
    $OutputDir = $BaselineDir
}
elseif (-not $OutputDir) {
    $OutputDir = $ActualDir
}

if (-not (Test-Path $AppPath)) {
    Write-Error "App not found at: $AppPath`nBuild Debug first."
    exit 1
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# -- Load assemblies --

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing

# -- Win32 interop --

$win32Src = @'
using System;
using System.Runtime.InteropServices;

public class Win32Window {
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr hDC, uint nFlags);

    public const uint PW_RENDERFULLCONTENT = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left, Top, Right, Bottom;
    }
}
'@
try { Add-Type -TypeDefinition $win32Src } catch {}

# -- Screen list (matches NavTabs in MainWindow.xaml.cs) --

$Screens = @(
    @{ Name = "my_info";  Label = "My Card" },
    @{ Name = "contacts"; Label = "Contacts" },
    @{ Name = "exchange"; Label = "Exchange" },
    @{ Name = "groups";   Label = "Groups" },
    @{ Name = "settings"; Label = "More" }
)

# -- Helper: capture window screenshot --

function Capture-Window {
    param([System.Diagnostics.Process]$Process, [string]$FilePath)

    $hwnd = $Process.MainWindowHandle
    if ($hwnd -eq [IntPtr]::Zero) {
        Write-Warning "No window handle - skipping capture"
        return $false
    }

    [Win32Window]::SetForegroundWindow($hwnd) | Out-Null
    Start-Sleep -Milliseconds 500

    $rect = New-Object Win32Window+RECT
    [Win32Window]::GetWindowRect($hwnd, [ref]$rect) | Out-Null

    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top

    if ($width -le 0 -or $height -le 0) {
        Write-Warning "Invalid window dimensions: ${width}x${height}"
        return $false
    }

    # Use PrintWindow to capture the actual window content (works even when obscured)
    $bmp = New-Object System.Drawing.Bitmap($width, $height)
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $gfx.GetHdc()
    [Win32Window]::PrintWindow($hwnd, $hdc, [Win32Window]::PW_RENDERFULLCONTENT) | Out-Null
    $gfx.ReleaseHdc($hdc)
    $gfx.Dispose()
    $bmp.Save($FilePath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()

    return $true
}

# -- Helper: click nav item by name --

function Navigate-To {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$Label
    )

    $nameCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $Label)

    $element = $Window.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants, $nameCondition)

    if ($null -eq $element) {
        Write-Warning "Nav item '$Label' not found"
        return $false
    }

    $invokePattern = $null
    try {
        $invokePattern = $element.GetCurrentPattern(
            [System.Windows.Automation.InvokePattern]::Pattern)
    }
    catch {}

    if ($null -ne $invokePattern) {
        $invokePattern.Invoke()
        return $true
    }

    # Try SelectionItemPattern (NavigationViewItem)
    $selectPattern = $null
    try {
        $selectPattern = $element.GetCurrentPattern(
            [System.Windows.Automation.SelectionItemPattern]::Pattern)
    }
    catch {}

    if ($null -ne $selectPattern) {
        $selectPattern.Select()
        return $true
    }

    Write-Warning "No invoke/select pattern for '$Label'"
    return $false
}

# -- Main --

Write-Host "[snapshots] Launching Vauchi with --reset-for-testing..."
$proc = Start-Process -FilePath $AppPath -ArgumentList "--reset-for-testing" -PassThru

# Wait for window to appear
$timeout = 30
$elapsed = 0
while ($proc.MainWindowHandle -eq [IntPtr]::Zero -and $elapsed -lt $timeout) {
    Start-Sleep -Seconds 1
    $elapsed++
    $proc.Refresh()
}

if ($proc.MainWindowHandle -eq [IntPtr]::Zero) {
    Write-Error "App window did not appear within ${timeout}s"
    $proc.Kill()
    exit 1
}

Write-Host "[snapshots] Window appeared after ${elapsed}s"

# Extra settle time for rendering
Start-Sleep -Seconds 3

# WinAppSDK windows can briefly return IntPtr.Zero on MainWindowHandle
# immediately after appearing, because the visible top-level window is
# owned by a child/composition process. Retry with exponential backoff
# up to 10 s (see 2026-04-20-windows-test-jobs-broken). On every retry,
# refresh the process handle and re-query MainWindowHandle.
$hwnd = $proc.MainWindowHandle
$retry = 0
while ($hwnd -eq [IntPtr]::Zero -and $retry -lt 10) {
    Start-Sleep -Milliseconds (200 * [Math]::Pow(2, $retry))
    $proc.Refresh()
    $hwnd = $proc.MainWindowHandle
    $retry++
}
if ($hwnd -eq [IntPtr]::Zero) {
    Write-Error "MainWindowHandle still IntPtr.Zero after ${retry} retries (~10s). The visible window is likely owned by a child process — enumerate top-level windows by PID in a future fix."
    $proc.Kill()
    exit 1
}

# Get automation element for the window
$windowElement = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)

$captured = 0
$failed = 0

foreach ($screen in $Screens) {
    $name = $screen.Name
    $label = $screen.Label

    Write-Host "[snapshots] Navigating to: $label"

    if ($name -ne "my_info") {
        $navOk = Navigate-To -Window $windowElement -Label $label
        if (-not $navOk) {
            Write-Warning "[snapshots] Failed to navigate to $label - capturing anyway"
        }
        Start-Sleep -Seconds 2
    }

    $outPath = Join-Path $OutputDir "${name}.png"
    $ok = Capture-Window -Process $proc -FilePath $outPath

    if ($ok) {
        Write-Host "[snapshots]   Captured: $outPath"
        $captured++
    }
    else {
        Write-Warning "[snapshots]   FAILED: $name"
        $failed++
    }
}

# Clean up
Write-Host "[snapshots] Shutting down app..."
$proc.CloseMainWindow() | Out-Null
Start-Sleep -Seconds 2
if (-not $proc.HasExited) {
    $proc.Kill()
}

# Summary
Write-Host ""
Write-Host "[snapshots] Done: $captured captured, $failed failed"
Write-Host "[snapshots] Output: $OutputDir"

if ($failed -gt 0) {
    exit 1
}
