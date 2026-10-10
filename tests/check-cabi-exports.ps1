# SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
# SPDX-License-Identifier: GPL-3.0-or-later

<#
.SYNOPSIS
    Fail when VauchiNative.cs imports an entry point vauchi_cabi.dll does not export.

.DESCRIPTION
    A LibraryImport whose export is gone compiles and only throws
    EntryPointNotFoundException when called. Core removed
    vauchi_config_enable_ble in July and Windows kept calling it on every
    launch until a crash log showed it (vauchi/private#609). This loads the
    pinned DLL and resolves every EntryPoint named in the interop file.

.PARAMETER Dll
    Path to vauchi_cabi.dll. Defaults to cabi/vauchi_cabi.dll.

.PARAMETER Interop
    The C# file holding the LibraryImport declarations.
#>
param(
    [string]$Dll = 'cabi/vauchi_cabi.dll',
    [string]$Interop = 'Vauchi/Interop/VauchiNative.cs'
)

$ErrorActionPreference = 'Stop'

$names = @(Select-String -Path $Interop -Pattern 'EntryPoint\s*=\s*"([^"]+)"' -AllMatches |
    ForEach-Object { $_.Matches } |
    ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique)
if ($names.Count -eq 0) {
    Write-Host "ERROR: no EntryPoint found in $Interop; the pattern no longer matches the declarations"
    exit 1
}

Add-Type -Namespace VauchiCheck -Name Kernel32 -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
public static extern System.IntPtr LoadLibraryW(string path);
[System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
public static extern System.IntPtr GetProcAddress(System.IntPtr module, string name);
'@

$path = (Resolve-Path $Dll).Path
$module = [VauchiCheck.Kernel32]::LoadLibraryW($path)
if ($module -eq [IntPtr]::Zero) {
    $code = [Runtime.InteropServices.Marshal]::GetLastWin32Error()
    Write-Host "ERROR: could not load $path (Win32 error $code)"
    exit 1
}

$missing = @($names | Where-Object { [VauchiCheck.Kernel32]::GetProcAddress($module, $_) -eq [IntPtr]::Zero })
Write-Host "[cabi-exports] $($names.Count) entry points imported by $Interop, $($missing.Count) missing from $Dll"
if ($missing.Count -gt 0) {
    $missing | ForEach-Object { Write-Host "  missing: $_" }
    exit 1
}
