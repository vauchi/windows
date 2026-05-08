// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Linq;
using Vauchi.Helpers;
using Xunit;

namespace Vauchi.UnitTests;

public class MimeTypeMapperTests
{
    [Fact]
    public void Maps_TextVcard_To_VcfExtension()
    {
        string[] exts = MimeTypeMapper.ToFileExtensions(["text/vcard"]).ToArray();
        Assert.Equal([".vcf"], exts);
    }

    [Fact]
    public void Maps_AllVcardVariants_To_SingleVcfExtension()
    {
        // Core advertises three vCard MIME variants for ImportContacts.
        // The mapper must dedupe so the picker doesn't show ".vcf"
        // three times.
        string[] exts = MimeTypeMapper
            .ToFileExtensions(["text/vcard", "text/x-vcard", "text/directory"])
            .ToArray();
        Assert.Equal([".vcf"], exts);
    }

    [Fact]
    public void Maps_OctetStream_To_BackupExtensions()
    {
        string[] exts = MimeTypeMapper.ToFileExtensions(["application/octet-stream"]).ToArray();
        Assert.Contains(".vbk", exts);
        Assert.Contains(".bin", exts);
    }

    [Fact]
    public void Empty_Input_Returns_Empty()
    {
        Assert.Empty(MimeTypeMapper.ToFileExtensions([]));
    }

    [Fact]
    public void Unknown_Mime_Yields_Empty_Without_Throwing()
    {
        // Unmapped MIME types are dropped silently — caller decides
        // whether to add a wildcard fallback.
        Assert.Empty(MimeTypeMapper.ToFileExtensions(["application/x-vauchi-future"]));
    }

    [Fact]
    public void Mixed_Known_And_Unknown_Yields_Only_Mapped()
    {
        string[] exts = MimeTypeMapper
            .ToFileExtensions(["text/vcard", "application/x-future", "application/octet-stream"])
            .ToArray();
        Assert.Contains(".vcf", exts);
        Assert.Contains(".vbk", exts);
        Assert.DoesNotContain("application/x-future", exts);
    }
}
