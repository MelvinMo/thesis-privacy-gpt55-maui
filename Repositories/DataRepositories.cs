using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using SleepTrackerMaui.Models;
using SleepTrackerMaui.Services;
using SleepTrackerMaui.Stores;

namespace SleepTrackerMaui.Repositories;

public sealed class LocalDatabase
{
    private readonly string _databasePath = Path.Combine(FileSystem.AppDataDirectory, "sleeptracker_data.db");
    private bool _initialized;

    public async Task<SqliteConnection> OpenAsync()
    {
        SqliteConnection connection = new($"Data Source={_databasePath}");
        await connection.OpenAsync();
        if (!_initialized)
        {
            await CreateTablesAsync(connection);
            _initialized = true;
        }
        return connection;
    }

    private static async Task CreateTablesAsync(SqliteConnection connection)
    {
        string journalSql = """
            CREATE TABLE IF NOT EXISTS journals (
                journalId TEXT PRIMARY KEY NOT NULL,
                userId TEXT NOT NULL,
                date TEXT NOT NULL,
                bedtime TEXT,
                alarmTime TEXT,
                sleepDuration TEXT,
                diaryEntry TEXT,
                sleepNotes TEXT,
                createdAt TEXT NOT NULL DEFAULT (STRFTIME('%Y-%m-%dT%H:%M:%fZ', 'NOW'))
            );
            """;

        string sensorSql = """
            CREATE TABLE IF NOT EXISTS sensor_data (
                id TEXT PRIMARY KEY NOT NULL,
                userId TEXT NOT NULL,
                timestamp INTEGER NOT NULL,
                date TEXT NOT NULL,
                sensorType TEXT NOT NULL,
                averageDecibels TEXT,
                peakDecibels TEXT,
                frequencyBands TEXT,
                audioClipUri TEXT,
                snoreDetected INTEGER,
                ambientNoiseLevel TEXT,
                illuminance TEXT,
                lightLevel TEXT,
                x TEXT,
                y TEXT,
                z TEXT,
                magnitude TEXT,
                movementIntensity TEXT,
                createdAt TEXT NOT NULL DEFAULT (STRFTIME('%Y-%m-%dT%H:%M:%fZ', 'NOW'))
            );
            """;

        // MIGRATION: Table names and columns are byte-for-byte aligned with
        //            Expo SQLite so local data can survive framework migration.
        await using SqliteCommand journal = connection.CreateCommand();
        journal.CommandText = journalSql;
        await journal.ExecuteNonQueryAsync();

        await using SqliteCommand sensor = connection.CreateCommand();
        sensor.CommandText = sensorSql;
        await sensor.ExecuteNonQueryAsync();
    }
}

public interface IAuthRepository
{
    User? CurrentUser { get; }
    string? Token { get; }
    bool IsAuthenticated { get; }
    Task CheckAuthAsync();
    Task LoginAsync(string email, string password);
    Task RegisterAsync(string firstName, string lastName, string email, string password);
    Task LogoutAsync();
}

public sealed class AuthRepository(ISecureKeyValueStore secureStore, HttpClient httpClient) : IAuthRepository
{
    private const string UserKey = "authUser";
    private const string TokenKey = "authToken";

    public User? CurrentUser { get; private set; }
    public string? Token { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null && !string.IsNullOrWhiteSpace(Token);

    public async Task CheckAuthAsync()
    {
        string userJson = Preferences.Default.Get(UserKey, string.Empty);
        string? token = await secureStore.ReadAsync(TokenKey);
        if (string.IsNullOrWhiteSpace(userJson) || string.IsNullOrWhiteSpace(token) || token == "demo-token")
        {
            // MIGRATION: Previous migration feedback exposed accidental fake
            //            auth. MAUI clears stale/fake tokens and never treats
            //            local data alone as authentication.
            await LogoutAsync();
            return;
        }

        CurrentUser = JsonSerializer.Deserialize<User>(userJson, AppJson.Options);
        Token = token;
    }

    public async Task LoginAsync(string email, string password)
    {
        await LogoutAsync();
        AuthResponse response = await PostAuthAsync("auth/login", new AuthLoginRequest(email, password));
        await PersistAuthAsync(response);
    }

    public async Task RegisterAsync(string firstName, string lastName, string email, string password)
    {
        await LogoutAsync();
        AuthResponse response = await PostAuthAsync("auth/register", new AuthRegisterRequest(firstName, lastName, email, password));
        await PersistAuthAsync(response);
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        Token = null;
        Preferences.Default.Remove(UserKey);
        await secureStore.DeleteAsync(TokenKey);
    }

    private async Task<AuthResponse> PostAuthAsync<TBody>(string path, TBody body)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(path, body, AppJson.Options);
        string raw = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            ApiErrorResponse? apiError = TryDeserialize<ApiErrorResponse>(raw);
            throw new InvalidOperationException(apiError?.Message ?? $"API request failed ({(int)response.StatusCode})");
        }

