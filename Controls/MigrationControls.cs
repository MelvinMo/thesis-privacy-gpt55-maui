using SleepTrackerMaui.Models;
using SleepTrackerMaui.Services;
using Microsoft.Maui.Controls.Shapes;

namespace SleepTrackerMaui.Controls;

public static class Ui
{
    public static Label Text(string text, double size, Color? color = null, FontAttributes attributes = FontAttributes.None, TextAlignment align = TextAlignment.Start)
    {
        return new Label
        {
            Text = text,
            TextColor = color ?? Colors.White,
            FontFamily = "SpaceMono",
            FontSize = size,
            FontAttributes = attributes,
            HorizontalTextAlignment = align,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    public static Border Card(View content, double padding = 20, double radius = 16, Color? background = null)
    {
        return new Border
        {
            BackgroundColor = background ?? AppColors.LightBlack,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = radius },
            Padding = padding,
            Content = content
        };
    }

    public static Button BlueButton(string title, Func<Task> clicked)
    {
        Button button = new()
        {
            Text = title,
            FontFamily = "SpaceMono",
            FontAttributes = FontAttributes.Bold,
            FontSize = 16,
            TextColor = AppColors.LightBlack,
            BackgroundColor = AppColors.GeneralBlue,
            CornerRadius = 8,
            Padding = new Thickness(32, 16),
            HorizontalOptions = LayoutOptions.Fill
        };
        button.Clicked += async (_, _) => await RunGuardedAsync(clicked);
        return button;
    }

    public static Entry Input(string placeholder, bool password = false)
    {
        return new Entry
        {
            Placeholder = placeholder,
            PlaceholderColor = Color.FromArgb("#9CA3AF"),
            TextColor = Colors.White,
            FontFamily = "SpaceMono",
            FontSize = 16,
            IsPassword = password,
            BackgroundColor = AppColors.InputFieldBackground,
            Margin = new Thickness(0, 0, 0, 16)
        };
    }

    public static Image AssetImage(string file, Aspect aspect = Aspect.AspectFit)
    {
        return new Image
        {
            Source = ImageSource.FromFile(file),
            Aspect = aspect
        };
    }

    public static string NoteLabel(SleepNote note)
    {
        return note switch
        {
            SleepNote.WarmBath => "Warm Bath",
            SleepNote.HeavyMeal => "Heavy Meal",
            _ => note.ToString()
        };
    }

    public static async Task RunGuardedAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            // MIGRATION: React Native event handlers report promise failures
            //            through component state. MAUI async event exceptions
            //            can crash Android, so shared buttons surface them as
            //            a small alert instead.
            Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null)
            {
                await page.DisplayAlert("Action failed", ex.Message, "OK");
            }
        }
    }
}

public sealed class PrivacyTooltipView : Grid
{
    private readonly ImageButton _icon;
    private readonly double _iconSize;
    private readonly TransparencyEvent _event;
    private readonly string _dataType;

    public PrivacyTooltipView(TransparencyEvent transparencyEvent, string dataType, double iconSize = 40)
    {
        _event = transparencyEvent;
        _dataType = dataType;
        _iconSize = iconSize;
        // MIGRATION: The React Native tooltip is an overlay and must not take
        //            layout space while closed. Reserving tooltip width here
        //            squeezed page titles into one-letter columns on phones.
        WidthRequest = iconSize + 12;
        HeightRequest = iconSize + 12;
        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.Center;

        _icon = new ImageButton
        {
            Source = ImageSource.FromFile(GetIconFile(false)),
            WidthRequest = iconSize,
            HeightRequest = iconSize,
            Padding = 4,
            BackgroundColor = Colors.Transparent
        };
        AutomationProperties.SetName(_icon, $"Privacy {dataType}");
        SemanticProperties.SetDescription(_icon, $"Privacy {dataType}");
        _icon.Clicked += async (_, _) => await Ui.RunGuardedAsync(ShowTooltipAsync);

        // MIGRATION: React Native measured pageY before selecting top/bottom
        //            tooltip placement. MAUI does the same by walking visual
        //            parents, but renders the tooltip in a screen-level overlay
        //            so page headers and cards never get squeezed or clipped.
        Children.Add(_icon);
    }

