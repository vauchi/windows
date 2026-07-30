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
using Windows.System;

namespace Vauchi.CoreUI;

public sealed partial class PresentationHost : UserControl
{
    private readonly PresentationState _state = new();
    private IntPtr _appHandle;
    private bool _reducedMotion;
    private int _reportedWidth = -1;
    private int _reportedHeight = -1;

    public event Action<JsonElement>? NativeEffectReady;
    public event Action? NativeBackRequested;

    public PresentationHost()
    {
        InitializeComponent();
        _reducedMotion = ReducedMotionRequested();
        SizeChanged += (_, args) =>
            ReportEnvironment((int)args.NewSize.Width, (int)args.NewSize.Height);
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
        Dispatch(PresentationEvents.EnvironmentChanged(width, height, _reducedMotion));
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

        for (int index = 0; index < visible.Count; index++)
        {
            string surfaceId = visible[index];
            if (_state.Surface(surfaceId) is not { } surface)
                continue;
            var renderer = new PresentationSurface(surface);
            renderer.EventReady += DispatchSurfaceEvent;
            var border = new Border
            {
                Child = renderer,
                BorderThickness = surfaceId == _state.ActiveSurfaceId
                    ? new Thickness(2)
                    : new Thickness(0),
                BorderBrush = new SolidColorBrush(ThemeColors.ActiveSurfaceBorder),
            };
            Grid.SetColumn(border, index);
            SurfaceGrid.Children.Add(border);
        }
        RenderContextBar();
    }

    private void RenderContextBar()
    {
        JsonElement? bar = _state.ActiveContextBar;
        ConfigureRole(BackButton, bar, "back");
        ConfigureRole(NavigationButton, bar, "navigation");
        ConfigureRole(PrimaryButton, bar, "primary");
        ConfigureRole(SecondaryButton, bar, "secondary");
        CommandStrip.Visibility =
            BackButton.Visibility == Visibility.Collapsed
            && NavigationButton.Visibility == Visibility.Collapsed
            && PrimaryButton.Visibility == Visibility.Collapsed
            && SecondaryButton.Visibility == Visibility.Collapsed
                ? Visibility.Collapsed
                : Visibility.Visible;
        RegisterShortcuts(bar);
    }

    private static void ConfigureRole(Button button, JsonElement? bar, string role)
    {
        if (bar is not { } value
            || !value.TryGetProperty(role, out JsonElement action)
            || action.ValueKind != JsonValueKind.Object)
        {
            button.Visibility = Visibility.Collapsed;
            button.Tag = null;
            return;
        }
        button.Visibility = Visibility.Visible;
        button.Content = action.GetProperty("label").GetString() ?? "";
        button.IsEnabled = !action.TryGetProperty("enabled", out JsonElement enabled)
                           || enabled.GetBoolean();
        button.Tag = action.GetProperty("interaction_id").GetString() ?? "";
        string accessible = action.TryGetProperty(
            "accessibility_label",
            out JsonElement accessibility)
            ? accessibility.GetString() ?? ""
            : "";
        AutomationProperties.SetName(button, accessible);
    }

    private void ActivateRole(Button button)
    {
        if (_state.ActiveSurfaceId is not { Length: > 0 } surfaceId
            || button.Tag is not string { Length: > 0 } interactionId)
            return;
        DispatchSurfaceEvent(
            surfaceId,
            PresentationEvents.ActionActivated(surfaceId, interactionId));
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) =>
        ActivateRole(BackButton);

    private void NavigationButton_Click(object sender, RoutedEventArgs e) =>
        ActivateRole(NavigationButton);

    private void PrimaryButton_Click(object sender, RoutedEventArgs e) =>
        ActivateRole(PrimaryButton);

    private void SecondaryButton_Click(object sender, RoutedEventArgs e) =>
        ActivateRole(SecondaryButton);

    private void RegisterShortcuts(JsonElement? bar)
    {
        KeyboardAccelerators.Clear();
        AddShortcut(VirtualKey.Escape, VirtualKeyModifiers.None, RequestBack);
        AddShortcut(VirtualKey.K, VirtualKeyModifiers.Control, () => ActivateRole(NavigationButton));
        AddShortcut(VirtualKey.Enter, VirtualKeyModifiers.Control, () => ActivateRole(PrimaryButton));
        AddShortcut(VirtualKey.Down, VirtualKeyModifiers.Menu, () => ActivateRole(SecondaryButton));
        if (bar is { } value
            && value.TryGetProperty("primary", out JsonElement primary)
            && primary.ValueKind == JsonValueKind.Object
            && primary.TryGetProperty("shortcut", out JsonElement shortcut)
            && shortcut.GetString() == "undo")
        {
            AddShortcut(VirtualKey.Z, VirtualKeyModifiers.Control, () => ActivateRole(PrimaryButton));
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