        AuthResponse auth = TryDeserialize<AuthResponse>(raw)
            ?? throw new InvalidOperationException("Authentication response was not JSON.");
        if (auth.User is null || string.IsNullOrWhiteSpace(auth.User.UserId) || string.IsNullOrWhiteSpace(auth.Token))
        {
            // MIGRATION: Auth succeeds only with backend user + JWT. The app
            //            never creates a local fallback user for malformed 2xx.
            throw new InvalidOperationException("Authentication response missing user or token.");
        }
        return auth;
    }

    private async Task PersistAuthAsync(AuthResponse response)
    {
        CurrentUser = response.User;
        Token = response.Token;
        Preferences.Default.Set(UserKey, JsonSerializer.Serialize(response.User, AppJson.Options));
        await secureStore.WriteAsync(TokenKey, response.Token);
    }

    private static T? TryDeserialize<T>(string raw)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(raw, AppJson.Options);
        }
        catch (JsonException)
        {
            // MIGRATION_FLAG: Dev backend can return HTML fallback pages. We
            //                 normalize that to an API failure instead of
            //                 showing raw <!DOCTYPE html> inside the UI.
            return default;
        }
    }
}

public interface IProfileRepository
{
    Task<UserConsentPreferences> GetPreferencesAsync();
    Task SavePreferencesAsync(UserConsentPreferences preferences);
    bool PrivacyOnboardingComplete { get; set; }
    bool AppOnboardingComplete { get; set; }
}

public sealed class ProfileRepository : IProfileRepository
{
    private const string PreferencesKey = "userConsentPreferences";
    private const string PrivacyCompleteKey = "hasCompletedPrivacyOnboarding";
    private const string AppCompleteKey = "hasCompletedAppOnboarding";

    public Task<UserConsentPreferences> GetPreferencesAsync()
    {
        string raw = Preferences.Default.Get(PreferencesKey, string.Empty);
        UserConsentPreferences preferences = string.IsNullOrWhiteSpace(raw)
            ? UserConsentPreferences.Default
            : JsonSerializer.Deserialize<UserConsentPreferences>(raw, AppJson.Options) ?? UserConsentPreferences.Default;
        return Task.FromResult(preferences);
    }

    public Task SavePreferencesAsync(UserConsentPreferences preferences)
    {
        Preferences.Default.Set(PreferencesKey, JsonSerializer.Serialize(preferences, AppJson.Options));
        return Task.CompletedTask;
    }

    public bool PrivacyOnboardingComplete
    {
        get => Preferences.Default.Get(PrivacyCompleteKey, false);
        set => Preferences.Default.Set(PrivacyCompleteKey, value);
    }

    public bool AppOnboardingComplete
    {
        get => Preferences.Default.Get(AppCompleteKey, false);
        set => Preferences.Default.Set(AppCompleteKey, value);
    }
}

public interface IJournalRepository
{
    Task<JournalData?> GetJournalByDateAsync(string date);
    Task<JournalData?> EditJournalAsync(string date, JournalPatch patch);
}