    private async Task ShowTooltipAsync()
    {
        _icon.Source = ImageSource.FromFile(GetIconFile(true));
        try
        {
            double screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
            double screenHeight = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;
            Point pagePoint = CalculatePagePoint(_icon);
            await Shell.Current.Navigation.PushModalAsync(
                new PrivacyTooltipOverlayPage(
                    _event,
                    _dataType,
                    pagePoint.X,
                    pagePoint.Y,
                    _iconSize,
                    screenWidth,
                    screenHeight),
                animated: false);
        }
        finally
        {
            _icon.Source = ImageSource.FromFile(GetIconFile(false));
        }
    }

    private static Point CalculatePagePoint(VisualElement element)
    {
        double x = element.X;
        double y = element.Y;
        Element? parent = element.Parent;
        while (parent is VisualElement visual)
        {
            x += visual.X;
            y += visual.Y;
            if (visual is ScrollView scrollView)
            {
                // MIGRATION: React Native measure() returns pageX/pageY after
                //            scroll offset has already been applied. MAUI
                //            visual coordinates are layout-relative, so scrolled
                //            content must subtract ScrollX/ScrollY to keep the
                //            tooltip anchored to the visible icon position.
                x -= scrollView.ScrollX;
                y -= scrollView.ScrollY;
            }
            parent = visual.Parent;
        }
        return new Point(x, y);
    }

    internal static View BuildTooltipContent(TransparencyEvent transparencyEvent, string dataType, Func<Task> close, double maxContentHeight = 500)
    {
        string title = transparencyEvent.PrivacyRisk == PrivacyRisk.LOW
            ? "No Privacy Violations Detected"
            : $"{RiskLabel(transparencyEvent.PrivacyRisk)} Privacy Risk";

        VerticalStackLayout stack = new() { Spacing = 10 };
        stack.Children.Add(Ui.Text(title, 13, Colors.Black, FontAttributes.Bold));
        if (transparencyEvent.PrivacyRisk != PrivacyRisk.LOW && !string.IsNullOrWhiteSpace(transparencyEvent.RegulatoryCompliance.Issues))
        {
            stack.Children.Add(Ui.Text(transparencyEvent.RegulatoryCompliance.Issues, 12, Colors.Black));
        }
        stack.Children.Add(Ui.Text("Purpose:", 13, Colors.Black, FontAttributes.Bold));
        stack.Children.Add(Ui.Text(transparencyEvent.AiExplanation.Why, 12, Colors.Black));
        stack.Children.Add(Ui.Text("Storage:", 13, Colors.Black, FontAttributes.Bold));
        stack.Children.Add(Ui.Text(string.IsNullOrWhiteSpace(transparencyEvent.AiExplanation.Storage) ? StorageText(dataType) : transparencyEvent.AiExplanation.Storage, 12, Colors.Black));
        stack.Children.Add(Ui.Text("Access:", 13, Colors.Black, FontAttributes.Bold));
        stack.Children.Add(Ui.Text(string.IsNullOrWhiteSpace(transparencyEvent.AiExplanation.Access) ? "Only you can view this data in the app unless you opt into cloud storage." : transparencyEvent.AiExplanation.Access, 12, Colors.Black));

        VerticalStackLayout root = new() { Spacing = 10 };
        root.Children.Add(new ScrollView
        {
            // MIGRATION: React Native's walkthrough tooltip caps content height
            //            and lets long privacy copy scroll internally. Keeping
            //            the Close affordance outside the scroll body prevents
            //            lower-page tooltips from looking cut off.
            MaximumHeightRequest = Math.Max(120, maxContentHeight - 44),
            Content = stack
        });

        Label closeLabel = Ui.Text("Close", 13, Colors.Black, FontAttributes.Bold, TextAlignment.End);
        TapGestureRecognizer closeTap = new();
        closeTap.Tapped += async (_, _) => await close();
        closeLabel.GestureRecognizers.Add(closeTap);
        root.Children.Add(closeLabel);
        return root;
    }

