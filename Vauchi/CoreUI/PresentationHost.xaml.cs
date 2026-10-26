// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Vauchi.Interop;
using Vauchi.Platform;
using Vauchi.Services;
using Windows.System;

namespace Vauchi.CoreUI;

public sealed partial class PresentationHost : UserControl
{
    private PresentationState _state = new();
    private IntPtr _appHandle;
    private bool _reducedMotion;
    private string _colorScheme = "";
    // Held so the ColorValuesChanged subscription lives as long as the host.
    private Windows.UI.ViewManagement.UISettings? _uiSettings;
    private readonly Brush? _markupOverlayBackground;
    private readonly Brush? _markupOverlayBorder;
    private int _reportedWidth = -1;
    private int _reportedHeight = -1;
    private string _overlaySurfaceId = "";
    private string _overlayKind = "";
    private Button? _activeSecondaryAnchor;

    public event Action<JsonElement>? NativeEffectReady;
    public event Action? NativeBackRequested;

    public PresentationHost()
    {
        InitializeComponent();
        _markupOverlayBackground = OverlayPanel.Background;
        _markupOverlayBorder = OverlayPanel.BorderBrush;
        _reducedMotion = ReducedMotionRequested();
        _colorScheme = SystemColorScheme.Current();
        SizeChanged += (_, args) =>
            ReportEnvironment((int)args.NewSize.Width, (int)args.NewSize.Height);
        WatchColorScheme();
    }

    /// <summary>
    /// Under "Follow system" Core repaints when the OS switches between
    /// light and dark, so the switch is reported as it happens.
    /// </summary>
    private void WatchColorScheme()
    {
        try
        {
            _uiSettings = new Windows.UI.ViewManagement.UISettings();
            _uiSettings.ColorValuesChanged += (_, _) =>
                DispatcherQueue.TryEnqueue(() =>
                {
                    string scheme = SystemColorScheme.Current();
                    if (scheme == _colorScheme)
                        return;
                    _colorScheme = scheme;
                    ReportEnvironment(_reportedWidth, _reportedHeight, force: true);
                });
        }
        catch (Exception)
        {
            _uiSettings = null;
        }
    }

    public void Initialize(IntPtr appHandle)
    {
        _appHandle = appHandle;
        Refresh();
        ReportEnvironment((int)ActualWidth, (int)ActualHeight, force: true);
    }

    public void Refresh()
    {
        if (_appHandle == IntPtr.Zero)
            return;
        string? envelope = VauchiNative.AppInitialCommands(_appHandle);
        if (!string.IsNullOrEmpty(envelope))
            ApplyEnvelope(envelope);
    }

    public void ApplyEnvelope(string envelope)
    {
        string focusId = CurrentFocusId();
        if (!_state.TryApplyEnvelope(envelope, out var effects, out string? error))
        {
            ShowProtocolError(error ?? "Core returned an invalid presentation batch");
            return;
        }

        RenderPresentation();
        RestoreFocus(focusId);
        foreach (JsonElement effect in effects)
            ExecuteEffect(effect);
    }

    public void DispatchPlatformEvent(string eventJson) =>
        Dispatch(eventJson);

    /// <summary>
    /// Shows an already-reduced state without a Core handle, for the
    /// screen-catalog renderer. Effects are not executed: nothing here
    /// can answer them, and the catalog is a set of stills.
    /// </summary>
    public void Present(PresentationState state)
    {
        _state = state;
        RenderPresentation();
    }

    public void RequestBack()
    {
        if (_state.ActiveSurfaceId is not { Length: > 0 } surfaceId)
            return;
        Dispatch(PresentationEvents.SurfaceActivated(surfaceId));
        Dispatch(PresentationEvents.BackRequested(surfaceId));
    }

    private void ReportEnvironment(int width, int height, bool force = false)
    {
        if (_appHandle == IntPtr.Zero)
            return;
        width = Math.Max(0, width);
        height = Math.Max(0, height);
        if (!force && width == _reportedWidth && height == _reportedHeight)
            return;
        _reportedWidth = width;
        _reportedHeight = height;
        Dispatch(PresentationEvents.EnvironmentChanged(width, height, _reducedMotion, _colorScheme));
    }

    private void DispatchSurfaceEvent(string surfaceId, string eventJson)
    {
        Dispatch(PresentationEvents.SurfaceActivated(surfaceId));
        Dispatch(eventJson);
    }

