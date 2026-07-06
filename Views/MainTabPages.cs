using SleepTrackerMaui.Controls;
using SleepTrackerMaui.Models;
using SleepTrackerMaui.Services;
using SleepTrackerMaui.Stores;
using SleepTrackerMaui.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace SleepTrackerMaui.Views;

public sealed class SleepPage : ContentPage
{
    private readonly SleepViewModel _viewModel;
    private readonly TransparencyStore _transparency;

    public SleepPage(SleepViewModel viewModel, TransparencyStore transparency)
    {
        _viewModel = viewModel;
        _transparency = transparency;
        BackgroundColor = AppColors.AppBackground;
        Shell.SetNavBarIsVisible(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _transparency.LoadAsync();
        await _viewModel.LoadAsync();
        Render();
    }

    private void Render()
    {
        // MIGRATION: Recreate the visual tree for each render. React Native
        //            can diff virtual nodes; MAUI native views cannot be
        //            reused after they are attached to a parent.
        VerticalStackLayout stack = new()
        {
            Padding = new Thickness(20, 50, 20, 30),
            Spacing = 15
        };

        Grid header = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        header.Add(Ui.Text("Sleep Tracker", 30, Colors.White, FontAttributes.Bold, TextAlignment.Center), 0, 0);
        header.Add(new PrivacyTooltipView(_transparency.Journal, "Journal", 50), 1, 0);
        stack.Children.Add(header);

        stack.Children.Add(new Grid
        {
            HeightRequest = 330,
            Children =
            {
                Ui.AssetImage("sleep_duration_wheel.png")
            }
        });

        stack.Children.Add(TimeCard("Bedtime", string.IsNullOrWhiteSpace(_viewModel.Bedtime) ? "Set Time" : _viewModel.Bedtime, async () =>
        {
            string? value = await DisplayPromptAsync("Set Bedtime", "Enter bedtime", initialValue: _viewModel.Bedtime, placeholder: "10:14 PM");
            if (!string.IsNullOrWhiteSpace(value))
            {
                await _viewModel.SaveBedtimeAsync(value);
                Render();
            }
        }));

        stack.Children.Add(TimeCard("Alarm", string.IsNullOrWhiteSpace(_viewModel.Alarm) ? "Set Time" : _viewModel.Alarm, async () =>
        {
            string? value = await DisplayPromptAsync("Set Alarm", "Enter alarm time", initialValue: _viewModel.Alarm, placeholder: "6:44 AM");
            if (!string.IsNullOrWhiteSpace(value))
            {
                await _viewModel.SaveAlarmAsync(value);
                Render();
            }
        }));

        Button sleepNow = Ui.BlueButton("SLEEP NOW", async () =>
        {
            if (string.IsNullOrWhiteSpace(_viewModel.Bedtime) || string.IsNullOrWhiteSpace(_viewModel.Alarm))
            {
                await DisplayAlert("Missing Information", "Please set your Bedtime and Alarm before starting sleep mode.", "OK");
                return;
            }
            await Shell.Current.GoToAsync(nameof(SleepModePage));
        });
        sleepNow.CornerRadius = 12;
        sleepNow.Padding = new Thickness(0, 18);
        sleepNow.TextColor = Colors.White;
        stack.Children.Add(sleepNow);

        Content = new ScrollView { Content = stack };
    }

    private static View TimeCard(string label, string value, Func<Task> edit)
    {
        Grid row = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        row.Add(Ui.Text(label, 18, Colors.White, FontAttributes.Bold), 0, 0);
        row.Add(Ui.Text(value, 18, Colors.White.WithAlpha(0.8f)), 1, 0);
        Button pencil = new()
        {
            Text = "Edit",
            FontFamily = "SpaceMono",
            FontSize = 13,
            TextColor = Colors.White,
            BackgroundColor = Colors.Transparent,
            Padding = new Thickness(8, 0)
        };
        pencil.Clicked += async (_, _) => await Ui.RunGuardedAsync(edit);
        row.Add(pencil, 2, 0);
        return Ui.Card(row, padding: 15, radius: 12);
    }
}

public sealed class SleepModePage : ContentPage
{
    private readonly SleepViewModel _sleep;
    private readonly ProfileViewModel _profile;
    private readonly TransparencyStore _transparency;
    private Label? _time;
    private CancellationTokenSource? _wakeHold;

