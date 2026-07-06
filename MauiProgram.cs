using Microsoft.Extensions.Logging;
using SleepTrackerMaui.Repositories;
using SleepTrackerMaui.Services;
using SleepTrackerMaui.Stores;
using SleepTrackerMaui.ViewModels;
using SleepTrackerMaui.Views;

namespace SleepTrackerMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("SpaceMono-Regular.ttf", "SpaceMono");
            });

        // MIGRATION: Expo module singletons and Zustand stores become MAUI DI
        //            services plus CommunityToolkit.Mvvm view models. This
        //            keeps repositories injectable and testable across pages.
        builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(AppConfig.ApiBaseUrl) });
        builder.Services.AddSingleton<LocalDatabase>();
        builder.Services.AddSingleton<ISecureKeyValueStore, SecureKeyValueStore>();
        builder.Services.AddSingleton<ICryptoService, CryptoService>();
        builder.Services.AddSingleton<TransparencyStore>();
        builder.Services.AddSingleton<IAuthRepository, AuthRepository>();
        builder.Services.AddSingleton<IProfileRepository, ProfileRepository>();
        builder.Services.AddSingleton<IJournalRepository, JournalRepository>();
        builder.Services.AddSingleton<ISensorRepository, SensorRepository>();
        builder.Services.AddSingleton<IDeviceSensorService, DeviceSensorService>();
        builder.Services.AddSingleton<IForegroundAccelerometerController, ForegroundAccelerometerController>();
        builder.Services.AddSingleton<IGeneralSleepRepository, GeneralSleepRepository>();

        builder.Services.AddSingleton<AuthViewModel>();
        builder.Services.AddSingleton<ProfileViewModel>();
        builder.Services.AddTransient<SleepViewModel>();
        builder.Services.AddTransient<JournalViewModel>();
        builder.Services.AddTransient<StatisticsViewModel>();
        builder.Services.AddTransient<OnboardingViewModel>();

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<AuthPage>();
        builder.Services.AddTransient<OnboardingPage>();
        builder.Services.AddTransient<SleepPage>();
        builder.Services.AddTransient<SleepModePage>();
        builder.Services.AddTransient<JournalPage>();
        builder.Services.AddTransient<StatisticsPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<ConsentPreferencesPage>();
        builder.Services.AddTransient<PrivacyPolicyPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
