# SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
# SPDX-License-Identifier: GPL-3.0-or-later
#
# Vauchi Windows UI Automation Helper
# Usage: .\interact.ps1 <command> [args...]
#
# Commands:
#   launch              - Launch Vauchi and wait for window
#   screenshot <path>   - Capture the Vauchi window to PNG
#   tree                - Dump the UI automation tree (element names + types)
#   click <name>        - Click a button/element by automation name
#   type <name> <text>  - Type text into a text field by automation name
#   read-title          - Read the current screen title
#   read-all            - Read all visible text elements
#   kill                - Close Vauchi

param(
    [Parameter(Position=0)] [string]$Command,
    [Parameter(Position=1)] [string]$Arg1,
    [Parameter(Position=2)] [string]$Arg2
)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing

$AppPath = "C:\Users\matti\Workspace\vauchi\windows\Vauchi\bin\x64\Release\net8.0-windows10.0.22621.0\win-x64\Vauchi.exe"

function Get-VauchiWindow {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, "Vauchi")
    $win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
    if (-not $win) {
        # Try by process
        $p = Get-Process -Name Vauchi -ErrorAction SilentlyContinue
        if ($p) {
            $pidCond = New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $p.Id)
            $win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $pidCond)
        }
    }
    return $win
}

function Dump-Tree {
    param($element, $indent = 0)
    $prefix = " " * $indent
    $name = $element.Current.Name
    $type = $element.Current.ControlType.ProgrammaticName
    $enabled = $element.Current.IsEnabled
    if ($name -or $type -ne "ControlType.Custom") {
        $short = $type -replace "ControlType\.", ""
        if ($name) {
            Write-Output "$prefix[$short] `"$name`" (enabled=$enabled)"
        } else {
            Write-Output "$prefix[$short] (enabled=$enabled)"
        }
    }
    $children = $element.FindAll(
        [System.Windows.Automation.TreeScope]::Children,
        [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($child in $children) {
        Dump-Tree $child ($indent + 2)
    }
}

function Click-Element {
    param($window, $name)
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $el = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if (-not $el) {
        Write-Error "Element '$name' not found"
        return $false
    }
    # Try InvokePattern (buttons)
    try {
        $invoke = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        if ($invoke) { $invoke.Invoke(); Write-Output "Clicked: $name"; return $true }
    } catch {}
    # Try TogglePattern (toggle buttons)
    try {
        $toggle = $el.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
        if ($toggle) { $toggle.Toggle(); Write-Output "Toggled: $name"; return $true }
    } catch {}
    # Try SelectionItemPattern (nav tabs, list items)
    try {
        $select = $el.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
        if ($select) { $select.Select(); Write-Output "Selected: $name"; return $true }
    } catch {}
    # Fallback: focus and send Enter
    try {
        $el.SetFocus()
        Start-Sleep -Milliseconds 100
        Add-Type -AssemblyName System.Windows.Forms
        [System.Windows.Forms.SendKeys]::SendWait(" ")
        Write-Output "Space-activated: $name"
        return $true
    } catch {}
    Write-Error "Element '$name' - no supported interaction pattern"
    return $false
}

function Type-Text {
    param($window, $name, $text)
    # Find Edit control by name (not TextBlock labels which share the same name)
    $nameCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $typeCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Edit)
    $cond = New-Object System.Windows.Automation.AndCondition($nameCond, $typeCond)
    $el = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    # Fallback to any element with that name
    if (-not $el) {
        $el = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $nameCond)
    }
    if (-not $el) {
        Write-Error "Element '$name' not found"
        return $false
    }
    # Try ValuePattern first (works for some controls)
    try {
        $value = $el.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
        if ($value) {
            $value.SetValue($text)
            Write-Output "Typed '$text' into: $name (ValuePattern)"
            return $true
        }
    } catch {}
    # Fallback: focus element and use SendKeys
    try {
        $el.SetFocus()
        Start-Sleep -Milliseconds 200
        # Clear existing text first
        [System.Windows.Forms.SendKeys]::SendWait("^a")
        Start-Sleep -Milliseconds 100
        # SendKeys needs special escaping for +^%~{}[]()
        $escaped = $text -replace '([+^%~{}[\]()])', '{$1}'
        [System.Windows.Forms.SendKeys]::SendWait($escaped)
        Write-Output "Typed '$text' into: $name (SendKeys)"
        return $true
    } catch {
        Write-Error "Failed to type into '$name': $_"
        return $false
    }
}

function Capture-Window {
    param($path)
    Add-Type @'
using System; using System.Runtime.InteropServices; using System.Drawing;
public class WinCap {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
'@
    $p = Get-Process -Name Vauchi -ErrorAction SilentlyContinue
    if (-not $p) { Write-Error "Vauchi not running"; return }
    $rect = New-Object WinCap+RECT
    [WinCap]::GetWindowRect($p.MainWindowHandle, [ref]$rect)
    $w = $rect.R - $rect.L; $h = $rect.B - $rect.T
    if ($w -le 0 -or $h -le 0) { Write-Error "Invalid window size"; return }
    $bmp = New-Object Drawing.Bitmap($w, $h)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.L, $rect.T, 0, 0, (New-Object Drawing.Size($w, $h)))
    $bmp.Save($path)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "Screenshot saved: $path"
}

# ── Command dispatch ─────────────────────────────────────────────

switch ($Command) {
    "launch" {
        $existing = Get-Process -Name Vauchi -ErrorAction SilentlyContinue
        if ($existing) { Write-Output "Already running: PID $($existing.Id)"; exit 0 }
        Start-Process $AppPath
        for ($i = 0; $i -lt 10; $i++) {
            Start-Sleep -Seconds 1
            $win = Get-VauchiWindow
            if ($win) { Write-Output "Launched: PID $((Get-Process Vauchi).Id)"; exit 0 }
        }
        Write-Error "Vauchi did not start within 10s"
        exit 1
    }
    "kill" {
        Stop-Process -Name Vauchi -Force -ErrorAction SilentlyContinue
        Write-Output "Killed"
    }
    "screenshot" {
        if (-not $Arg1) { $Arg1 = "C:\Users\matti\Workspace\vauchi\vauchi_auto.png" }
        Capture-Window $Arg1
    }
    "tree" {
        $win = Get-VauchiWindow
        if (-not $win) { Write-Error "Vauchi window not found"; exit 1 }
        Dump-Tree $win
    }
    "click" {
        if (-not $Arg1) { Write-Error "Usage: interact.ps1 click <name>"; exit 1 }
        $win = Get-VauchiWindow
        if (-not $win) { Write-Error "Vauchi window not found"; exit 1 }
        Click-Element $win $Arg1
    }
    "type" {
        if (-not $Arg1 -or -not $Arg2) { Write-Error "Usage: interact.ps1 type <name> <text>"; exit 1 }
        $win = Get-VauchiWindow
        if (-not $win) { Write-Error "Vauchi window not found"; exit 1 }
        Type-Text $win $Arg1 $Arg2
    }
    "read-title" {
        $win = Get-VauchiWindow
        if (-not $win) { Write-Error "Vauchi window not found"; exit 1 }
        $cond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Text)
        $texts = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
        foreach ($t in $texts) {
            if ($t.Current.Name) { Write-Output $t.Current.Name; break }
        }
    }
    "enter" {
        Add-Type -AssemblyName System.Windows.Forms
        [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
        Write-Output "Sent Enter"
    }
    "wait" {
        $ms = if ($Arg1) { [int]$Arg1 } else { 1000 }
        Start-Sleep -Milliseconds $ms
        Write-Output "Waited ${ms}ms"
    }
    "read-all" {
        $win = Get-VauchiWindow
        if (-not $win) { Write-Error "Vauchi window not found"; exit 1 }
        $cond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Text)
        $texts = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
        foreach ($t in $texts) {
            if ($t.Current.Name) { Write-Output $t.Current.Name }
        }
    }
    default {
        Write-Output "Commands: launch, kill, screenshot [path], tree, click <name>, type <name> <text>, read-title, read-all"
    }
}