    public SleepModePage(SleepViewModel sleep, ProfileViewModel profile, TransparencyStore transparency)
    {
        _sleep = sleep;
        _profile = profile;
        _transparency = transparency;
        BackgroundColor = Colors.Black;
        Shell.SetNavBarIsVisible(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _sleep.LoadAsync();
        await _profile.LoadAsync();
        await _transparency.SetMicrophoneAsync(_transparency.Microphone with
        {
            BackgroundMode = _profile.Preferences.MicrophoneEnabled,
            SamplingRate = _profile.Preferences.MicrophoneEnabled ? 5 : null,
            StorageLocation = _profile.Preferences.CloudStorageEnabled ? DataDestination.GOOGLE_CLOUD : DataDestination.SQLITE_DB
        });
        await _transparency.SetLightAsync(_transparency.Light with
        {
            BackgroundMode = _profile.Preferences.LightSensorEnabled,
            SamplingRate = _profile.Preferences.LightSensorEnabled ? 10 : null,
            StorageLocation = _profile.Preferences.CloudStorageEnabled ? DataDestination.GOOGLE_CLOUD : DataDestination.SQLITE_DB
        });
        Build();
        Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
        {
            if (_time is not null)
            {
                _time.Text = DateTime.Now.ToString("hh:mm tt");
            }
            return Shell.Current.CurrentPage == this;
        });
    }

    private void Build()
    {
        Grid root = new();
        root.Children.Add(Ui.AssetImage("sleep_mode_bg.png", Aspect.AspectFill));
        root.Children.Add(new BoxView { BackgroundColor = Color.FromArgb("#33000000") });

        VerticalStackLayout stack = new()
        {
            Padding = new Thickness(20, 50, 20, 20),
            Spacing = 20,
            VerticalOptions = LayoutOptions.Fill
        };

        HorizontalStackLayout icons = new()
        {
            Spacing = 20,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new PrivacyTooltipView(_transparency.Accelerometer, "Activity Tracker", 40),
                new PrivacyTooltipView(_transparency.Light, "Light Sensor", 40),
                new PrivacyTooltipView(_transparency.Microphone, "Microphone", 40)
            }
        };
        stack.Children.Add(icons);
        _time = Ui.Text(DateTime.Now.ToString("hh:mm tt"), 60, Colors.White, FontAttributes.Bold, TextAlignment.Center);
        stack.Children.Add(new Grid { HeightRequest = 250, Children = { _time } });

        Grid alarm = new()
        {
            Padding = new Thickness(20, 15),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        alarm.Add(Ui.Text("Alarm", 16, Colors.White.WithAlpha(0.8f)), 0, 0);
        alarm.Add(Ui.Text(_sleep.Alarm, 16, Colors.White, FontAttributes.Bold), 1, 0);
        stack.Children.Add(Ui.Card(alarm, padding: 0, radius: 12, background: Color.FromArgb("#80000000")));

        Button wake = new()
        {
            Text = "Wake up",
            FontFamily = "SpaceMono",
            FontAttributes = FontAttributes.Bold,
            FontSize = 20,
            TextColor = Colors.White,
            BackgroundColor = AppColors.GeneralBlue,
            CornerRadius = 12,
            Padding = new Thickness(0, 20),
            HorizontalOptions = LayoutOptions.Fill
        };
        wake.Pressed += (_, _) => StartWakeHold(wake);
        wake.Released += (_, _) => CancelWakeHold(wake);
        stack.Children.Add(wake);

        root.Children.Add(stack);
        Content = root;
    }

    private async void StartWakeHold(Button wake)
    {
        _wakeHold = new CancellationTokenSource();
        wake.Text = "Hold...";
        try
        {
            // MIGRATION: RN used onPressIn/onPressOut with a two-second hold.
            //            MAUI Button.Pressed/Released preserves the same wake
            //            intent and avoids accidental exits.
            await Task.Delay(TimeSpan.FromSeconds(2), _wakeHold.Token);
            await WakeAsync();
        }
        catch (TaskCanceledException)
        {
            wake.Text = "Wake up";
        }
    }

