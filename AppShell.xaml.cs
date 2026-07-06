using SleepTrackerMaui.Services;
using SleepTrackerMaui.Views;

namespace SleepTrackerMaui;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();
        Items.Clear();
        // MIGRATION: Expo tab navigation used dark themed tabs with icon-only
        //            visual affordances. MAUI Shell defaults to a white tab bar,
        //            so the colors are set explicitly to preserve the source UI.
        Shell.SetTabBarBackgroundColor(this, AppColors.AppBackground);
        Shell.SetTabBarForegroundColor(this, AppColors.GeneralBlue);
        Shell.SetTabBarTitleColor(this, AppColors.GeneralBlue);
        Shell.SetTabBarUnselectedColor(this, AppColors.LightGrey);

        // MIGRATION: Expo Router stacks become MAUI Shell routes. Auth and
        //            onboarding stay outside the tab bar, while Sleep/Journal/
        //            Statistics/Profile preserve the original tab order.
        Routing.RegisterRoute(nameof(SleepModePage), typeof(SleepModePage));
        Routing.RegisterRoute(nameof(PrivacyPolicyPage), typeof(PrivacyPolicyPage));
        Routing.RegisterRoute(nameof(ConsentPreferencesPage), typeof(ConsentPreferencesPage));

        Items.Add(new ShellContent
        {
            Route = "Auth",
            ContentTemplate = new DataTemplate(() => services.GetRequiredService<AuthPage>())
        });

        Items.Add(new ShellContent
        {
            Route = "Onboarding",
            ContentTemplate = new DataTemplate(() => services.GetRequiredService<OnboardingPage>())
        });

        TabBar tabs = new()
        {
            Route = "Tabs",
            FlyoutDisplayOptions = FlyoutDisplayOptions.AsMultipleItems
        };
        tabs.Items.Add(Tab("Sleep", "Sleep", "tab_sleep.svg", services.GetRequiredService<SleepPage>));
        tabs.Items.Add(Tab("Journal", "Journal", "tab_journal.svg", services.GetRequiredService<JournalPage>));
        tabs.Items.Add(Tab("Statistics", "Statistics", "tab_statistics.svg", services.GetRequiredService<StatisticsPage>));
        tabs.Items.Add(Tab("Profile", "Profile", "tab_profile.svg", services.GetRequiredService<ProfilePage>));
        Items.Add(tabs);
    }

    private static ShellContent Tab(string title, string route, string iconFile, Func<Page> pageFactory)
    {
        return new ShellContent
        {
            Title = title,
            Route = route,
            // MIGRATION: The Flutter/KMP passes showed placeholder icon text is
            //            visually wrong. MAUI uses image-backed Shell icons so
            //            tab items are symbols, not words like "Moon"/"Doc".
            Icon = ImageSource.FromFile(iconFile),
            ContentTemplate = new DataTemplate(pageFactory)
        };
    }
}
