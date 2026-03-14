// SPDX-FileCopyrightText: 2026 Mattia Egloff <mattia.egloff@pm.me>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Text.Json;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Vauchi.CoreUI.Components;

public sealed partial class ContactListComponent : UserControl, IRenderable
{
    public event EventHandler<string>? ActionRequested;

    private string _componentId = "";
    private bool _eventsWired;

    public ContactListComponent()
    {
        InitializeComponent();
    }

    public void Render(JsonElement data)
    {
        _componentId = data.TryGetProperty("id", out var id)
            ? id.GetString() ?? ""
            : "";

        var searchable = data.TryGetProperty("searchable", out var s) && s.GetBoolean();

        if (searchable)
        {
            SearchBox.Visibility = Visibility.Visible;
            if (!_eventsWired)
            {
                SearchBox.TextChanged += (_, args) =>
                {
                    if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                    {
                        ActionRequested?.Invoke(this,
                            JsonSerializer.Serialize(new
                            {
                                SearchChanged = new
                                {
                                    component_id = _componentId,
                                    query = SearchBox.Text
                                }
                            }));
                    }
                };
                _eventsWired = true;
            }
        }

        ContactListView.Items.Clear();
        ContactListView.SelectionChanged -= OnSelectionChanged;

        if (data.TryGetProperty("contacts", out var contacts)
            && contacts.ValueKind == JsonValueKind.Array)
        {
            foreach (var contact in contacts.EnumerateArray())
            {
                var contactId = contact.TryGetProperty("id", out var cid)
                    ? cid.GetString() ?? ""
                    : "";
                var name = contact.TryGetProperty("name", out var n)
                    ? n.GetString() ?? ""
                    : "";
                var hasSubtitle = contact.TryGetProperty("subtitle", out var sub)
                                  && sub.ValueKind != JsonValueKind.Null;
                var initials = contact.TryGetProperty("avatar_initials", out var ai)
                    ? ai.GetString() ?? ""
                    : "";

                var grid = new Grid
                {
                    Tag = contactId,
                    Padding = new Thickness(4),
                    ColumnSpacing = 12
                };
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(grid, name);
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Avatar circle
                var avatarBorder = new Border
                {
                    Width = 36,
                    Height = 36,
                    CornerRadius = new CornerRadius(18),
                    Background = new SolidColorBrush(Colors.CadetBlue)
                };
                var initialsBlock = new TextBlock
                {
                    Text = initials,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14
                };
                avatarBorder.Child = initialsBlock;
                Grid.SetColumn(avatarBorder, 0);
                grid.Children.Add(avatarBorder);

                // Name + subtitle
                var textPanel = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 0
                };
                textPanel.Children.Add(new TextBlock { Text = name });

                if (hasSubtitle)
                {
                    textPanel.Children.Add(new TextBlock
                    {
                        Text = sub.GetString() ?? "",
                        Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                        Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"]
                    });
                }

                Grid.SetColumn(textPanel, 1);
                grid.Children.Add(textPanel);

                ContactListView.Items.Add(new ListViewItem { Content = grid, Tag = contactId });
            }
        }

        ContactListView.SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ContactListView.SelectedItem is ListViewItem selectedItem
            && selectedItem.Tag is string contactId)
        {
            ActionRequested?.Invoke(this,
                JsonSerializer.Serialize(new
                {
                    ListItemSelected = new
                    {
                        component_id = _componentId,
                        item_id = contactId
                    }
                }));
        }
    }
}