    private void CancelWakeHold(Button wake)
    {
        _wakeHold?.Cancel();
        wake.Text = "Wake up";
    }

    private async Task WakeAsync()
    {
        await _transparency.SetMicrophoneAsync(_transparency.Microphone with { BackgroundMode = false });
        await _transparency.SetLightAsync(_transparency.Light with { BackgroundMode = false });
        await Shell.Current.GoToAsync("//Statistics");
    }
}

public sealed class JournalPage : ContentPage
{
    private readonly JournalViewModel _viewModel;
    private readonly TransparencyStore _transparency;

    public JournalPage(JournalViewModel viewModel, TransparencyStore transparency)
    {
        _viewModel = viewModel;
        _transparency = transparency;
        BackgroundColor = AppColors.AppBackground;
        Shell.SetNavBarIsVisible(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _transparency.LoadAsync();
        await _viewModel.LoadAsync(_viewModel.SelectedDate);
        Render();
    }

    private void Render()
    {
        Grid page = new()
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };
        // MIGRATION: React Native ScrollView gets a constrained viewport from
        //            flex layout. A MAUI ScrollView inside VerticalStackLayout
        //            measures to its full content height, which disables real
        //            scrolling on Android. The Grid star row gives the list a
        //            finite viewport so swipes work.
        page.Add(CalendarHeader("Today", DateTime.Parse(_viewModel.SelectedDate), async date =>
        {
            await _viewModel.LoadAsync(date.ToString("yyyy-MM-dd"));
            Render();
        }, showTabs: false), 0, 0);

        VerticalStackLayout content = new()
        {
            // MIGRATION: RN tab screens keep ScrollView content visually clear
            //            of the tab bar. Extra bottom padding gives MAUI the
            //            same reachable end-of-list space on Android.
            Padding = new Thickness(20, 0, 20, 90),
            Spacing = 15
        };
        content.Children.Add(Ui.Text("Sleep Goal", 20, Colors.White, FontAttributes.Bold));
        content.Children.Add(SleepGoalCard());

        Grid diaryTitle = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        diaryTitle.Add(Ui.Text("Diary", 20, Colors.White, FontAttributes.Bold), 0, 0);
        diaryTitle.Add(new PrivacyTooltipView(_transparency.Journal, "Journal", 40), 1, 0);
        content.Children.Add(diaryTitle);
        content.Children.Add(SleepNotesCard());
        content.Children.Add(JournalEntryCard());

        Grid activityTitle = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        activityTitle.Add(Ui.Text("Activity Tracker", 20, Colors.White, FontAttributes.Bold), 0, 0);
        activityTitle.Add(new PrivacyTooltipView(_transparency.Accelerometer, "Activity Tracker", 40), 1, 0);
        content.Children.Add(activityTitle);
        content.Children.Add(ActivityCard());
        page.Add(new ScrollView
        {
            Content = content,
            VerticalOptions = LayoutOptions.Fill
        }, 0, 1);
        Content = page;
    }

    private View SleepGoalCard()
    {
        Grid row = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        VerticalStackLayout left = new() { Spacing = 15 };
        left.Children.Add(Ui.Text("Moon  Bedtime", 14, Colors.White.WithAlpha(0.7f)));
        left.Children.Add(Ui.Text(string.IsNullOrWhiteSpace(_viewModel.Bedtime) ? "--" : _viewModel.Bedtime, 18, Colors.White, FontAttributes.Bold));
        left.Children.Add(Ui.Text("Alarm  Alarm", 14, Colors.White.WithAlpha(0.7f)));
        left.Children.Add(Ui.Text(string.IsNullOrWhiteSpace(_viewModel.Alarm) ? "--" : _viewModel.Alarm, 16, Colors.White, FontAttributes.Bold));
        row.Add(left, 0, 0);
        VerticalStackLayout goal = new() { HorizontalOptions = LayoutOptions.End, Spacing = 5 };
        goal.Children.Add(Ui.Text("Compass  Goal", 14, Colors.White.WithAlpha(0.7f), align: TextAlignment.End));
        goal.Children.Add(Ui.Text(string.IsNullOrWhiteSpace(_viewModel.SleepDuration) ? _viewModel.SleepGoal : _viewModel.SleepDuration, 18, Colors.White, FontAttributes.Bold, TextAlignment.End));
        row.Add(goal, 1, 0);
        return Ui.Card(row);
    }