    private string GetIconFile(bool open)
    {
        string risk = _event.PrivacyRisk switch
        {
            PrivacyRisk.HIGH => "privacy_high",
            PrivacyRisk.MEDIUM => "privacy_medium",
            _ => "privacy_low"
        };
        return open ? $"{risk}_open.png" : $"{risk}.png";
    }

    private static string RiskLabel(PrivacyRisk risk) => risk switch
    {
        PrivacyRisk.HIGH => "Major",
        PrivacyRisk.MEDIUM => "Medium",
        _ => "Low"
    };

    internal static Color RiskColor(PrivacyRisk risk) => risk switch
    {
        PrivacyRisk.HIGH => AppColors.TooltipRed,
        PrivacyRisk.MEDIUM => AppColors.TooltipYellow,
        _ => AppColors.TooltipGreen
    };

    private static string StorageText(string dataType) => dataType switch
    {
        "Journal" => "Stored locally in SQLite unless cloud storage is enabled.",
        "Statistics" => "Derived from sleep and journal data for on-device display.",
        "Activity Tracker" => "Stored locally in SQLite for movement tracking.",
        _ => "Stored according to your consent preferences."
    };
}

internal sealed class PrivacyTooltipOverlayPage : ContentPage
{
    private const double TooltipWidth = 300;
    private const double TooltipFallbackHeight = 360;

    public PrivacyTooltipOverlayPage(
        TransparencyEvent transparencyEvent,
        string dataType,
        double iconX,
        double iconY,
        double iconSize,
        double screenWidth,
        double screenHeight)
    {
        // MIGRATION: This modal is intentionally transparent and full-screen so
        //            the tooltip can be positioned like React Native absolute
        //            overlay UI without disturbing the page layout underneath.
        BackgroundColor = Color.FromRgba(0, 0, 0, 1);
        Padding = 0;

        AbsoluteLayout root = new();
        BoxView outside = new()
        {
            BackgroundColor = Colors.Transparent
        };
        TapGestureRecognizer outsideTap = new();
        outsideTap.Tapped += async (_, _) => await Navigation.PopModalAsync(animated: false);
        outside.GestureRecognizers.Add(outsideTap);
        AbsoluteLayout.SetLayoutBounds(outside, new Rect(0, 0, 1, 1));
        AbsoluteLayout.SetLayoutFlags(outside, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);
        root.Children.Add(outside);

        double safeScreenHeight = Math.Max(220, screenHeight - 56);
        double visibleIconY = Math.Clamp(iconY, 0, Math.Max(0, safeScreenHeight - iconSize));
        bool showAbove = iconY > screenHeight / 2;
        double availableHeight = showAbove
            ? visibleIconY - 24 - 8
            : safeScreenHeight - (visibleIconY + iconSize + 8) - 24;
        double maxTooltipHeight = Math.Clamp(availableHeight, 160, 500);

        Border tooltip = new()
        {
            WidthRequest = TooltipWidth,
            MaximumHeightRequest = maxTooltipHeight,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = PrivacyTooltipView.RiskColor(transparencyEvent.PrivacyRisk),
            Padding = 16,
            Content = PrivacyTooltipView.BuildTooltipContent(
                transparencyEvent,
                dataType,
                async () => await Navigation.PopModalAsync(animated: false),
                Math.Max(120, maxTooltipHeight - 32))
        };

        double x = Math.Clamp(iconX + iconSize - TooltipWidth, 12, Math.Max(12, screenWidth - TooltipWidth - 12));
        void PlaceTooltip(double measuredHeight)
        {
            double tooltipHeight = measuredHeight > 0 ? measuredHeight : TooltipFallbackHeight;
            // MIGRATION: Smart positioning mirrors the source rule exactly:
            //            if pageY is in the lower half, render above the icon;
            //            otherwise render below it. MAUI content height is only
            //            known after layout, so the final placement uses the
            //            measured height rather than a fixed estimate.
            double y = showAbove
                ? Math.Max(24, visibleIconY - tooltipHeight - 8)
                : Math.Min(safeScreenHeight - tooltipHeight - 24, visibleIconY + iconSize + 8);

            AbsoluteLayout.SetLayoutBounds(tooltip, new Rect(x, y, TooltipWidth, AbsoluteLayout.AutoSize));
        }

        PlaceTooltip(TooltipFallbackHeight);
        tooltip.SizeChanged += (_, _) => PlaceTooltip(tooltip.Height);
        root.Children.Add(tooltip);
        Content = root;
    }
}

