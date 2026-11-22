# SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
# SPDX-License-Identifier: GPL-3.0-or-later

<#
.SYNOPSIS
  Reads the running app's UI Automation tree, the one Narrator reads, on
  every navigation destination and checks what a screen reader needs.

.DESCRIPTION
  For each destination of a fresh, seeded identity:
    - the window has a name;
    - every control a user can operate (button, text field, drop-down,
      list item, switch) has a non-empty name;
    - every navigation destination has an automation id.
  Core supplies the names; this checks that the shell hands them to the
  platform. It replaces three skipped placeholders in Vauchi.Tests
  (vauchi/private#348).

  Two controls on screen sharing one automation id are reported as a
  warning, not a failure: the ids are Core's and repeat across the list
  and detail surfaces (vauchi/private#1099 notes).

  Needs a Debug build (--reset-for-testing) and a desktop session.

.PARAMETER AppPath
  Path to Vauchi.exe.
#>
param(
    [Parameter(Mandatory = $true)][string]$AppPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$Automation = [System.Windows.Automation.AutomationElement]
$Descendants = [System.Windows.Automation.TreeScope]::Descendants
$ControlType = [System.Windows.Automation.ControlType]
$Operable = @(
    $ControlType::Button, $ControlType::Edit, $ControlType::ComboBox,
    $ControlType::ListItem, $ControlType::CheckBox, $ControlType::Slider,
    $ControlType::Hyperlink, $ControlType::RadioButton
)
$WaitMs = 8000

$script:failed = 0
$script:checked = 0
function Fail {
    param([string]$What)
    Write-Host "[a11y] FAIL $What"
    $script:failed++
}

function Wait-Until {
    param([scriptblock]$Probe)
    $waited = 0
    while ($waited -lt $WaitMs) {
        $result = & $Probe
        if ($result) { return $result }
        Start-Sleep -Milliseconds 100
        $waited += 100
    }
    return $null
}

function Get-Operable {
    $found = @()
    foreach ($element in $script:window.FindAll($Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        try {
            $current = $element.Current
            if ($Operable -contains $current.ControlType -and -not $current.IsOffscreen) {
                $found += $element
            }
        }
        catch [System.Windows.Automation.ElementNotAvailableException] {
            # Replaced by a re-render while the tree was being read.
        }
    }
    return $found
}

function Check-Screen {
    param([string]$Screen)
    $controls = @(Get-Operable)
    if ($controls.Count -eq 0) {
        Fail "${Screen}: no operable control was found, so nothing was checked"
        return
    }
    $unnamed = 0
    $ids = @{}
    foreach ($control in $controls) {
        $current = $control.Current
        $type = $current.ControlType.ProgrammaticName -replace 'ControlType\.', ''
        $script:checked++
        if ([string]::IsNullOrWhiteSpace($current.Name)) {
            Fail "${Screen}: a $type with automation id '$($current.AutomationId)' has no name"
            $unnamed++
        }
        if ($current.AutomationId) {
            $ids[$current.AutomationId] = 1 + [int]$ids[$current.AutomationId]
        }
    }
    foreach ($id in $ids.Keys) {
        if ($ids[$id] -gt 1) {
            Write-Warning "[a11y] ${Screen}: $($ids[$id]) controls share the automation id '$id'"
        }
    }
    Write-Host "[a11y] ${Screen}: $($controls.Count) operable controls, $unnamed without a name"
}

# -- Launch on a fresh, seeded identity --

Get-Process -Name Vauchi -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
$dataDir = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Vauchi'
if (Test-Path $dataDir) { Remove-Item -Path $dataDir -Recurse -Force }

$process = Start-Process -FilePath $AppPath -ArgumentList "--reset-for-testing" -PassThru
$handle = Wait-Until {
    $process.Refresh()
    if ($process.MainWindowHandle -ne [IntPtr]::Zero) { $process.MainWindowHandle } else { $null }
}
if ($null -eq $handle) {
    Write-Error "The app showed no window."
    exit 1
}
$script:window = $Automation::FromHandle($handle)

try {
    if ([string]::IsNullOrWhiteSpace($script:window.Current.Name)) {
        Fail "the window has no name"
    }

    $selectPattern = [System.Windows.Automation.SelectionItemPattern]::Pattern
    $itemCondition = New-Object System.Windows.Automation.PropertyCondition(
        $Automation::ControlTypeProperty, $ControlType::ListItem)
    $destinations = Wait-Until {
        $names = @()
        foreach ($item in $script:window.FindAll($Descendants, $itemCondition)) {
            $pattern = $null
            if ($item.TryGetCurrentPattern($selectPattern, [ref]$pattern)) {
                if (-not $item.Current.AutomationId) {
                    Fail "the destination '$($item.Current.Name)' has no automation id"
                }
                $names += $item.Current.Name
            }
        }
        if ($names.Count -gt 1) { , $names } else { $null }
    }
    if ($null -eq $destinations) {
        Fail "fewer than two navigation destinations were found"
        $destinations = @()
    }

    foreach ($destination in $destinations) {
        $nameCondition = New-Object System.Windows.Automation.PropertyCondition(
            $Automation::NameProperty, $destination)
        $item = Wait-Until {
            foreach ($candidate in $script:window.FindAll($Descendants, $nameCondition)) {
                $pattern = $null
                if ($candidate.TryGetCurrentPattern($selectPattern, [ref]$pattern)) { return $candidate }
            }
            return $null
        }
        if ($null -eq $item) {
            Fail "the destination '$destination' disappeared"
            continue
        }
        $item.GetCurrentPattern($selectPattern).Select()
        # The surface is replaced after the selection; let it settle.
        Start-Sleep -Milliseconds 1500
        Check-Screen $destination
    }
}
catch {
    Fail "the walk stopped: $_"
}
finally {
    $process.Refresh()
    if ($process.HasExited) {
        Fail "the app exited during the walk (exit code $($process.ExitCode))"
    }
    else {
        $process.Kill()
    }
}

if ($script:checked -eq 0) {
    Fail "no control was checked"
}
if ($script:failed -gt 0) {
    Write-Error "$($script:failed) accessibility check(s) failed."
    exit 1
}
Write-Host "[a11y] $($script:checked) controls checked, all named"
