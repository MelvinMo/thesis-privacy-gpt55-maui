using System.Text.Json;
using SleepTrackerMaui.Controls;
using SleepTrackerMaui.Repositories;
using SleepTrackerMaui.Services;
using SleepTrackerMaui.ViewModels;

namespace SleepTrackerMaui.Views;

public sealed class ProfilePage : ContentPage
{
    private readonly IAuthRepository _auth;
    private readonly AuthViewModel _authViewModel;

    public ProfilePage(IAuthRepository auth, AuthViewModel authViewModel)
    {
        _auth = auth;
        _authViewModel = authViewModel;
        BackgroundColor = AppColors.AppBackground;
        Shell.SetNavBarIsVisible(this, false);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Render();
    }

    private void Render()
    {
        VerticalStackLayout stack = new()
        {
            Padding = new Thickness(20),
            Spacing = 30,
            VerticalOptions = LayoutOptions.Fill,
            Children =
            {
                Ui.Text("Profile", 32, Colors.White, FontAttributes.Bold, TextAlignment.Center),
                Ui.Text($"Hello, {_auth.CurrentUser?.FirstName ?? "Guest"}", 24, Colors.White, FontAttributes.Bold, TextAlignment.Center),
                MenuButton("Consent Preferences", async () => await Shell.Current.GoToAsync(nameof(ConsentPreferencesPage))),
                MenuButton("Privacy Policy", async () => await Shell.Current.GoToAsync(nameof(PrivacyPolicyPage))),
                new BoxView { HeightRequest = 40, Opacity = 0 },
                LogoutButton()
            }
        };
        Content = stack;
    }

    private static Button MenuButton(string text, Func<Task> clicked)
    {
        Button button = new()
        {
            Text = text,
            FontFamily = "SpaceMono",
            FontSize = 18,
            TextColor = Colors.White,
            BackgroundColor = AppColors.LightBlack,
            CornerRadius = 12,
            Padding = new Thickness(20, 16),
            HorizontalOptions = LayoutOptions.Fill
        };
        button.Clicked += async (_, _) => await Ui.RunGuardedAsync(clicked);
        return button;
    }

    private Button LogoutButton()
    {
        Button button = Ui.BlueButton("LOGOUT", async () =>
        {
            await _authViewModel.LogoutCommand.ExecuteAsync(null);
            await Shell.Current.GoToAsync("//Auth");
        });
        button.TextColor = Colors.White;
        button.CornerRadius = 12;
        button.Padding = new Thickness(0, 18);
        return button;
    }
}

public sealed class ConsentPreferencesPage : ContentPage
{
    private readonly ProfileViewModel _profile;
    private readonly IDeviceSensorService _sensors;
    private string _message = string.Empty;

    public ConsentPreferencesPage(ProfileViewModel profile, IDeviceSensorService sensors)
    {
        _profile = profile;
        _sensors = sensors;
        BackgroundColor = AppColors.AppBackground;
        Shell.SetNavBarIsVisible(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _profile.LoadAsync();
        Render();
    }

    private void Render()
    {
        // MIGRATION: Recreate the layout tree on each render instead of
        //            wrapping a cached StackLayout in a new ScrollView. Android
        //            native views can only belong to one parent at a time.
        VerticalStackLayout stack = new()
        {
            Padding = new Thickness(24, 50, 24, 24),
            Spacing = 12
        };
        stack.Children.Add(Header());
        stack.Children.Add(Toggle("Yes, you have permission to access my microphone to record my sleep sounds.", _profile.Preferences.MicrophoneEnabled, async enabled =>
        {
            if (enabled)
            {
                PermissionStatus status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    // MIGRATION: Inline permission feedback avoids stacking a
                    //            MAUI AlertDialog over Android's permission
                    //            callback path, which is fragile on some OEM
                    //            Android builds.
                    _message = "Microphone access denied. Enable it in device settings if you wish to use this feature.";
                    enabled = false;
                }
            }
            await _profile.SetMicrophoneEnabledAsync(enabled);
            Render();
        }));
        stack.Children.Add(Link("Read more about sound data and snoring detection"));
        stack.Children.Add(Toggle("Yes, you have my permission to access my accelerometer to track my activity levels.", _profile.Preferences.AccelerometerEnabled, async enabled =>
        {
            await _profile.SetAccelerometerEnabledAsync(enabled);
            Render();
        }));
        stack.Children.Add(Link("More about collecting activity data"));
        if (!_sensors.IsLightSensorAvailable)
        {
            stack.Children.Add(new SensorNotAvailableWidget());
        }
        stack.Children.Add(Toggle("Yes, you have my permission to access my light sensor to track ambient light levels.", _profile.Preferences.LightSensorEnabled, async enabled =>
        {
            await _profile.SetLightSensorEnabledAsync(enabled);
            Render();
        }));
        stack.Children.Add(Link("More about collecting ambient light data"));
        stack.Children.Add(Toggle("Yes, you have my permission to store my personal health information on secure Google Cloud servers", _profile.Preferences.CloudStorageEnabled, async enabled =>
        {
            await _profile.SetCloudStorageEnabledAsync(enabled);
            Render();
        }));
        stack.Children.Add(Link("More about data storage and data access"));
        stack.Children.Add(Ui.Text(_message, 13, AppColors.TooltipRed));
        Content = new ScrollView { Content = stack };
    }