    private View SleepNotesCard()
    {
        VerticalStackLayout stack = new() { Spacing = 10 };
        Grid header = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        header.Add(Ui.Text("Sleep Notes", 18, Colors.White, FontAttributes.Bold), 0, 0);
        Button add = new()
        {
            Text = "+",
            FontSize = 24,
            TextColor = AppColors.GeneralBlue,
            BackgroundColor = Colors.Transparent,
            Padding = 0
        };
        add.Clicked += async (_, _) => await Ui.RunGuardedAsync(ChooseSleepNoteAsync);
        header.Add(add, 1, 0);
        stack.Children.Add(header);
        if (_viewModel.SleepNotes.Count == 0)
        {
            stack.Children.Add(Ui.Text("No sleep notes added yet.", 16, AppColors.LightGrey, FontAttributes.Italic, TextAlignment.Center));
        }
        foreach (SleepNote note in _viewModel.SleepNotes)
        {
            stack.Children.Add(Ui.Text($"• {Ui.NoteLabel(note)}", 16));
        }
        return Ui.Card(stack);
    }

    private View JournalEntryCard()
    {
        Grid row = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(6, GridUnitType.Star)),
                new ColumnDefinition(GridLength.Star)
            }
        };
        row.Add(Ui.Text(string.IsNullOrWhiteSpace(_viewModel.DiaryEntry) ? "Write something to record your day... " : _viewModel.DiaryEntry, 16, Colors.White.WithAlpha(0.8f)), 0, 0);
        Button edit = new()
        {
            Text = "Edit",
            TextColor = Colors.White,
            BackgroundColor = Colors.Transparent,
            FontFamily = "SpaceMono"
        };
        edit.Clicked += async (_, _) => await Ui.RunGuardedAsync(async () =>
        {
            string? diary = await DisplayPromptAsync("Diary", "Write something to record your day", initialValue: _viewModel.DiaryEntry, maxLength: 500);
            if (diary is not null)
            {
                await _viewModel.SaveDiaryAsync(diary);
                Render();
            }
        });
        row.Add(edit, 1, 0);
        return Ui.Card(row);
    }

    private View ActivityCard()
    {
        Grid row = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };
        row.Add(ActivityItem("Steps", "83", "steps"), 0, 0);
        row.Add(ActivityItem("Calories", "83", "kcal"), 1, 0);
        return Ui.Card(row);
    }

    private static View ActivityItem(string label, string value, string unit)
    {
        return new VerticalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                Ui.Text(label, 16, Colors.White, FontAttributes.Bold, TextAlignment.Center),
                Ui.Card(new VerticalStackLayout
                {
                    Children =
                    {
                        Ui.Text(value, 20, Colors.White, FontAttributes.Bold, TextAlignment.Center),
                        Ui.Text(unit, 12, Colors.White.WithAlpha(0.7f), align: TextAlignment.Center)
                    }
                }, padding: 10, radius: 40, background: Color.FromArgb("#1AFFFFFF"))
            }
        };
    }

    private async Task ChooseSleepNoteAsync()
    {
        string cancel = "Cancel";
        string? choice = await DisplayActionSheet("Sleep Notes", cancel, null, Enum.GetValues<SleepNote>().Select(Ui.NoteLabel).ToArray());
        if (choice is null || choice == cancel)
        {
            return;
        }

        SleepNote note = Enum.GetValues<SleepNote>().First(item => Ui.NoteLabel(item) == choice);
        await _viewModel.ToggleNoteAsync(note);
        Render();
    }

    private View CalendarHeader(string title, DateTime date, Func<DateTime, Task> dateSelected, bool showTabs)
    {
        Border frame = new()
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Margin = new Thickness(0, 0, 0, 20),
            Content = new Grid
            {
                Children =
                {
                    Ui.AssetImage("journal_bg.png", Aspect.AspectFill),
                    new BoxView { BackgroundColor = Color.FromArgb("#CC001428") },
                    new VerticalStackLayout
                    {
                        Padding = new Thickness(30, 50, 30, 20),
                        Spacing = 10,
                        Children =
                        {
                            Ui.Text(title, 32, Colors.White, FontAttributes.Bold),
                            Ui.Text(date.ToString("MMMM dd"), 18, Colors.White.WithAlpha(0.8f)),
                            new WeekCalendarView(() => DateTime.Parse(_viewModel.SelectedDate), dateSelected)
                        }
                    }
                }
            }
        };
        return frame;
    }
}

