// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;

namespace Vauchi.CoreUI;

/// <summary>
/// Resolves the platform-neutral <c>icon_token</c> Core attaches to an action
/// into a Segoe Fluent Icons glyph.
/// </summary>
/// <remarks>
/// Core names its tokens after the SF Symbols core set, which shares no
/// namespace with the Windows icon font, so the translation has to be an
/// explicit table rather than a pass-through. Solid weights are chosen
/// wherever the font ships a pair: outlines lose definition at the size a
/// navigation row uses and are the first thing to disappear for low-vision
/// users. Segoe Fluent Icons is a Windows system font, so nothing is
/// packaged with the app.
/// </remarks>
public static class NavigationIcons
{
    /// <summary>
    /// Shown when Core names a token this build has not learned, so a new
    /// destination arrives with a neutral marker rather than a hole in the row.
    /// </summary>
    public const string FallbackGlyph = "\uE91F"; // CircleFill

    private static readonly IReadOnlyDictionary<string, string> GlyphsByToken =
        new Dictionary<string, string>
        {
            ["person.crop.rectangle"] = "\uEA8C",        // ContactSolid
            ["person.2"] = "\uE716",                     // People
            ["qrcode"] = "\uED14",                       // QRCode
            ["folder"] = "\uE8D5",                       // FolderFill
            ["tag"] = "\uE8EC",                          // Tag
            ["mappin.and.ellipse"] = "\uE707",           // MapPin
            ["person.badge.plus"] = "\uE8FA",            // AddFriend
            ["gearshape"] = "\uE713",                    // Settings
            ["questionmark.circle"] = "\uE9CE",          // Unknown
            ["key.horizontal"] = "\uE192",               // Permissions
            ["laptopcomputer"] = "\uE772",               // Devices
            ["externaldrive"] = "\uEDA2",                // HardDrive
            ["hand.raised"] = "\uEA18",                  // Shield
            ["bubble.left.and.bubble.right"] = "\uE8BD", // Message
            ["list.bullet.rectangle"] = "\uE8FD",        // BulletedList
            ["house"] = "\uE80F",                        // Home
        };

    /// <summary>Total over every token, present or not.</summary>
    public static string Glyph(string? token) =>
        !string.IsNullOrWhiteSpace(token)
        && GlyphsByToken.TryGetValue(token.Trim(), out string? glyph)
            ? glyph
            : FallbackGlyph;
}