public sealed class JournalRepository(
    LocalDatabase database,
    ICryptoService crypto,
    IAuthRepository auth,
    TransparencyStore transparencyStore) : IJournalRepository
{
    public async Task<JournalData?> GetJournalByDateAsync(string date)
    {
        User user = auth.CurrentUser ?? throw new InvalidOperationException("User is not authenticated. Please log in first.");
        await using SqliteConnection connection = await database.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT journalId, userId, date, bedtime, alarmTime, sleepDuration, diaryEntry, sleepNotes
            FROM journals
            WHERE userId = $userId AND date = $date
            """;
        command.Parameters.AddWithValue("$userId", user.UserId);
        command.Parameters.AddWithValue("$date", date);
        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        JournalData encrypted = MapJournal(reader);
        return await crypto.DecryptJournalAsync(encrypted);
    }

    public async Task<JournalData?> EditJournalAsync(string date, JournalPatch patch)
    {
        User user = auth.CurrentUser ?? throw new InvalidOperationException("User is not authenticated. Please log in first.");
        JournalPatch encryptedPatch = await crypto.EncryptJournalPatchAsync(patch);
        await using SqliteConnection connection = await database.OpenAsync();
        JournalData? existing = await GetJournalByDateAsync(date);

        if (existing is null)
        {
            string journalId = Guid.NewGuid().ToString();
            await using SqliteCommand insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT INTO journals
                (journalId, userId, date, bedtime, alarmTime, sleepDuration, diaryEntry, sleepNotes, createdAt)
                VALUES ($journalId, $userId, $date, $bedtime, $alarmTime, $sleepDuration, $diaryEntry, $sleepNotes, $createdAt)
                """;
            AddJournalParameters(insert, journalId, user.UserId, encryptedPatch.Date ?? date, encryptedPatch);
            await insert.ExecuteNonQueryAsync();
        }
        else
        {
            await using SqliteCommand update = connection.CreateCommand();
            update.CommandText = """
                UPDATE journals
                SET bedtime = COALESCE($bedtime, bedtime),
                    alarmTime = COALESCE($alarmTime, alarmTime),
                    sleepDuration = COALESCE($sleepDuration, sleepDuration),
                    diaryEntry = COALESCE($diaryEntry, diaryEntry),
                    sleepNotes = COALESCE($sleepNotes, sleepNotes)
                WHERE userId = $userId AND date = $date
                """;
            update.Parameters.AddWithValue("$bedtime", (object?)encryptedPatch.Bedtime ?? DBNull.Value);
            update.Parameters.AddWithValue("$alarmTime", (object?)encryptedPatch.AlarmTime ?? DBNull.Value);
            update.Parameters.AddWithValue("$sleepDuration", (object?)encryptedPatch.SleepDuration ?? DBNull.Value);
            update.Parameters.AddWithValue("$diaryEntry", (object?)encryptedPatch.DiaryEntry ?? DBNull.Value);
            update.Parameters.AddWithValue("$sleepNotes", encryptedPatch.SleepNotes is null ? DBNull.Value : JsonSerializer.Serialize(encryptedPatch.SleepNotes, AppJson.Options));
            update.Parameters.AddWithValue("$userId", user.UserId);
            update.Parameters.AddWithValue("$date", date);
            await update.ExecuteNonQueryAsync();
        }

        await transparencyStore.SetJournalAsync(transparencyStore.Journal with { StorageLocation = DataDestination.SQLITE_DB });
        return await GetJournalByDateAsync(date);
    }

    private static void AddJournalParameters(SqliteCommand command, string journalId, string userId, string date, JournalPatch patch)
    {
        command.Parameters.AddWithValue("$journalId", journalId);
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$date", date);
        command.Parameters.AddWithValue("$bedtime", (object?)patch.Bedtime ?? DBNull.Value);
        command.Parameters.AddWithValue("$alarmTime", (object?)patch.AlarmTime ?? DBNull.Value);
        command.Parameters.AddWithValue("$sleepDuration", (object?)patch.SleepDuration ?? DBNull.Value);
        command.Parameters.AddWithValue("$diaryEntry", (object?)patch.DiaryEntry ?? DBNull.Value);
        command.Parameters.AddWithValue("$sleepNotes", patch.SleepNotes is null ? DBNull.Value : JsonSerializer.Serialize(patch.SleepNotes, AppJson.Options));
        command.Parameters.AddWithValue("$createdAt", DateTimeOffset.UtcNow.ToString("O"));
    }

    private static JournalData MapJournal(SqliteDataReader reader)
    {
        string sleepNotesJson = reader["sleepNotes"] as string ?? "[]";
        IReadOnlyList<SleepNote> notes = JsonSerializer.Deserialize<List<SleepNote>>(sleepNotesJson, AppJson.Options) ?? [];
        return new JournalData(
            Date: reader["date"] as string ?? string.Empty,
            UserId: reader["userId"] as string ?? string.Empty,
            JournalId: reader["journalId"] as string ?? string.Empty,
            Bedtime: reader["bedtime"] as string ?? string.Empty,
            AlarmTime: reader["alarmTime"] as string ?? string.Empty,
            SleepDuration: reader["sleepDuration"] as string ?? string.Empty,
            DiaryEntry: reader["diaryEntry"] as string ?? string.Empty,
            SleepNotes: notes);
    }
}

public interface ISensorRepository
{
    Task SaveAccelerometerSampleAsync(double x, double y, double z);
}