public sealed class StatisticsPage : ContentPage
{
    private readonly StatisticsViewModel _viewModel;
    private readonly TransparencyStore _transparency;

    public StatisticsPage(StatisticsViewModel viewModel, TransparencyStore transparency)
    {
        _viewModel = viewModel;
        _transparency = transparency;
        BackgroundColor = AppColors.AppBackground;
        Shell.SetNavBarIsVisible(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _transparency.LoadAsync();
        Render();
    }

    private void Render()
    {
        Grid page = new()
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };
        // MIGRATION: Keeping the statistics header outside the scroll region
        //            matches the source layout, but MAUI needs the scrollable
        //            body constrained by a star row to make Android swipes
        //            functional.
        page.Add(Header(), 0, 0);
        VerticalStackLayout content = new()
        {
            // MIGRATION: RN ScrollView content is not hidden by the tab bar at
            //            the bottom. MAUI Shell tabs need explicit bottom
            //            padding so Sleep Clips can scroll fully into view.
            Padding = new Thickness(20, 0, 20, 90),
            Spacing = 16
        };
        if (!_viewModel.NormalUi)
        {
            content.Children.Add(PrivacyStatisticsContent());
        }
        else if (_viewModel.Daily)
        {
            DailyStatisticsContent(content);
        }
        else
        {
            content.Children.Add(StatisticItem("Sleep Quality", "sleep_quality_graph.png"));
            content.Children.Add(StatisticItem("Sleep Duration", "sleep_duration_graph.png"));
            content.Children.Add(StatisticItem("Sleep Stages", "sleep_duration_graph.png"));
            content.Children.Add(StatisticItem("Snore Time", "sleep_quality_graph.png"));
        }

        page.Add(new ScrollView
        {
            Content = content,
            VerticalOptions = LayoutOptions.Fill
        }, 0, 1);
        Content = page;
    }

    private View Header()
    {
        Grid root = new()
        {
            HeightRequest = _viewModel.Daily ? 340 : 145,
            Margin = new Thickness(0, 0, 0, 20)
        };
        // MIGRATION: The React Native ImageBackground only grows to fit the
        //            visible header controls. When the Statistics tab hides
        //            the calendar, MAUI's unconstrained Image measured too
        //            tall and left a dead empty area, so the header height is
        //            explicit for the two RN states.
        root.Children.Add(Ui.AssetImage("journal_bg.png", Aspect.AspectFill));
        root.Children.Add(new BoxView { BackgroundColor = Color.FromArgb("#CC001428") });

        VerticalStackLayout stack = new() { Padding = new Thickness(30, 50, 30, 20), Spacing = 15 };
        Grid row = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        HorizontalStackLayout tabs = new() { Spacing = 10 };
        tabs.Children.Add(TabButton("Daily", _viewModel.Daily, () =>
        {
            _viewModel.Daily = true;
            _viewModel.NormalUi = true;
            Render();
        }));
        tabs.Children.Add(TabButton("Statistics", !_viewModel.Daily, () =>
        {
            _viewModel.Daily = false;
            _viewModel.NormalUi = true;
            Render();
        }));
        row.Add(tabs, 0, 0);
        row.Add(new PrivacyTooltipView(_transparency.Statistics, "Statistics", 50), 1, 0);
        stack.Children.Add(row);
        if (_viewModel.Daily)
        {
            stack.Children.Add(new WeekCalendarView(() => _viewModel.SelectedDate, async date =>
            {
                _viewModel.SelectedDate = date;
                await Task.Delay(300);
            }));
        }
        root.Children.Add(stack);
        return root;
    }