    private void Dispatch(string eventJson)
    {
        if (_appHandle == IntPtr.Zero)
            return;
        string? envelope = VauchiNative.AppDispatch(_appHandle, eventJson);
        if (string.IsNullOrEmpty(envelope))
        {
            ShowProtocolError("Core did not return a presentation batch");
            return;
        }
        ApplyEnvelope(envelope);
    }

    private void RenderPresentation()
    {
        SurfaceGrid.Children.Clear();
        SurfaceGrid.ColumnDefinitions.Clear();
        IReadOnlyList<string> visible = _state.VisibleSurfaceIds;
        foreach (string surfaceId in visible)
            SurfaceGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
            });

        _activeSecondaryAnchor = null;
        SurfaceColors? activeColors = _state.ActiveSurfaceId is { Length: > 0 } activeId
            && _state.Surface(activeId) is { } activeSurface
                ? SurfaceColors.From(activeSurface)
                : null;
        PaintChrome(activeColors);
        for (int index = 0; index < visible.Count; index++)
        {
            string surfaceId = visible[index];
            if (_state.Surface(surfaceId) is not { } surface)
                continue;
            var renderer = new PresentationSurface(surface, _state.ContextBar(surfaceId));
            renderer.EventReady += DispatchSurfaceEvent;
            if (surfaceId == _state.ActiveSurfaceId)
                _activeSecondaryAnchor = renderer.SecondaryActionButton;
            var border = new Border
            {
                Child = renderer,
                BorderThickness = surfaceId == _state.ActiveSurfaceId
                    ? new Thickness(2)
                    : new Thickness(0),
            };
            if (SurfaceColors.From(surface) is { } colors)
                border.BorderBrush = new SolidColorBrush(colors.ActiveSurfaceBorder);
            Grid.SetColumn(border, index);
            SurfaceGrid.Children.Add(border);
        }
        RegisterShortcuts(_state.ActiveContextBar);
        RenderNavigation(activeColors);
        RenderOverlay();
    }

    /// <summary>
    /// The window chrome and the overlay take the active surface's colours:
    /// overlays carry no tokens of their own (vauchi/private#814). Without a
    /// palette the XAML theme resources stay.
    /// </summary>
    private void PaintChrome(SurfaceColors? colors)
    {
        if (colors is null)
        {
            RequestedTheme = ElementTheme.Default;
            SurfaceGrid.ClearValue(Panel.BackgroundProperty);
            ClearValue(ForegroundProperty);
            OverlayPanel.Background = _markupOverlayBackground;
            OverlayPanel.BorderBrush = _markupOverlayBorder;
            PaintPane(null, ElementTheme.Default);
            return;
        }
        ElementTheme theme = colors.IsLight ? ElementTheme.Light : ElementTheme.Dark;
        RequestedTheme = theme;
        SurfaceGrid.Background = new SolidColorBrush(colors.Background);
        Foreground = new SolidColorBrush(colors.Text);
        OverlayPanel.Background = new SolidColorBrush(colors.SurfaceElevated);
        OverlayPanel.BorderBrush = new SolidColorBrush(colors.Border);
        PaintPane(new SolidColorBrush(colors.Surface), theme);
    }

    // The keys NavigationView's template reads its pane background from.
    private static readonly string[] PaneBackgroundKeys =
    [
        "NavigationViewDefaultPaneBackground",
        "NavigationViewExpandedPaneBackground",
        "NavigationViewTopPaneBackground",
    ];

    /// <summary>
    /// The navigation pane takes the active surface's <c>surface</c> colour
    /// instead of the system backdrop (vauchi/private#814).
    /// </summary>
    private void PaintPane(Brush? background, ElementTheme theme)
    {
        foreach (string key in PaneBackgroundKeys)
        {
            if (background is null)
                Sidebar.Resources.Remove(key);
            else
                Sidebar.Resources[key] = background;
        }
        // The template resolves these keys only when its theme changes, so a
        // new palette under the same light/dark would otherwise not show.
        Sidebar.RequestedTheme = theme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
        Sidebar.RequestedTheme = theme;
    }

    /// <summary>
    /// Core now publishes navigation persistently via
    /// <c>SetNavigation</c> rather than only inside a dismissable
    /// overlay (see D4 in the cross-frontend design review), so the
    /// pane renders every time state changes instead of on demand.
    /// </summary>
    private void RenderNavigation(SurfaceColors? colors)
    {
        SidebarModel model = SidebarModel.From(_state.ActiveNavigation);
        Sidebar.MenuItems.Clear();
        foreach (SidebarItemModel item in model.Items)
        {
            var navigationItem = new NavigationViewItem
            {
                Content = item.Label,
                Icon = new FontIcon { Glyph = item.Glyph },
                IsSelected = item.Selected,
                Tag = item.InteractionId,
            };
            if (item.BadgeCount > 0)
                navigationItem.InfoBadge = new InfoBadge { Value = item.BadgeCount };
            FocusVisualStyle.Apply(navigationItem, colors);
            AutomationProperties.SetAutomationId(navigationItem, item.InteractionId);
            AutomationProperties.SetName(navigationItem, item.AccessibilityLabel);
            Sidebar.MenuItems.Add(navigationItem);
        }
        Sidebar.IsPaneVisible = !model.Hidden;
        Sidebar.PaneDisplayMode = _state.WindowClass == "compact"
            ? NavigationViewPaneDisplayMode.LeftCompact
            : NavigationViewPaneDisplayMode.Left;
    }

    private void Sidebar_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (_state.ActiveSurfaceId is not { Length: > 0 } surfaceId
            || args.InvokedItemContainer?.Tag is not string { Length: > 0 } interactionId)
        {
            return;
        }
        DispatchSurfaceEvent(
            surfaceId,
            PresentationEvents.ActionActivated(surfaceId, interactionId));
    }

    /// <summary>
    /// Navigation now renders persistently in <see cref="Sidebar"/>
    /// (see D4 in the cross-frontend design review), so the scrim only
    /// ever needs to carry the action menu.
    /// </summary>
    private void RenderOverlay()
    {
        OverlayItems.Children.Clear();
        if (_state.ActiveOverlay is not { } presented
            || !presented.TryGetProperty("surface_id", out JsonElement surfaceIdValue)
            || surfaceIdValue.GetString() is not { Length: > 0 } surfaceId
            || !presented.TryGetProperty("overlay", out JsonElement overlay)
            || overlay.ValueKind != JsonValueKind.Object
            || !overlay.TryGetProperty("kind", out JsonElement overlayKind)
            || overlayKind.GetString() != "action_menu")
        {
            OverlayScrim.Visibility = Visibility.Collapsed;
            _overlaySurfaceId = "";
            _overlayKind = "";
            return;
        }

        _overlaySurfaceId = surfaceId;
        _overlayKind = overlay.TryGetProperty("kind", out JsonElement kind)
            ? kind.GetString() ?? ""
            : "";
        OverlayTitle.Text = overlay.TryGetProperty("title", out JsonElement title)
            ? title.GetString() ?? ""
            : "";
        AutomationProperties.SetName(OverlayPanel, OverlayTitle.Text);
        OverlayCloseButton.Content = new FontIcon { Glyph = "\uE8BB" };
        AutomationProperties.SetName(OverlayCloseButton, OverlayCloseLabel.For(overlay, Localizer.T));

        if (overlay.TryGetProperty("items", out JsonElement items)
            && items.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in items.EnumerateArray())
                OverlayItems.Children.Add(OverlayItem(item));
        }
        OverlayScrim.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Icon <em>and</em> label, never icon alone: the glyph is a recognition
    /// aid for a reader who skims, and dropping the word would trade one
    /// barrier for another.
    /// </summary>
    private Button OverlayItem(JsonElement action)
    {
        string? iconToken =
            action.TryGetProperty("icon_token", out JsonElement icon)
            && icon.ValueKind == JsonValueKind.String
                ? icon.GetString()
                : null;
        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
        };
        content.Children.Add(PresentationIcons.Element(iconToken));
        content.Children.Add(new TextBlock
        {
            Text = action.TryGetProperty("label", out JsonElement label)
                ? label.GetString() ?? ""
                : "",
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        });

        var button = new Button
        {
            Content = content,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            IsEnabled = !action.TryGetProperty("enabled", out JsonElement enabled)
                        || enabled.GetBoolean(),
            Tag = action.TryGetProperty("interaction_id", out JsonElement interaction)
                ? interaction.GetString() ?? ""
                : "",
        };
        AutomationProperties.SetName(
            button,
            action.TryGetProperty("accessibility_label", out JsonElement accessible)
                ? accessible.GetString() ?? ""
                : "");
        button.Click += (sender, _) => ActivateOverlayItem(sender as Button);
        return button;
    }

    private void ActivateOverlayItem(Button? button)
    {
        if (_overlaySurfaceId.Length == 0
            || button?.Tag is not string { Length: > 0 } interactionId)
        {
            return;
        }
        DispatchSurfaceEvent(
            _overlaySurfaceId,
            PresentationEvents.ActionActivated(_overlaySurfaceId, interactionId));
    }

    private void OverlayCloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_overlaySurfaceId.Length == 0)
            return;
        DispatchSurfaceEvent(
            _overlaySurfaceId,
            PresentationEvents.OverlayDismissed(_overlaySurfaceId, _overlayKind));
    }

    /// <summary>
    /// Keyboard equivalent of tapping a role's button in its surface's
    /// title row or bottom primary button (vauchi/private#534) — reads the
    /// role straight from the active bar rather than a named control, since
    /// each surface now builds its own buttons.
    /// </summary>
    private void ActivateRole(JsonElement? bar, string role)
    {
        if (_state.ActiveSurfaceId is not { Length: > 0 } surfaceId
            || bar is not { } value
            || !value.TryGetProperty(role, out JsonElement action)
            || action.ValueKind != JsonValueKind.Object
            || action.GetProperty("interaction_id").GetString() is not { Length: > 0 } interactionId)
            return;
        DispatchSurfaceEvent(
            surfaceId,
            PresentationEvents.ActionActivated(surfaceId, interactionId));
    }

    private void RegisterShortcuts(JsonElement? bar)
    {
        KeyboardAccelerators.Clear();
        AddShortcut(VirtualKey.Escape, VirtualKeyModifiers.None, RequestBack);
        AddShortcut(VirtualKey.K, VirtualKeyModifiers.Control, () => ActivateRole(bar, "navigation"));
        AddShortcut(VirtualKey.Enter, VirtualKeyModifiers.Control, () => ActivateRole(bar, "primary"));
        AddShortcut(VirtualKey.Down, VirtualKeyModifiers.Menu, () => ActivateRole(bar, "secondary"));
        AddShortcut(VirtualKey.F1, VirtualKeyModifiers.None, () => ActivateRole(bar, "info"));
        if (bar is { } value
            && value.TryGetProperty("primary", out JsonElement primary)
            && primary.ValueKind == JsonValueKind.Object
            && primary.TryGetProperty("shortcut", out JsonElement shortcut)
            && shortcut.GetString() == "undo")
        {
            AddShortcut(VirtualKey.Z, VirtualKeyModifiers.Control, () => ActivateRole(bar, "primary"));
        }
    }

    private void AddShortcut(VirtualKey key, VirtualKeyModifiers modifiers, Action action)
    {
        var accelerator = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
        accelerator.Invoked += (_, args) =>
        {
            action();
            args.Handled = true;
        };
        KeyboardAccelerators.Add(accelerator);
    }

    private string CurrentFocusId() =>
        XamlRoot is not null
        && FocusManager.GetFocusedElement(XamlRoot) is DependencyObject focused
            ? AutomationProperties.GetAutomationId(focused)
            : "";

    private void RestoreFocus(string automationId)
    {
        if (automationId.Length == 0)
            return;
        if (FindAutomationElement(this, automationId) is Control control)
            control.Focus(FocusState.Programmatic);
    }

    private static DependencyObject? FindAutomationElement(
        DependencyObject root,
        string automationId)
    {
        if (AutomationProperties.GetAutomationId(root) == automationId)
            return root;
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int index = 0; index < count; index++)
        {
            if (FindAutomationElement(
                    VisualTreeHelper.GetChild(root, index),
                    automationId) is { } found)
            {
                return found;
            }
        }
        return null;
    }

    private void ShowProtocolError(string message)
    {
        ProtocolErrorBar.Title = "Presentation protocol error";
        ProtocolErrorBar.Message = message;
        ProtocolErrorBar.IsOpen = true;
    }

    private static bool ReducedMotionRequested()
    {
        try
        {
            return !new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
        }
        catch
        {
            return false;
        }
    }
}
