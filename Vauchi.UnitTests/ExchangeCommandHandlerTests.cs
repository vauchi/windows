// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Vauchi.Handlers;
using Xunit;

namespace Vauchi.UnitTests;

public class ExchangeCommandHandlerTests
{
    [Fact]
    public void BuildFileTypeFilter_Adds_Dot_To_Each_Extension()
    {
        string[] filters = ExchangeCommandHandler.BuildFileTypeFilter(["vcf", "vcard"]);
        Assert.Equal([".vcf", ".vcard"], filters);
    }

    [Fact]
    public void BuildFileTypeFilter_Maps_Single_Backup_Extension()
    {
        // Core (!1582) derives this from FilePickPurpose::ImportBackup — the
        // extension of the vauchi-backup.vauchi export, not the old .vbk/.bin
        // guess the frontend used to hardcode.
        string[] filters = ExchangeCommandHandler.BuildFileTypeFilter(["vauchi"]);
        Assert.Equal([".vauchi"], filters);
    }

    [Fact]
    public void BuildFileTypeFilter_Empty_Input_Yields_Wildcard()
    {
        string[] filters = ExchangeCommandHandler.BuildFileTypeFilter([]);
        Assert.Equal(["*"], filters);
    }
}