public sealed class WeekCalendarView : VerticalStackLayout
{
    private readonly Func<DateTime> _getSelectedDate;
    private readonly Func<DateTime, Task> _setSelectedDate;

    public WeekCalendarView(Func<DateTime> getSelectedDate, Func<DateTime, Task> setSelectedDate)
    {
        _getSelectedDate = getSelectedDate;
        _setSelectedDate = setSelectedDate;
        Spacing = 12;
        Padding = 16;
        Render();
    }

    public void Render()
    {
        Children.Clear();
        DateTime selected = _getSelectedDate().Date;
        DateTime sunday = selected.AddDays(-(int)selected.DayOfWeek);

        Grid weekHeader = SevenColumnGrid();
        string[] labels = ["S", "M", "T", "W", "T", "F", "S"];
        for (int i = 0; i < labels.Length; i++)
        {
            weekHeader.Add(Ui.Text(labels[i], 14, Colors.White.WithAlpha(0.7f), FontAttributes.Bold, TextAlignment.Center), i, 0);
        }
        Children.Add(weekHeader);

        Grid days = SevenColumnGrid();
        for (int i = 0; i < 7; i++)
        {
            DateTime day = sunday.AddDays(i);
            Button dayButton = new()
            {
                Text = day.Day.ToString(),
                FontFamily = "SpaceMono",
                FontSize = 16,
                FontAttributes = day == selected ? FontAttributes.Bold : FontAttributes.None,
                TextColor = day == selected ? Color.FromArgb("#001122") : Colors.White,
                BackgroundColor = day == selected ? Colors.White : Colors.Transparent,
                CornerRadius = 18,
                WidthRequest = 35,
                HeightRequest = 35,
                Padding = 0
            };
            dayButton.Clicked += async (_, _) => await Ui.RunGuardedAsync(async () =>
            {
                await _setSelectedDate(day);
                Render();
            });
            days.Add(dayButton, i, 0);
        }
        Children.Add(days);

        HorizontalStackLayout moveWeek = new()
        {
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 20
        };
        moveWeek.Children.Add(LinkButton("< Previous", async () =>
        {
            await _setSelectedDate(selected.AddDays(-7));
            Render();
        }));
        moveWeek.Children.Add(LinkButton("Next >", async () =>
        {
            await _setSelectedDate(selected.AddDays(7));
            Render();
        }));
        Children.Add(moveWeek);
    }

    private static Grid SevenColumnGrid()
    {
        Grid grid = new();
        for (int i = 0; i < 7; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }
        return grid;
    }

    private static Button LinkButton(string text, Func<Task> clicked)
    {
        Button button = new()
        {
            Text = text,
            FontFamily = "SpaceMono",
            FontSize = 12,
            TextColor = AppColors.GeneralBlue,
            BackgroundColor = Colors.Transparent,
            Padding = 0
        };
        button.Clicked += async (_, _) => await Ui.RunGuardedAsync(clicked);
        return button;
    }
}
