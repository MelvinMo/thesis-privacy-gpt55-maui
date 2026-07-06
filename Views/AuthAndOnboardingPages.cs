using SleepTrackerMaui.Controls;
using SleepTrackerMaui.Services;
using SleepTrackerMaui.ViewModels;

namespace SleepTrackerMaui.Views;

public sealed class AuthPage : ContentPage
{
    private readonly AuthViewModel _viewModel;
    private readonly ProfileViewModel _profile;
    private readonly Entry _email;
    private readonly Entry _password;
    private readonly Entry _firstName;
    private readonly Entry _lastName;
    private readonly Entry _confirmPassword;
    private readonly Label _title;
    private readonly Label _subtitle;
    private readonly Label _error;
    private readonly Button _submit;
    private readonly Button _toggle;

    public AuthPage(AuthViewModel viewModel, ProfileViewModel profile)
    {
        _viewModel = viewModel;
        _profile = profile;
        BindingContext = _viewModel;
        BackgroundColor = AppColors.AppBackground;
        Shell.SetNavBarIsVisible(this, false);

        _title = Ui.Text("Welcome Back!", 32, Colors.White, FontAttributes.Bold);
        _subtitle = Ui.Text("Sign in to your account", 16);
        _email = Ui.Input("Email");
        _password = Ui.Input("Password", password: true);
        _firstName = Ui.Input("First Name");
        _lastName = Ui.Input("Last Name");
        _confirmPassword = Ui.Input("Confirm Password", password: true);
        _error = Ui.Text(string.Empty, 13, AppColors.TooltipRed);
        _submit = Ui.BlueButton("Sign In", SubmitAsync);
        _toggle = new Button
        {
            Text = "Don't have an account? Register",
            FontFamily = "SpaceMono",
            TextColor = AppColors.HyperlinkBlue,
            BackgroundColor = Colors.Transparent
        };
        _toggle.Clicked += (_, _) =>
        {
            _viewModel.RegisterMode = !_viewModel.RegisterMode;
            RenderMode();
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24, 40, 24, 24),
                Spacing = 0,
                Children =
                {
                    _title,
                    _subtitle,
                    new BoxView { HeightRequest = 40, Opacity = 0 },
                    _email,
                    _firstName,
                    _lastName,
                    _password,
                    _confirmPassword,
                    _error,
                    _submit,
                    _toggle
                }
            }
        };
        RenderMode();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CheckAuthAsync();
        await _profile.LoadAsync();
        if (_viewModel.IsAuthenticated)
        {
            await NavigateAfterAuthAsync();
        }
    }

    private void RenderMode()
    {
        bool register = _viewModel.RegisterMode;
        _title.Text = register ? "Register Now!" : "Welcome Back!";
        _subtitle.Text = register ? "Create an account" : "Sign in to your account";
        _firstName.IsVisible = register;
        _lastName.IsVisible = register;
        _confirmPassword.IsVisible = register;
        _submit.Text = register ? "Register" : "Sign In";
        _toggle.Text = register ? "Do you have an account? Sign In" : "Don't have an account? Register";
        _error.Text = string.Empty;
    }

    private async Task SubmitAsync()
    {
        _viewModel.Email = _email.Text ?? string.Empty;
        _viewModel.Password = _password.Text ?? string.Empty;
        _viewModel.FirstName = _firstName.Text ?? string.Empty;
        _viewModel.LastName = _lastName.Text ?? string.Empty;
        _viewModel.ConfirmPassword = _confirmPassword.Text ?? string.Empty;

        // MIGRATION: The RN store only considered login successful when the
        //            backend returned a user and token. The MAUI page follows
        //            that strict repository result and never falls through to
        //            tabs on failed credentials.
        await _viewModel.SubmitCommand.ExecuteAsync(null);
        _error.Text = _viewModel.ErrorMessage ?? string.Empty;
        if (_viewModel.IsAuthenticated)
        {
            await _profile.LoadAsync();
            await NavigateAfterAuthAsync();
        }
    }

    private async Task NavigateAfterAuthAsync()
    {
        if (!_profile.PrivacyOnboardingComplete || !_profile.AppOnboardingComplete)
        {
            await Shell.Current.GoToAsync("//Onboarding");
            return;
        }
        await Shell.Current.GoToAsync("//Sleep");
    }
}

public sealed class OnboardingPage : ContentPage
{
    private readonly OnboardingViewModel _viewModel;
    private readonly IDeviceSensorService _sensors;
    private readonly Grid _root = new();

