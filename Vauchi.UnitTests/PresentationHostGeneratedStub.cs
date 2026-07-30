// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml.Controls;

namespace Vauchi.CoreUI;

public sealed partial class PresentationHost
{
    private readonly Grid SurfaceGrid = new();
    private readonly Border CommandStrip = new();
    private readonly Button BackButton = new();
    private readonly Button NavigationButton = new();
    private readonly Button PrimaryButton = new();
    private readonly Button SecondaryButton = new();
    private readonly InfoBar ProtocolErrorBar = new();
    private readonly InfoBar AlertBar = new();
    private readonly InfoBar FeedbackBar = new();

    private void InitializeComponent()
    {
    }
}
