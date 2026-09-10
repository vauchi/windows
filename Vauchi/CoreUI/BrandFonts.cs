// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Vauchi.CoreUI;

/// <summary>
/// Resolves a Core text-role style (<c>heading</c>, <c>body</c>,
/// <c>caption</c>, <c>monospace</c>, <c>muted</c>) into the vendored brand
/// font that renders it, as a WinUI <c>FontFamily</c> source string.
/// </summary>
/// <remarks>
/// <c>PresentationSurfaceFields.RenderText</c> builds its <c>TextBlock</c> in
/// code rather than XAML, so it cannot bind to the
/// <c>BrandDisplayFont</c>/<c>BrandBodyFont</c>/<c>BrandMonoFont</c>
/// resources declared in <c>App.xaml</c> — these constants are the value
/// both places share; keep them in sync by hand if a font file moves.
/// </remarks>
public static class BrandFonts
{
    public const string DisplaySource =
        "ms-appx:///Assets/Fonts/BricolageGrotesque[opsz,wdth,wght].ttf#Bricolage Grotesque";
    public const string BodySource =
        "ms-appx:///Assets/Fonts/HankenGrotesk[wght].ttf#Hanken Grotesk";
    public const string MonoSource =
        "ms-appx:///Assets/Fonts/JetBrainsMono[wght].ttf#JetBrains Mono";

    /// <summary>Total over every text-role style, known or not.</summary>
    public static string SourceForStyle(string? style) => style switch
    {
        "heading" => DisplaySource,
        "monospace" => MonoSource,
        _ => BodySource,
    };
}