    public OnboardingPage(OnboardingViewModel viewModel, IDeviceSensorService sensors)
    {
        _viewModel = viewModel;
        _sensors = sensors;
        BindingContext = _viewModel;
        BackgroundColor = AppColors.AppBackground;
        Shell.SetNavBarIsVisible(this, false);
        // MIGRATION: The page keeps one root container and only swaps its
        //            children. Reassigning the same root to Content on every
        //            render can trigger Android's "child already has parent"
        //            native view guard.
        Content = _root;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Profile.LoadAsync();
        Render();
    }

    private void Render()
    {
        _root.Children.Clear();
        _root.RowDefinitions.Clear();
        _root.RowDefinitions.Add(new RowDefinition(new GridLength(3, GridUnitType.Star)));
        _root.RowDefinitions.Add(new RowDefinition(new GridLength(4, GridUnitType.Star)));

        OnboardingStep step = OnboardingStep.For(_viewModel.StepIndex);
        if (!string.IsNullOrWhiteSpace(step.ImageFile))
        {
            _root.Add(HeaderWithImage(step.ImageFile, back: !_viewModel.IsFirstStep), 0, 0);
            _root.Add(Body(step), 0, 1);
        }
        else
        {
            _root.RowDefinitions.Clear();
            _root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            _root.Add(HeaderOnly(back: !_viewModel.IsFirstStep), 0, 0);
            _root.Add(Body(step), 0, 1);
        }
    }

    private View HeaderWithImage(string imageFile, bool back)
    {
        Grid grid = new();
        grid.Children.Add(Ui.AssetImage(imageFile, Aspect.AspectFill));
        grid.Children.Add(new BoxView { BackgroundColor = Color.FromArgb("#66000000") });
        grid.Children.Add(OnboardingHeader("Your Privacy Matters to Us", back));
        return grid;
    }

    private View HeaderOnly(bool back) => OnboardingHeader("Your Privacy Matters to Us", back);

