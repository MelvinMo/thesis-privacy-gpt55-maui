using System.Text.Json.Serialization;

namespace SleepTrackerMaui.Models;

// MIGRATION: TypeScript string enums become C# enums decorated with
//            JsonStringEnumConverter so local persistence and API JSON keep
//            readable wire values without using dynamic/object bags.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DataType
{
    SENSOR_AUDIO,
    SENSOR_MOTION,
    SENSOR_LIGHT,
    USER_JOURNAL,
    USER_PROFILE,
    GENERAL_SLEEP,
    SLEEP_STATISTICS,
    DEVICE_INFO,
    LOCATION,
    USAGE_ANALYTICS
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DataSource
{
    MICROPHONE,
    ACCELEROMETER,
    LIGHT_SENSOR,
    USER_INPUT,
    DERIVED_DATA,
    SYSTEM_INFO
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DataDestination
{
    ASYNC_STORAGE,
    SECURE_STORE,
    SQLITE_DB,
    MEMORY,
    GOOGLE_CLOUD,
    THIRD_PARTY
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EncryptionMethod
{
    NONE,
    AES_256,
    JWT,
    DEVICE_KEYCHAIN
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PrivacyRisk
{
    LOW,
    MEDIUM,
    HIGH
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RegulatoryFramework
{
    PIPEDA,
    PHIPA,
    GDPR
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SleepNote
{
    Pain,
    Stress,
    Anxiety,
    Medication,
    Caffeine,
    Alcohol,
    WarmBath,
    HeavyMeal
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AmbientNoiseLevel
{
    quiet,
    moderate,
    loud,
    very_loud
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LightLevel
{
    dark,
    dim,
    moderate,
    bright
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MovementIntensity
{
    still,
    light,
    moderate,
    active
}

public sealed record RegulatoryCompliance(
    RegulatoryFramework Framework,
    bool Compliant,
    string Issues,
    IReadOnlyList<string> RelevantSections);

public sealed record AiExplanation(
    string Why,
    string Storage,
    string Access,
    string PrivacyExplanation,
    IReadOnlyList<string> PrivacyPolicyLink,
    IReadOnlyList<string> RegulationLink);

public sealed record TransparencyEvent(
    DateTimeOffset? Timestamp,
    DataType DataType,
    DataSource Source,
    string? SensorType,
    int? SamplingRate,
    int? Duration,
    EncryptionMethod? EncryptionMethod,
    DataDestination? StorageLocation,
    string? Endpoint,
    string? Protocol,
    bool? BackgroundMode,
    PrivacyRisk PrivacyRisk,
    RegulatoryCompliance RegulatoryCompliance,
    AiExplanation AiExplanation);

public sealed record User(
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    string Password = "");

public sealed record UserConsentPreferences(
    bool AccelerometerEnabled,
    bool LightSensorEnabled,
    bool MicrophoneEnabled,
    bool CloudStorageEnabled,
    bool AgreedToPrivacyPolicy,
    bool AnalyticsEnabled,
    bool MarketingCommunications,
    bool NotificationsEnabled)
{
    public static UserConsentPreferences Default { get; } = new(
        AccelerometerEnabled: false,
        LightSensorEnabled: false,
        MicrophoneEnabled: false,
        CloudStorageEnabled: false,
        AgreedToPrivacyPolicy: false,
        AnalyticsEnabled: false,
        MarketingCommunications: false,
        NotificationsEnabled: false);
}

public sealed record JournalData(
    string Date,
    string UserId,
    string JournalId,
    string Bedtime,
    string AlarmTime,
    string SleepDuration,
    string DiaryEntry,
    IReadOnlyList<SleepNote> SleepNotes);

public sealed record JournalPatch(
    string? Date = null,
    string? Bedtime = null,
    string? AlarmTime = null,
    string? SleepDuration = null,
    string? DiaryEntry = null,
    IReadOnlyList<SleepNote>? SleepNotes = null);

public sealed record GeneralSleepData(
    string UserId,
    string CurrentSleepDuration,
    string Snoring,
    string TirednessFrequency,
    string DaytimeSleepiness);

public sealed record FrequencyBands(string Low, string Mid, string High);

public abstract record SensorData(
    string Id,
    string UserId,
    string Timestamp,
    string Date,
    string SensorType);

public sealed record AudioSensorData(
    string Id,
    string UserId,
    string Timestamp,
    string Date,
    string AverageDecibels,
    string PeakDecibels,
    FrequencyBands FrequencyBands,
    string? AudioClipUri,
    bool SnoreDetected,
    AmbientNoiseLevel AmbientNoiseLevel)
    : SensorData(Id, UserId, Timestamp, Date, "audio");

public sealed record LightSensorData(
    string Id,
    string UserId,
    string Timestamp,
    string Date,
    string Illuminance,
    LightLevel LightLevel)
    : SensorData(Id, UserId, Timestamp, Date, "light");

public sealed record AccelerometerSensorData(
    string Id,
    string UserId,
    string Timestamp,
    string Date,
    string X,
    string Y,
    string Z,
    string Magnitude,
    MovementIntensity MovementIntensity)
    : SensorData(Id, UserId, Timestamp, Date, "accelerometer");

public sealed record AuthLoginRequest(string Email, string Password);

public sealed record AuthRegisterRequest(string FirstName, string LastName, string Email, string Password);

public sealed record AuthResponse(string Message, User User, string Token);

public sealed record ApiErrorResponse(string Message);

public sealed record PrivacyPolicyTocEntry(string Title, string SectionId);

public sealed record PrivacyPolicyContentItem(string Kind, string Text, string? Label = null, int Level = 0, string? Id = null);