    private static View Header()
    {
        return new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            Children =
            {
                BackButton(),
                Ui.Text("Your Privacy Matters to Us", 24, Colors.White, FontAttributes.Bold)
            }
        };
    }

    private static Button BackButton()
    {
        Button button = new()
        {
            Text = "<",
            TextColor = AppColors.GeneralBlue,
            BackgroundColor = Colors.Transparent,
            FontFamily = "SpaceMono",
            FontSize = 24,
            Padding = 0
        };
        button.Clicked += async (_, _) => await Ui.RunGuardedAsync(async () => await Shell.Current.GoToAsync(".."));
        return button;
    }

    private View Toggle(string label, bool value, Func<bool, Task> changed)
    {
        Switch toggle = new()
        {
            IsToggled = value,
            ThumbColor = Colors.White,
            OnColor = Color.FromArgb("#4CAF50")
        };
        toggle.Toggled += async (_, args) =>
        {
            try
            {
                _message = string.Empty;
                await changed(args.Value);
            }
            catch (Exception ex)
            {
                // MIGRATION: Event-handler failures are translated to page
                //            state instead of escaping the UI thread and
                //            killing the Android process.
                _message = ex.Message;
                Render();
            }
        };
        Grid row = new()
        {
            Padding = new Thickness(20, 10),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        row.Add(Ui.Text(label, 16), 0, 0);
        row.Add(toggle, 1, 0);
        return row;
    }

    private static Button Link(string text)
    {
        Button button = new()
        {
            Text = text,
            FontFamily = "SpaceMono",
            FontSize = 14,
            TextColor = AppColors.HyperlinkBlue,
            BackgroundColor = Colors.Transparent,
            Padding = 0
        };
        button.Clicked += async (_, _) => await Ui.RunGuardedAsync(async () => await Shell.Current.GoToAsync(nameof(PrivacyPolicyPage)));
        return button;
    }
}

public sealed class PrivacyPolicyPage : ContentPage
{
    private readonly VerticalStackLayout _stack = new();

    public PrivacyPolicyPage()
    {
        BackgroundColor = AppColors.AppBackground;
        Shell.SetNavBarIsVisible(this, false);
        Content = new ScrollView { Content = _stack };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RenderPolicyAsync();
    }

    private async Task RenderPolicyAsync()
    {
        _stack.Children.Clear();
        _stack.Padding = new Thickness(15, 50, 15, 24);
        _stack.Spacing = 10;
        _stack.Children.Add(Header());

        try
        {
            // MIGRATION: The policy is loaded from the original React Native
            //            JSON asset so MAUI content stays text-compatible with
            //            the source app and avoids stale hand-copied sections.
            await using Stream stream = await FileSystem.OpenAppPackageFileAsync("privacyPolicyData.json");
            using JsonDocument document = await JsonDocument.ParseAsync(stream);
            JsonElement policy = document.RootElement.GetProperty("privacyPolicy");
            JsonElement metadata = policy.GetProperty("metadata");
            _stack.Children.Add(Ui.Text($"Version: {metadata.GetProperty("version").GetString()} | Effective Date: {metadata.GetProperty("effectiveDate").GetString()} | Last Updated: {metadata.GetProperty("lastUpdated").GetString()}", 12, Color.FromArgb("#BBBBBB"), align: TextAlignment.Center));

            _stack.Children.Add(Ui.Text("Table of Contents", 18, Colors.White, FontAttributes.Bold));
            foreach (JsonElement toc in policy.GetProperty("tableOfContents").EnumerateArray())
            {
                _stack.Children.Add(Ui.Text($"• {toc.GetString()}", 15, AppColors.GeneralBlue, FontAttributes.Bold));
            }

            RenderElement(policy.GetProperty("sections"), _stack, 1, null);
        }
        catch (Exception ex)
        {
            _stack.Children.Add(Ui.Text($"Failed to load privacy policy: {ex.Message}", 14, AppColors.TooltipRed));
        }
    }

    private static View Header()
    {
        Grid header = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };
        Button back = new()
        {
            Text = "<",
            TextColor = AppColors.GeneralBlue,
            BackgroundColor = Colors.Transparent,
            FontFamily = "SpaceMono",
            FontSize = 24,
            Padding = 0
        };
        back.Clicked += async (_, _) => await Ui.RunGuardedAsync(async () => await Shell.Current.GoToAsync(".."));
        header.Add(back, 0, 0);
        header.Add(Ui.Text("Privacy Policy", 24, Colors.White, FontAttributes.Bold), 1, 0);
        return header;
    }

    private static void RenderElement(JsonElement element, VerticalStackLayout stack, int level, string? label)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty("title", out JsonElement title))
                {
                    stack.Children.Add(Heading(title.GetString() ?? label ?? string.Empty, level));
                }
                else if (!string.IsNullOrWhiteSpace(label) && label != "id" && label != "content")
                {
                    stack.Children.Add(Heading(Humanize(label), level));
                }

                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (property.Name is "id" or "title")
                    {
                        continue;
                    }
                    RenderElement(property.Value, stack, level + 1, property.Name);
                }
                break;
            case JsonValueKind.Array:
                foreach (JsonElement child in element.EnumerateArray())
                {
                    RenderElement(child, stack, level, label);
                }
                break;
            case JsonValueKind.String:
                string value = element.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(value))
                {
                    break;
                }
                stack.Children.Add(Ui.Text(label is "content" or null ? value : $"• {Humanize(label)}: {value}", level <= 2 ? 16 : 14, label is null or "content" ? Colors.White : Color.FromArgb("#ADD8E6")));
                break;
        }
    }

    private static Label Heading(string text, int level)
    {
        return Ui.Text(text, level <= 2 ? 22 : 18, AppColors.GeneralBlue, FontAttributes.Bold);
    }

    private static string Humanize(string value)
    {
        List<char> chars = [];
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (i > 0 && char.IsUpper(c))
            {
                chars.Add(' ');
            }
            chars.Add(c);
        }
        string result = new(chars.ToArray());
        return char.ToUpperInvariant(result[0]) + result[1..];
    }
}
