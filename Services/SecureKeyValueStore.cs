namespace SleepTrackerMaui.Services;

public interface ISecureKeyValueStore
{
    Task<string?> ReadAsync(string key);
    Task WriteAsync(string key, string value);
    Task DeleteAsync(string key);
}

public sealed class SecureKeyValueStore : ISecureKeyValueStore
{
    public Task<string?> ReadAsync(string key)
    {
        // MIGRATION: Expo SecureStore maps directly to MAUI SecureStorage,
        //            which wraps Android Keystore and iOS Keychain.
        return SecureStorage.Default.GetAsync(key);
    }

    public Task WriteAsync(string key, string value)
    {
        return SecureStorage.Default.SetAsync(key, value);
    }

    public Task DeleteAsync(string key)
    {
        SecureStorage.Default.Remove(key);
        return Task.CompletedTask;
    }
}
