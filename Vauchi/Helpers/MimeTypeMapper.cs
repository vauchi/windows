// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;

namespace Vauchi.Helpers;

/// <summary>
/// Maps MIME types from <c>Command::FilePickFromUser.accepted_mime_types</c>
/// to WinUI <c>FileOpenPicker.FileTypeFilter</c> extensions. Core supplies
/// MIME, but the WinUI picker filters by extension.
///
/// Unmapped MIME types fall through silently — callers may add a wildcard
/// fallback when the resulting extension list is empty.
/// </summary>
public static class MimeTypeMapper
{
    // TODO(HUMBLE): T — frontend maps MIME types to extensions for import dialogs; core should provide accepted extensions in FilePickFromUser command (see _private/docs/problems/2026-07-06-desktop-tui-web-domain-shell-violations).
    private static readonly Dictionary<string, string[]> Map = new()
    {
        // vCard variants — the three core advertises for ImportContacts.
        ["text/vcard"]      = [".vcf"],
        ["text/x-vcard"]    = [".vcf"],
        ["text/directory"]  = [".vcf"],
        // Backup blobs (FilePickPurpose::ImportBackup).
        ["application/octet-stream"] = [".vbk", ".bin"],
    };

    /// <summary>
    /// Returns the deduplicated set of file-extension filters
    /// (e.g. <c>".vcf"</c>) for the given MIME types. Order is not
    /// guaranteed; <c>FileOpenPicker.FileTypeFilter</c> is set-like
    /// in practice.
    /// </summary>
    public static IEnumerable<string> ToFileExtensions(IEnumerable<string> mimeTypes)
    {
        var seen = new HashSet<string>();
        foreach (string mime in mimeTypes)
        {
            if (Map.TryGetValue(mime, out var exts))
            {
                foreach (string ext in exts)
                {
                    if (seen.Add(ext))
                        yield return ext;
                }
            }
        }
    }
}
