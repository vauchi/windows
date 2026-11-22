# SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
# SPDX-License-Identifier: GPL-3.0-or-later

<#
.SYNOPSIS
  Sends one value of each kind through the running app and checks that it
  comes back: text, a switch and a choice.

.DESCRIPTION
  Until vauchi/private#1099 this shell tagged every value in a way Core
  could not read, and nothing noticed: no typed text, switch or choice
  reached Core from Windows. This drives the real window through UI
  Automation and asserts only the round trip, which is the shell's part:
  what was entered is what the next surface shows. What Core does with a
  value is Core's to test.

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
$WaitMs = 8000

$script:failed = 0
function Report {
    param([bool]$Ok, [string]$What)
    if ($Ok) { Write-Host "[inputs] PASS $What" }
    else { Write-Host "[inputs] FAIL $What"; $script:failed++ }
}

# Polls instead of sleeping a fixed time: the app answers each event with
# a new surface, and the elements found before it are gone.
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

function Find-WithPattern {
    param(
        [System.Windows.Automation.AutomationProperty]$Property,
        [object]$Value,
        [System.Windows.Automation.AutomationPattern]$Pattern
    )
    $condition = New-Object System.Windows.Automation.PropertyCondition($Property, $Value)
    return Wait-Until {
        foreach ($element in $script:window.FindAll($Descendants, $condition)) {
            $found = $null
            if ($element.Current.IsEnabled -and $element.TryGetCurrentPattern($Pattern, [ref]$found)) {
                return $element
            }
        }
        return $null
    }
}

function Press {
    param([string]$Name)
    $pattern = [System.Windows.Automation.InvokePattern]::Pattern
    $element = Find-WithPattern $Automation::NameProperty $Name $pattern
    if ($null -eq $element) { throw "no enabled button named '$Name'" }
    $element.GetCurrentPattern($pattern).Invoke()
}

function Press-Primary {
    $pattern = [System.Windows.Automation.InvokePattern]::Pattern
    $element = Find-WithPattern $Automation::AutomationIdProperty "context-primary" $pattern
    if ($null -eq $element) { throw "no enabled primary action" }
    $element.GetCurrentPattern($pattern).Invoke()
}

function Open-Destination {
    param([string]$Name)
    $pattern = [System.Windows.Automation.SelectionItemPattern]::Pattern
    $element = Find-WithPattern $Automation::NameProperty $Name $pattern
    if ($null -eq $element) { throw "no destination named '$Name'" }
    $element.GetCurrentPattern($pattern).Select()
}

function Find-FirstOfType {
    param([System.Windows.Automation.ControlType]$Type, [System.Windows.Automation.AutomationPattern]$Pattern)
    return Find-WithPattern $Automation::ControlTypeProperty $Type $Pattern
}

function Text-IsShown {
    param([string]$Text)
    $condition = New-Object System.Windows.Automation.PropertyCondition($Automation::NameProperty, $Text)
    return $null -ne (Wait-Until { $script:window.FindFirst($Descendants, $condition) })
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
    # -- Text: a new display name is the card's title afterwards --
    $newName = "Round Trip $(Get-Random -Maximum 100000)"
    Open-Destination "Settings"
    Press "Display Name"
    $valuePattern = [System.Windows.Automation.ValuePattern]::Pattern
    $edit = Find-FirstOfType ([System.Windows.Automation.ControlType]::Edit) $valuePattern
    if ($null -eq $edit) { throw "the display-name screen has no text field" }
    $edit.GetCurrentPattern($valuePattern).SetValue($newName)
    Press-Primary
    Open-Destination "My Card"
    Report (Text-IsShown $newName) "text: the card shows the name that was typed"

    # -- Boolean: a switch keeps its new state across a re-render --
    Open-Destination "Settings"
    $togglePattern = [System.Windows.Automation.TogglePattern]::Pattern
    $switch = Find-FirstOfType ([System.Windows.Automation.ControlType]::Button) $togglePattern
    if ($null -eq $switch) { throw "Settings has no switch" }
    $switchId = $switch.Current.Name
    $before = $switch.GetCurrentPattern($togglePattern).Current.ToggleState
    $switch.GetCurrentPattern($togglePattern).Toggle()
    Open-Destination "Contacts"
    Open-Destination "Settings"
    $after = Wait-Until {
        $again = Find-WithPattern $Automation::NameProperty $switchId $togglePattern
        if ($null -eq $again) { return $null }
        $state = $again.GetCurrentPattern($togglePattern).Current.ToggleState
        if ($state -ne $before) { $state } else { $null }
    }
    Report ($null -ne $after) "boolean: '$switchId' went from $before to the other state and stayed"

    # -- Choice: the option picked in a drop-down is the one selected --
    Press "Appearance & Language"
    $expandPattern = [System.Windows.Automation.ExpandCollapsePattern]::Pattern
    $combo = Find-FirstOfType ([System.Windows.Automation.ControlType]::ComboBox) $expandPattern
    if ($null -eq $combo) { throw "Appearance & Language has no drop-down" }
    $comboName = $combo.Current.Name
    $combo.GetCurrentPattern($expandPattern).Expand()
    $itemCondition = New-Object System.Windows.Automation.PropertyCondition(
        $Automation::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)
    $options = Wait-Until {
        $items = $combo.FindAll($Descendants, $itemCondition)
        if ($items.Count -gt 1) { $items } else { $null }
    }
    if ($null -eq $options) { throw "'$comboName' offered fewer than two options" }
    $picked = $options[1].Current.Name
    $options[1].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $selectionPattern = [System.Windows.Automation.SelectionPattern]::Pattern
    $selected = Wait-Until {
        $again = Find-WithPattern $Automation::NameProperty $comboName $selectionPattern
        if ($null -eq $again) { return $null }
        $names = @($again.GetCurrentPattern($selectionPattern).Current.GetSelection() |
            ForEach-Object { $_.Current.Name })
        if ($names -contains $picked) { $picked } else { $null }
    }
    Report ($null -ne $selected) "choice: '$comboName' shows '$picked' after it was picked"
}
catch {
    Report $false "the walk stopped: $_"
}
finally {
    $process.Refresh()
    if ($process.HasExited) {
        Report $false "the app exited during the walk (exit code $($process.ExitCode))"
    }
    else {
        $process.Kill()
    }
}

if ($script:failed -gt 0) {
    Write-Error "$($script:failed) input check(s) failed."
    exit 1
}
Write-Host "[inputs] every value came back"