    private static Button TabButton(string text, bool active, Action clicked)
    {
        Button button = new()
        {
            Text = text,
            FontFamily = "SpaceMono",
            FontSize = 18,
            TextColor = active ? Colors.White : AppColors.LightGrey,
            BackgroundColor = active ? AppColors.GeneralBlue : Colors.Transparent,
            CornerRadius = 20,
            Padding = new Thickness(20, 10)
        };
        button.Clicked += (_, _) => clicked();
        return button;
    }

    private static void DailyStatisticsContent(VerticalStackLayout content)
    {
        content.Children.Add(Ui.Text("Sleep Quality", 20, Colors.White, FontAttributes.Bold));
        Grid quality = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };
        quality.Add(new Image { Source = "sleep_quality_daily.png", WidthRequest = 100, HeightRequest = 100, Aspect = Aspect.AspectFill }, 0, 0);
        quality.Add(new VerticalStackLayout
        {
            Padding = new Thickness(20, 0, 0, 0),
            Children =
            {
                Ui.Text("Time in Bed", 14, AppColors.LightGrey),
                Ui.Text("10:14 PM - 6:44 AM", 16, Colors.White, FontAttributes.Bold),
                Ui.Text("8h 30m", 14, AppColors.LightGrey),
                Ui.Text("Pretty Good!", 16, AppColors.GeneralBlue, FontAttributes.Bold)
            }
        }, 1, 0);
        content.Children.Add(Ui.Card(quality));
        content.Children.Add(StatisticItem("Sleep Stages", "sleep_stages_daily.png"));
        content.Children.Add(StageGrid());
        content.Children.Add(InsightsGrid());
        content.Children.Add(Ui.Text("Sleep Clips", 20, Colors.White, FontAttributes.Bold));
        content.Children.Add(SleepClips());
    }

    private static View StatisticItem(string label, string imageFile)
    {
        // MIGRATION: The RN StatisticItem renders the section label outside
        //            the dark graph card, then reserves a 200px image area.
        //            Keeping that structure prevents labels from overlapping
        //            the graph bitmap on smaller Android screens.
        return new VerticalStackLayout
        {
            Spacing = 15,
            Children =
            {
                Ui.Text(label, 18, Colors.White, FontAttributes.Bold),
                Ui.Card(new Grid
                {
                    HeightRequest = 200,
                    Children =
                    {
                        new Image
                        {
                            Source = imageFile,
                            Aspect = Aspect.AspectFit,
                            HorizontalOptions = LayoutOptions.Fill,
                            VerticalOptions = LayoutOptions.Fill
                        }
                    }
                })
            }
        };
    }

    private static View StageGrid()
    {
        Grid grid = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };
        (string label, string pct, string dur, Color color)[] stages =
        [
            ("Deep Sleep", "21%", "2h 25m", Color.FromArgb("#4A4A4A")),
            ("Light Sleep", "56%", "4h 35m", Color.FromArgb("#6A9EFF")),
            ("REM", "17%", "1h 25m", Color.FromArgb("#8A6AFF")),
            ("Awake", "6%", "30m", Color.FromArgb("#FFA64A"))
        ];
        for (int i = 0; i < stages.Length; i++)
        {
            grid.Add(new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    Ui.Card(new Label { Text = "Moon", TextColor = Colors.White, FontFamily = "SpaceMono", FontSize = 12, HorizontalTextAlignment = TextAlignment.Center }, padding: 8, radius: 20, background: stages[i].color),
                    Ui.Text(stages[i].label, 12, AppColors.LightGrey, align: TextAlignment.Center),
                    Ui.Text(stages[i].pct, 16, Colors.White, FontAttributes.Bold, TextAlignment.Center),
                    Ui.Text(stages[i].dur, 12, AppColors.LightGrey, align: TextAlignment.Center)
                }
            }, i, 0);
        }
        return grid;
    }

    private static View InsightsGrid()
    {
        VerticalStackLayout outer = new() { Spacing = 12 };
        outer.Children.Add(InsightRow(("Bed", "In Bed", "8h 30 min"), ("Moon", "Asleep", "7h 34 min"), ("Time", "Asleep After", "11 min")));
        outer.Children.Add(InsightRow(("Volume", "Noise", "39 dB"), ("Volume", "Snoring", "1h 30 min")));
        return outer;
    }

    private static View InsightRow(params (string icon, string label, string value)[] items)
    {
        Grid row = new();
        for (int i = 0; i < items.Length; i++)
        {
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            row.Add(Ui.Card(new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    Ui.Text(items[i].icon, 14, AppColors.GeneralBlue, FontAttributes.Bold, TextAlignment.Center),
                    Ui.Text(items[i].label, 12, AppColors.LightGrey, align: TextAlignment.Center),
                    Ui.Text(items[i].value, 14, Colors.White, FontAttributes.Bold, TextAlignment.Center)
                }
            }, padding: 16, radius: 12), i, 0);
        }
        return row;
    }

    private enum SleepClipTab
    {
        Snoring,
        Talking
    }

    private static View SleepClips()
    {
        VerticalStackLayout stack = new() { Spacing = 12 };
        HorizontalStackLayout tabs = new() { Spacing = 10 };
        SleepClipTab activeClipTab = SleepClipTab.Snoring;
        Button snoringTab = ClipTabButton("Snoring", active: true);
        Button talkingTab = ClipTabButton("Talking", active: false);

        void SetActiveClipTab(SleepClipTab tab)
        {
            activeClipTab = tab;
            // MIGRATION: RN stores activeClipTab in component state and only
            //            restyles the two clip pills. Updating these MAUI
            //            buttons in place preserves that behavior without
            //            rebuilding the page and jumping scroll position.
            StyleClipTab(snoringTab, activeClipTab == SleepClipTab.Snoring);
            StyleClipTab(talkingTab, activeClipTab == SleepClipTab.Talking);
        }

        snoringTab.Clicked += (_, _) => SetActiveClipTab(SleepClipTab.Snoring);
        talkingTab.Clicked += (_, _) => SetActiveClipTab(SleepClipTab.Talking);
        tabs.Children.Add(snoringTab);
        tabs.Children.Add(talkingTab);
        stack.Children.Add(tabs);
        for (int i = 0; i < 3; i++)
        {
            Grid clip = new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 12,
                VerticalOptions = LayoutOptions.Center
            };
            // MIGRATION: React Native flex rows place the play icon, time,
            //            waveform, and menu in separate columns. MAUI Grid
            //            children default to column 0 unless assigned, which
            //            caused the text and waveform to draw over each other.
            clip.Add(Ui.Text("Play", 12, AppColors.GeneralBlue, FontAttributes.Bold), 0, 0);
            clip.Add(Ui.Text("11:04 PM", 14, Colors.White, FontAttributes.Bold), 1, 0);
            clip.Add(Waveform(), 2, 0);
            clip.Add(Ui.Text("...", 20, AppColors.LightGrey, align: TextAlignment.End), 3, 0);
            stack.Children.Add(Ui.Card(clip, padding: 12, radius: 12, background: Color.FromArgb("#333333")));
        }
        return Ui.Card(stack);
    }

    private static Button ClipTabButton(string text, bool active)
    {
        Button button = new()
        {
            Text = text,
            FontFamily = "SpaceMono",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 16,
            Padding = new Thickness(16, 8),
            MinimumHeightRequest = 0
        };
        StyleClipTab(button, active);
        return button;
    }

    private static void StyleClipTab(Button button, bool active)
    {
        button.BackgroundColor = active ? AppColors.GeneralBlue : Color.FromArgb("#333333");
        button.TextColor = active ? Colors.White : AppColors.LightGrey;
    }

    private static View Waveform()
    {
        HorizontalStackLayout bars = new() { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        int[] heights = [8, 13, 20, 9, 18, 14, 24, 10, 16, 22, 12, 19, 7, 15, 21, 11, 17, 23, 9, 14];
        foreach (int height in heights)
        {
            bars.Children.Add(new BoxView { WidthRequest = 2, HeightRequest = height, BackgroundColor = AppColors.GeneralBlue });
        }
        return bars;
    }

    private static View PrivacyStatisticsContent()
    {
        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Ui.Text("Statistics Privacy", 20, Colors.White, FontAttributes.Bold),
                Ui.Text("Statistics are derived from sleep, journal, microphone, light, and accelerometer data according to your consent preferences.", 15),
                Ui.Text("Storage: local derived summaries unless cloud storage is enabled.", 15),
                Ui.Text("Access: no third-party marketing or commercial access.", 15)
            }
        });
    }
}