public sealed class SensorRepository(LocalDatabase database, IAuthRepository auth, TransparencyStore transparencyStore) : ISensorRepository
{
    public async Task SaveAccelerometerSampleAsync(double x, double y, double z)
    {
        if (auth.CurrentUser is null)
        {
            return;
        }

        double magnitude = Math.Sqrt(x * x + y * y + z * z);
        await using SqliteConnection connection = await database.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO sensor_data
            (id, userId, timestamp, date, sensorType, x, y, z, magnitude, movementIntensity, createdAt)
            VALUES ($id, $userId, $timestamp, $date, 'accelerometer', $x, $y, $z, $magnitude, $movementIntensity, $createdAt)
            """;
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("$userId", auth.CurrentUser.UserId);
        command.Parameters.AddWithValue("$timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        command.Parameters.AddWithValue("$date", DateTime.Today.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$x", x.ToString("0.###"));
        command.Parameters.AddWithValue("$y", y.ToString("0.###"));
        command.Parameters.AddWithValue("$z", z.ToString("0.###"));
        command.Parameters.AddWithValue("$magnitude", magnitude.ToString("0.###"));
        command.Parameters.AddWithValue("$movementIntensity", magnitude < 1.1 ? "still" : magnitude < 2 ? "light" : magnitude < 4 ? "moderate" : "active");
        command.Parameters.AddWithValue("$createdAt", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync();

        await transparencyStore.SetAccelerometerAsync(transparencyStore.Accelerometer with
        {
            StorageLocation = DataDestination.SQLITE_DB,
            BackgroundMode = true
        });
    }
}

public interface IGeneralSleepRepository
{
    Task<GeneralSleepData?> GetSleepDataAsync();
    Task<GeneralSleepData> SaveSleepDataAsync(string currentSleepDuration);
}

public sealed class GeneralSleepRepository(
    ISecureKeyValueStore secureStore,
    IAuthRepository auth,
    IProfileRepository profile,
    HttpClient httpClient,
    TransparencyStore transparencyStore) : IGeneralSleepRepository
{
    public async Task<GeneralSleepData?> GetSleepDataAsync()
    {
        User user = auth.CurrentUser ?? throw new InvalidOperationException("User is not authenticated. Please log in first.");
        UserConsentPreferences preferences = await profile.GetPreferencesAsync();
        if (preferences.CloudStorageEnabled)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "phi/generalSleep/");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            using HttpResponseMessage response = await httpClient.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            string raw = await response.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(raw);
            JsonElement sleepData = document.RootElement.GetProperty("sleepData");
            return sleepData.Deserialize<GeneralSleepData>(AppJson.Options);
        }

        string? local = await secureStore.ReadAsync($"sleepData_{user.UserId}");
        return string.IsNullOrWhiteSpace(local)
            ? null
            : JsonSerializer.Deserialize<GeneralSleepData>(local, AppJson.Options);
    }

    public async Task<GeneralSleepData> SaveSleepDataAsync(string currentSleepDuration)
    {
        User user = auth.CurrentUser ?? throw new InvalidOperationException("User is not authenticated. Please log in first.");
        GeneralSleepData existing = await GetSleepDataAsync() ?? new GeneralSleepData(user.UserId, string.Empty, string.Empty, string.Empty, string.Empty);
        GeneralSleepData updated = existing with
        {
            CurrentSleepDuration = string.IsNullOrWhiteSpace(currentSleepDuration) ? existing.CurrentSleepDuration : currentSleepDuration
        };

        UserConsentPreferences preferences = await profile.GetPreferencesAsync();
        if (preferences.CloudStorageEnabled)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, "phi/generalSleep/")
            {
                Content = JsonContent.Create(updated, options: AppJson.Options)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
            using HttpResponseMessage response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string raw = await response.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(raw);
            JsonElement sleepData = document.RootElement.GetProperty("sleepData");
            updated = sleepData.Deserialize<GeneralSleepData>(AppJson.Options) ?? updated;
        }
        else
        {
            // MIGRATION: Expo SecureStore was used for small general sleep
            //            profile data. MAUI SecureStorage preserves the same
            //            local-only behavior when cloud consent is disabled.
            await secureStore.WriteAsync($"sleepData_{user.UserId}", JsonSerializer.Serialize(updated, AppJson.Options));
        }

        await transparencyStore.SetSleepAsync(transparencyStore.Sleep with
        {
            StorageLocation = preferences.CloudStorageEnabled ? DataDestination.GOOGLE_CLOUD : DataDestination.SECURE_STORE,
            EncryptionMethod = AppConfig.EncryptedAtRest ? EncryptionMethod.AES_256 : EncryptionMethod.NONE
        });
        return updated;
    }
}
