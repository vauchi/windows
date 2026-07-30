// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Text.Json;
using Windows.System;

namespace Vauchi.CoreUI;

public sealed partial class PresentationHost
{
    private void ExecuteEffect(JsonElement command)
    {
        string name = PresentationState.CommandName(command);
        switch (name)
        {
            case "PresentOverlay":
                PresentOverlay(command);
                break;
            case "PerformNativeBack":
                NativeBackRequested?.Invoke();
                break;
            case "ShowToast":
                ShowToast(command.GetProperty("ShowToast").GetProperty("toast"));
                break;
            case "PresentAlert":
                ShowAlert(command.GetProperty("PresentAlert").GetProperty("alert"));
                break;
            case "OpenExternalUrl":
                OpenExternalUrl(command.GetProperty("OpenExternalUrl"));
                break;
            default:
                NativeEffectReady?.Invoke(command);
                break;
        }
    }

    private void PresentOverlay(JsonElement command)
    {
        JsonElement payload = command.GetProperty("PresentOverlay");
        string surfaceId = payload.GetProperty("surface_id").GetString() ?? "";
        JsonElement overlay = payload.GetProperty("overlay");
        string kind = overlay.GetProperty("kind").GetString() ?? "";
        if (kind == "navigation")
            PresentNavigationOverlay(surfaceId, overlay);
        else
            PresentActionOverlay(surfaceId, overlay);
    }

    private async void PresentNavigationOverlay(
        string surfaceId,
        JsonElement overlay)
    {
        bool activated = false;
        var items = new ListView { IsItemClickEnabled = true };
        foreach (JsonElement action in overlay.GetProperty("items").EnumerateArray())
        {
            string interactionId = action.GetProperty("interaction_id").GetString() ?? "";
            var item = new ListViewItem
            {
                Content = action.GetProperty("label").GetString() ?? "",
                Tag = interactionId,
                IsEnabled = !action.TryGetProperty("enabled", out JsonElement enabled)
                            || enabled.GetBoolean(),
                MinHeight = 44,
            };
            AutomationProperties.SetAutomationId(item, interactionId);
            AutomationProperties.SetName(
                item,
                action.GetProperty("accessibility_label").GetString() ?? "");
            items.Items.Add(item);
        }
        if (!_reducedMotion)
        {
            items.Transitions = new TransitionCollection
            {
                new PaneThemeTransition { Edge = EdgeTransitionLocation.Left },
            };
        }

        var dialog = new ContentDialog
        {
            Title = overlay.TryGetProperty("title", out JsonElement title)
                ? title.GetString() ?? ""
                : "",
            Content = items,
            CloseButtonText = "Close",
            XamlRoot = XamlRoot,
        };
        items.ItemClick += (_, args) =>
        {
            if (args.ClickedItem is not ListViewItem item
                || item.Tag is not string { Length: > 0 } interactionId)
                return;
            activated = true;
            dialog.Hide();
            DispatchSurfaceEvent(
                surfaceId,
                PresentationEvents.ActionActivated(surfaceId, interactionId));
        };
        dialog.Closed += (_, _) =>
        {
            if (!activated)
                Dispatch(PresentationEvents.OverlayDismissed(surfaceId, "navigation"));
        };
        await dialog.ShowAsync();
    }

    private void PresentActionOverlay(string surfaceId, JsonElement overlay)
    {
        bool activated = false;
        var menu = new MenuFlyout
        {
            AreOpenCloseAnimationsEnabled = !_reducedMotion,
        };
        foreach (JsonElement action in overlay.GetProperty("items").EnumerateArray())
        {
            string interactionId = action.GetProperty("interaction_id").GetString() ?? "";
            var item = new MenuFlyoutItem
            {
                Text = action.GetProperty("label").GetString() ?? "",
                IsEnabled = !action.TryGetProperty("enabled", out JsonElement enabled)
                            || enabled.GetBoolean(),
            };
            AutomationProperties.SetAutomationId(item, interactionId);
            AutomationProperties.SetName(
                item,
                action.GetProperty("accessibility_label").GetString() ?? "");
            item.Click += (_, _) =>
            {
                activated = true;
                DispatchSurfaceEvent(
                    surfaceId,
                    PresentationEvents.ActionActivated(surfaceId, interactionId));
            };
            menu.Items.Add(item);
        }
        menu.Closed += (_, _) =>
        {
            if (!activated)
                Dispatch(PresentationEvents.OverlayDismissed(surfaceId, "action_menu"));
        };
        menu.ShowAt(SecondaryButton);
    }

    private void ShowToast(JsonElement toast)
    {
        FeedbackBar.Message = toast.GetProperty("message").GetString() ?? "";
        FeedbackBar.IsOpen = true;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            FeedbackBar.IsOpen = false;
        };
        timer.Start();
    }

    private void ShowAlert(JsonElement alert)
    {
        AlertBar.Title = alert.GetProperty("title").GetString() ?? "";
        AlertBar.Message = alert.GetProperty("message").GetString() ?? "";
        AlertBar.IsOpen = true;
    }

    private static async void OpenExternalUrl(JsonElement payload)
    {
        string value = payload.GetProperty("url").GetString() ?? "";
        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
            await Launcher.LaunchUriAsync(uri);
    }
}
