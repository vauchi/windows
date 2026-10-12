// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Xml.Linq;
using Vauchi.Platform;
using Xunit;

namespace Vauchi.UnitTests;

/// <summary>
/// The vauchi.app/x landing page's "Open in Vauchi" button and every shared
/// vauchi:// link must reach the installed app (vauchi/private#650): the MSIX
/// manifest registers the protocol, and only vauchi:// links go to Core.
/// </summary>
public class DeepLinksTests
{
    [Fact]
    public void TheMsixManifestRegistersOnlyTheVauchiProtocol()
    {
        XNamespace foundation = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
        XNamespace uap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
        XDocument manifest = XDocument.Load(MsixManifestPath());

        string[] protocols = manifest
            .Descendants(foundation + "Application")
            .Single(app => (string?)app.Attribute("Id") == "App")
            .Descendants(uap + "Protocol")
            .Select(protocol => (string)protocol.Attribute("Name")!)
            .ToArray();

        Assert.Equal(new[] { "vauchi" }, protocols);
    }

    [Fact]
    public void OnlyVauchiLinksAreSelected()
    {
        string[] candidates =
        {
            @"C:\Program Files\Vauchi\Vauchi.exe",
            "vauchi://exchange?pk=AAAA&n=BBBB",
            @"C:\Users\someone\contacts.vcf",
            "https://vauchi.app/x#pk=AAAA&n=BBBB",
            "VAUCHI://exchange?pk=CCCC&n=DDDD",
            "vauchi-evil://exchange",
        };

        Assert.Equal(
            new[] { "vauchi://exchange?pk=AAAA&n=BBBB", "VAUCHI://exchange?pk=CCCC&n=DDDD" },
            DeepLinks.Select(candidates));
    }

    [Fact]
    public void TheOpenedEventCarriesTheLinkAsAJsonString()
    {
        const string link = "vauchi://exchange?pk=\"},\"Evil\":{\"x\":\"1";

        using JsonDocument opened = JsonDocument.Parse(DeepLinks.OpenedEventJson(link));

        JsonProperty only = Assert.Single(opened.RootElement.EnumerateObject());
        Assert.Equal("DeepLinkOpened", only.Name);
        Assert.Equal(link, only.Value.GetProperty("uri").GetString());
    }

    private static string MsixManifestPath([CallerFilePath] string testSourcePath = "") =>
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(testSourcePath)!, "..", "packaging", "Package.appxmanifest"));
}
