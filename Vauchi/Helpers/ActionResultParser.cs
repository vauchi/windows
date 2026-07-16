// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;

namespace Vauchi.Helpers;

/// <summary>
/// Classifies ActionResult JSON from vauchi-cabi into a kind enum.
/// </summary>
// TODO(HUMBLE): W — hand-maintained ActionResultKind enum/string table; core should emit generic presentation results (see _private/docs/problems/2026-07-06-desktop-tui-web-domain-shell-violations).
public enum ActionResultKind
{
    UpdateScreen,
    NavigateTo,
    ValidationError,
    Complete,
    WipeComplete,
    OpenUrl,
    ShowAlert,
    RequestCamera,
    ShowToast,
    /// <summary>
    /// `ActionResult::Commands` (renamed from `ExchangeCommands` in core
    /// 0.47.0, Phase 0 of
    /// 2026-05-04-exchange-command-screen-presentation).
    /// </summary>
    Commands,
    BackupExportComplete,
    /// <summary>
    /// Back reached a back-stopping root. The frontend performs its
    /// platform default (minimize / suspend / no-op) (ADR-044 Am2a).
    /// </summary>
    PerformNativeBack,
    Error,
    Unknown,
}

public static class ActionResultParser
{
    private static readonly string[] KnownVariants =
    [
        "UpdateScreen", "NavigateTo", "ValidationError", "Complete",
        "WipeComplete",
        "OpenUrl", "ShowAlert",
        "RequestCamera", "ShowToast", "Commands",
        "BackupExportComplete", "PerformNativeBack",
    ];

    /// <summary>
    /// Parse ActionResult JSON and return its kind.
    /// Serde externally-tagged enums are: {"VariantName": {...}} or "VariantName".
    /// </summary>
    public static ActionResultKind Classify(string resultJson)
    {
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        // Unit variant: "Complete" (string)
        if (root.ValueKind == JsonValueKind.String)
        {
            string? variant = root.GetString();
            return VariantToKind(variant);
        }

        // Error from native: {"error": "..."}
        if (root.TryGetProperty("error", out _))
            return ActionResultKind.Error;

        // Tagged object variant: {"VariantName": {...}}
        foreach (var name in KnownVariants)
        {
            if (root.TryGetProperty(name, out _))
                return VariantToKind(name);
        }

        return ActionResultKind.Unknown;
    }

    private static ActionResultKind VariantToKind(string? name) => name switch
    {
        "UpdateScreen" => ActionResultKind.UpdateScreen,
        "NavigateTo" => ActionResultKind.NavigateTo,
        "ValidationError" => ActionResultKind.ValidationError,
        "Complete" => ActionResultKind.Complete,
        "WipeComplete" => ActionResultKind.WipeComplete,
        "OpenUrl" => ActionResultKind.OpenUrl,
        "ShowAlert" => ActionResultKind.ShowAlert,
        "RequestCamera" => ActionResultKind.RequestCamera,
        "ShowToast" => ActionResultKind.ShowToast,
        "Commands" => ActionResultKind.Commands,
        "BackupExportComplete" => ActionResultKind.BackupExportComplete,
        "PerformNativeBack" => ActionResultKind.PerformNativeBack,
        _ => ActionResultKind.Unknown,
    };
}