    private View OnboardingHeader(string title, bool back)
    {
        Grid row = new()
        {
            Padding = new Thickness(20, 60, 20, 20),
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(back ? 50 : 0)),
                new ColumnDefinition(GridLength.Star)
            }
        };
        if (back)
        {
            Button backButton = new()
            {
                Text = "<",
                TextColor = AppColors.GeneralBlue,
                BackgroundColor = Colors.Transparent,
                FontFamily = "SpaceMono",
                FontSize = 24
            };
            backButton.Clicked += (_, _) =>
            {
                _viewModel.Back();
                Render();
            };
            row.Add(backButton, 0, 0);
        }

        Label label = Ui.Text(title, 24, Colors.White, FontAttributes.Bold, back ? TextAlignment.Start : TextAlignment.Center);
        row.Add(label, 1, 0);
        return row;
    }

    private View Body(OnboardingStep step)
    {
        VerticalStackLayout stack = new()
        {
            Padding = new Thickness(24, 32, 24, 40),
            Spacing = 16
        };

        foreach (OnboardingTextBlock block in step.Blocks)
        {
            stack.Children.Add(Ui.Text(block.Title, 18, Colors.White, FontAttributes.Bold));
            stack.Children.Add(Ui.Text(block.Body, 16));
            if (!string.IsNullOrWhiteSpace(block.LinkText))
            {
                Button link = new()
                {
                    Text = block.LinkText,
                    FontFamily = "SpaceMono",
                    FontSize = 14,
                    TextColor = AppColors.HyperlinkBlue,
                    BackgroundColor = Colors.Transparent,
                    HorizontalOptions = LayoutOptions.Start,
                    Padding = 0
                };
                link.Clicked += async (_, _) => await Ui.RunGuardedAsync(async () => await Shell.Current.GoToAsync(nameof(PrivacyPolicyPage)));
                stack.Children.Add(link);
            }
        }

        if (step.Kind == OnboardingStepKind.Microphone)
        {
            stack.Children.Add(PermissionToggle(
                "Yes, you have permission to access my microphone to record my sleep sounds.",
                _viewModel.Profile.Preferences.MicrophoneEnabled,
                async enabled =>
                {
                    if (enabled)
                    {
                        PermissionStatus status = await Permissions.RequestAsync<Permissions.Microphone>();
                        if (status != PermissionStatus.Granted)
                        {
                            // MIGRATION: Android permission callbacks can arrive
                            //            while MAUI is re-rendering this page.
                            //            Inline feedback avoids stacking a
                            //            second native dialog over the OS
                            //            permission sheet.
                            _viewModel.ErrorMessage = "Microphone access denied. Enable it in device settings if you wish to use this feature.";
                            enabled = false;
                        }
                    }
                    await _viewModel.Profile.SetMicrophoneEnabledAsync(enabled);
                    Render();
                }));
        }

        if (step.Kind == OnboardingStepKind.Accelerometer)
        {
            stack.Children.Add(PermissionToggle(
                "Yes, you have my permission to access my accelerometer to track my activity levels.",
                _viewModel.Profile.Preferences.AccelerometerEnabled,
                async enabled =>
                {
                    await _viewModel.Profile.SetAccelerometerEnabledAsync(enabled);
                    Render();
                }));
        }

        if (step.Kind == OnboardingStepKind.Light)
        {
            if (!_sensors.IsLightSensorAvailable)
            {
                // MIGRATION: Expo light sensor did not work on iOS. MAUI
                //            exposes a graceful stub instead of rendering a
                //            switch that appears broken.
                stack.Children.Add(new SensorNotAvailableWidget());
            }
            stack.Children.Add(PermissionToggle(
                "Yes, you have my permission to access my light sensor to track ambient light levels.",
                _viewModel.Profile.Preferences.LightSensorEnabled,
                async enabled =>
                {
                    await _viewModel.Profile.SetLightSensorEnabledAsync(enabled);
                    Render();
                }));
        }

        if (step.Kind == OnboardingStepKind.Cloud)
        {
            stack.Children.Add(PermissionToggle(
                "Yes, you have my permission to store my personal health information on secure Google Cloud servers",
                _viewModel.Profile.Preferences.CloudStorageEnabled,
                async enabled =>
                {
                    await _viewModel.Profile.SetCloudStorageEnabledAsync(enabled);
                    Render();
                }));
        }

        if (step.Kind == OnboardingStepKind.PolicyAgreement)
        {
            stack.Children.Add(PermissionToggle(
                "I have read and agree to the Privacy Policy.",
                _viewModel.Profile.Preferences.AgreedToPrivacyPolicy,
                async enabled =>
                {
                    await _viewModel.Profile.SetPrivacyPolicyAgreementAsync(enabled);
                    Render();
                }));
        }

        if (step.Kind == OnboardingStepKind.Transparency)
        {
            stack.Children.Add(PrivacyIconLegend());
        }

        if (step.Kind == OnboardingStepKind.Question)
        {
            stack.Children.Add(QuestionOptions());
        }

        // MIGRATION: This label is recreated on each render because native
        //            Android views cannot be attached to two parents. Reusing
        //            a cached Label caused a crash during permission callbacks.
        stack.Children.Add(Ui.Text(_viewModel.ErrorMessage, 13, AppColors.TooltipRed));
        stack.Children.Add(Ui.BlueButton("Continue", ContinueAsync));
        return new ScrollView { Content = stack };
    }

    private View PermissionToggle(string label, bool value, Func<bool, Task> changed)
    {
        Switch toggle = new()
        {
            IsToggled = value,
            ThumbColor = Colors.White,
            OnColor = Color.FromArgb("#4CAF50"),
            HorizontalOptions = LayoutOptions.End
        };
        toggle.Toggled += async (_, args) =>
        {
            try
            {
                _viewModel.ErrorMessage = string.Empty;
                await changed(args.Value);
            }
            catch (Exception ex)
            {
                // MIGRATION: React Native promise rejections stayed inside the
                //            component state path. MAUI event handlers run on
                //            the UI thread, so exceptions must be captured to
                //            avoid terminating the Android process.
                _viewModel.ErrorMessage = ex.Message;
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

    private View PrivacyIconLegend()
    {
        HorizontalStackLayout row = new() { Spacing = 18, HorizontalOptions = LayoutOptions.Center };
        foreach (string icon in new[] { "privacy_high.png", "privacy_medium.png", "privacy_low.png" })
        {
            row.Children.Add(new Image { Source = ImageSource.FromFile(icon), WidthRequest = 42, HeightRequest = 42 });
        }
        return Ui.Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Ui.Text("Privacy Risk Indicators", 16, Colors.White, FontAttributes.Bold, TextAlignment.Center),
                row,
                Ui.Text("Tooltip System: click privacy icons next to data types for contextual information.", 14),
                Ui.Text("Privacy Pages: transform entire screens to show comprehensive privacy details.", 14),
                Ui.Text("Real-time Analysis: visual feedback explains privacy risks as they occur.", 14)
            }
        });
    }

    private View QuestionOptions()
    {
        VerticalStackLayout stack = new() { Spacing = 12 };
        foreach (string option in new[] { "6 hours or less", "6 - 8 hours", "8 - 10 hours" })
        {
            Button button = new()
            {
                Text = option,
                FontFamily = "SpaceMono",
                FontSize = 16,
                TextColor = _viewModel.SelectedSleepDuration == option ? AppColors.LightBlack : Colors.White,
                BackgroundColor = _viewModel.SelectedSleepDuration == option ? AppColors.GeneralBlue : AppColors.LightBlack,
                CornerRadius = 12,
                Padding = new Thickness(16)
            };
            button.Clicked += (_, _) =>
            {
                _viewModel.SelectedSleepDuration = option;
                Render();
            };
            stack.Children.Add(button);
        }
        return stack;
    }

    private async Task ContinueAsync()
    {
        await _viewModel.ContinueAsync();
        if (_viewModel.Profile.PrivacyOnboardingComplete && _viewModel.Profile.AppOnboardingComplete)
        {
            await Shell.Current.GoToAsync("//Sleep");
            return;
        }
        Render();
    }

    private sealed record OnboardingTextBlock(string Title, string Body, string LinkText = "");

    private sealed record OnboardingStep(OnboardingStepKind Kind, string ImageFile, IReadOnlyList<OnboardingTextBlock> Blocks)
    {
        public static OnboardingStep For(int index) => index switch
        {
            0 => new(OnboardingStepKind.Microphone, "microphone_bg.png",
            [
                new("Purpose:", "Your microphone will listen for sounds like snoring or sleep talking only while you are sleeping. Analyzing these sounds will help you detect potential sleep disruptions and get a clearer picture of your sleep environment.", "Read more about sound data and snoring detection")
            ]),
            1 => new(OnboardingStepKind.Accelerometer, "running_bg.png",
            [
                new("Purpose:", "The accelerometer on your device will be used to track your body movements during sleep and throughout the day continuously in the background. This will help us to correlate activity levels with sleep quality.", "More about collecting activity data")
            ]),
            2 => new(OnboardingStepKind.Light, "bedroom_light_bg.png",
            [
                new("Purpose:", "The ambient light sensor on your device will be used to monitor the light conditions in your sleep environment only while you are sleeping, helping us to understand how light exposure affects your sleep quality.", "More about collecting ambient light data")
            ]),
            3 => new(OnboardingStepKind.Journal, "journal_bg.png",
            [
                new("Journal Data:", "Information about your mood, habits, symptoms can help us correlate your personal experiences with your sleep patterns. You can voluntarily provide us with this data by making diary entries and sleep notes in the app's Journal section.", "More about collecting journal data"),
                new("Derived Data:", "The app will derive data about you such as sleep quality, correlations, insights and recommendations. This will be treated as sensitive personal health information.", "More about derived data")
            ]),
            4 => new(OnboardingStepKind.Cloud, string.Empty,
            [
                new("Data Storage", "By default all of your personal health information (data collected and derived data) will be stored on your mobile device. If you opt in, we will store your personal health information in the cloud, allowing us to provide more complex sleep analysis. All data will be encrypted while in storage and when it is being transmitted."),
                new("Data Access:", "We are committed to strict limitations on data sharing. We do not give your personal information to any third parties for marketing, advertising, or any other commercial purposes.", "More about data storage and data access")
            ]),
            5 => new(OnboardingStepKind.PolicyAgreement, string.Empty,
            [
                new(string.Empty, "The previous screens explained the most important parts of the privacy policy. Before you proceed, please review the full Privacy Policy to understand in greater detail how we collect, use, and protect your health data.", "Read our full Privacy Policy")
            ]),
            6 => new(OnboardingStepKind.Transparency, string.Empty,
            [
                new("Privacy Features In this App", "This prototype app is designed to prioritize transparency by embedding details about data collection within the UI. Our real-time privacy analysis system monitors data collection and provides instant visual feedback through dynamic privacy icons.")
            ]),
            _ => new(OnboardingStepKind.Question, string.Empty,
            [
                new("How much sleep do you usually get at night?", "The next few screens will ask you questions about your current sleep quality and sleep habits. This will help us understand your sleep better and provide personalized insights.")
            ])
        };
    }

    private enum OnboardingStepKind
    {
        Microphone,
        Accelerometer,
        Light,
        Journal,
        Cloud,
        PolicyAgreement,
        Transparency,
        Question
    }
}
