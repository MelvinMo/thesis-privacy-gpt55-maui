using Android.App;
using Android.Content.PM;
using Android.OS;

namespace SleepTrackerMaui;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // MIGRATION: Expo controlled the system bars through the native shell.
        //            MAUI's Android template leaves a purple status bar unless
        //            we explicitly apply the source app's dark theme color.
#pragma warning disable CA1422
        // MIGRATION_FLAG: These setters are deprecated only on Android 15+.
        //                 The current test device is Android 12, and the
        //                 fallback keeps pre-Android-15 visual parity.
        Window?.SetStatusBarColor(Android.Graphics.Color.ParseColor("#1A1A2E"));
        Window?.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#1A1A2E"));
#pragma warning restore CA1422
    }
}
