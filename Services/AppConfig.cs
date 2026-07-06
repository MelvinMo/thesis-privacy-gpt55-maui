using SleepTrackerMaui.Models;

namespace SleepTrackerMaui.Services;

public static class AppConfig
{
    // MIGRATION: .NET HttpClient treats leading-slash request URIs as rooted
    //            at the host, so the base URL intentionally ends in /api/ and
    //            repositories use relative paths such as auth/login.
    public const string ApiUnencryptedUrl = "http://YOUR_LAN_IP:7000/api/";
    public const string ApiEncryptedUrl = "https://your-backend-host.example.com/api/";

    // MIGRATION: React Native used transparencyConfig to drive demo behavior.
    //            MAUI keeps the same flags so fake sensors/encryption toggles
    //            can be tested without rewriting the app.
    public const bool InDemoMode = true;
    public const bool CollectAudio = false;
    public const bool CollectLight = false;
    public const bool CollectAccelerometer = false;
    public const bool EncryptedAtRest = false;
    public const bool EncryptedInTransit = false;

    public static string ApiBaseUrl => InDemoMode && !EncryptedInTransit ? ApiUnencryptedUrl : ApiEncryptedUrl;
}

public static class AppColors
{
    public static readonly Color AppBackground = Color.FromArgb("#1A1A2E");
    public static readonly Color Accent = Color.FromArgb("#4A90D9");
    public static readonly Color GeneralBlue = Color.FromArgb("#39ACE7");
    public static readonly Color LightBlack = Color.FromArgb("#181719");
    public static readonly Color InputFieldBackground = Color.FromArgb("#5B5775");
    public static readonly Color InputFieldPlaceholder = Color.FromArgb("#AFA3BF");
    public static readonly Color TooltipGreen = Color.FromArgb("#E0FFDF");
    public static readonly Color TooltipYellow = Color.FromArgb("#FFFD86");
    public static readonly Color TooltipRed = Color.FromArgb("#FD8686");
    public static readonly Color LightGrey = Color.FromArgb("#888888");
    public static readonly Color HyperlinkBlue = Color.FromArgb("#4A90E2");
}

public static class TransparencyDefaults
{
    private static RegulatoryCompliance PipedaOk() => new(
        RegulatoryFramework.PIPEDA,
        true,
        string.Empty,
        Array.Empty<string>());

    public static TransparencyEvent Journal() => new(
        null,
        DataType.USER_JOURNAL,
        DataSource.USER_INPUT,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        PrivacyRisk.LOW,
        PipedaOk(),
        new AiExplanation(
            "To analyze how your daily mood, habits, sleep goals affects your sleep quality.",
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<string>(),
            Array.Empty<string>()));

    public static TransparencyEvent Light() => new(
        null,
        DataType.SENSOR_LIGHT,
        DataSource.LIGHT_SENSOR,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        PrivacyRisk.LOW,
        PipedaOk(),
        new AiExplanation(
            "To understand how the light conditions in your sleep environment may affect your sleep quality",
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<string>(),
            Array.Empty<string>()));

    public static TransparencyEvent Microphone() => new(
        null,
        DataType.SENSOR_AUDIO,
        DataSource.MICROPHONE,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        PrivacyRisk.LOW,
        PipedaOk(),
        new AiExplanation(
            "To analyze sleep disturbances such as snoring and talking, as well as understanding the noise level in your sleep environment",
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<string>(),
            Array.Empty<string>()));

    public static TransparencyEvent Accelerometer() => new(
        null,
        DataType.SENSOR_MOTION,
        DataSource.ACCELEROMETER,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        PrivacyRisk.LOW,
        PipedaOk(),
        new AiExplanation(
            "To analyze how your movements during sleep and throughout the day impact sleep quality",
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<string>(),
            Array.Empty<string>()));

    public static TransparencyEvent Sleep() => new(
        null,
        DataType.GENERAL_SLEEP,
        DataSource.USER_INPUT,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        PrivacyRisk.LOW,
        PipedaOk(),
        new AiExplanation(
            "To understand your current sleep quality and how we can improve it",
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<string>(),
            Array.Empty<string>()));

    public static TransparencyEvent Statistics() => new(
        null,
        DataType.SLEEP_STATISTICS,
        DataSource.DERIVED_DATA,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        PrivacyRisk.LOW,
        PipedaOk(),
        new AiExplanation(
            "Provide summaries and actionable insights to help improve your sleep quality",
            "This data is stored securely in your preferred storage location with encryption.",
            "No third parties have access to this data. Only you can view it through the app.",
            "No privacy risks",
            new[] { "derivedData" },
            new[] { "access" }));
}
