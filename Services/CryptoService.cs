using System.Security.Cryptography;
using System.Text;
using SleepTrackerMaui.Models;

namespace SleepTrackerMaui.Services;

public interface ICryptoService
{
    Task<string> EncryptAsync(string plainText);
    Task<string> DecryptAsync(string encryptedText);
    Task<JournalPatch> EncryptJournalPatchAsync(JournalPatch patch);
    Task<JournalData> DecryptJournalAsync(JournalData journal);
}

public sealed class CryptoService(ISecureKeyValueStore store) : ICryptoService
{
    private const string EncryptionKeyName = "myAppEncryptionKey";
    private const string CompatibilitySalt = "sleeptracker-aes-v1-salt";
    private const int CompatibilityIterations = 10_000;

    public async Task<string> EncryptAsync(string plainText)
    {
        if (string.IsNullOrEmpty(plainText) || (AppConfig.InDemoMode && !AppConfig.EncryptedAtRest))
        {
            return plainText;
        }

        byte[] key = await GetAesKeyAsync();
        byte[] iv = RandomNumberGenerator.GetBytes(16);
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] cipher = encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(plainText), 0, Encoding.UTF8.GetByteCount(plainText));
        return $"{Convert.ToBase64String(iv)}:{Convert.ToBase64String(cipher)}";
    }

    public async Task<string> DecryptAsync(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText) || (AppConfig.InDemoMode && !AppConfig.EncryptedAtRest))
        {
            return encryptedText;
        }

        string[] parts = encryptedText.Split(':');
        if (parts.Length != 2)
        {
            // MIGRATION_FLAG: Existing local demo records may be plaintext
            //                 because Demo Mode can disable encryption.
            return encryptedText;
        }

        byte[] key = await GetAesKeyAsync();
        byte[] iv = Convert.FromBase64String(parts[0]);
        byte[] cipher = Convert.FromBase64String(parts[1]);
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using ICryptoTransform decryptor = aes.CreateDecryptor();
        byte[] plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }

    public async Task<JournalPatch> EncryptJournalPatchAsync(JournalPatch patch)
    {
        // MIGRATION: The RN service only encrypts sensitive journal fields.
        //            MAUI keeps the same field boundary so schema-compatible
        //            SQLite rows can be decrypted by matching key material.
        return patch with
        {
            Bedtime = patch.Bedtime is null ? null : await EncryptAsync(patch.Bedtime),
            AlarmTime = patch.AlarmTime is null ? null : await EncryptAsync(patch.AlarmTime),
            SleepDuration = patch.SleepDuration is null ? null : await EncryptAsync(patch.SleepDuration),
            DiaryEntry = patch.DiaryEntry is null ? null : await EncryptAsync(patch.DiaryEntry)
        };
    }

    public async Task<JournalData> DecryptJournalAsync(JournalData journal)
    {
        return journal with
        {
            Bedtime = await DecryptAsync(journal.Bedtime),
            AlarmTime = await DecryptAsync(journal.AlarmTime),
            SleepDuration = await DecryptAsync(journal.SleepDuration),
            DiaryEntry = await DecryptAsync(journal.DiaryEntry)
        };
    }

    private async Task<byte[]> GetAesKeyAsync()
    {
        string? stored = await store.ReadAsync(EncryptionKeyName);
        if (string.IsNullOrWhiteSpace(stored))
        {
            stored = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            await store.WriteAsync(EncryptionKeyName, stored);
        }

        // MIGRATION: Prompt requires PBKDF2 compatibility. The stored 256-bit
        //            seed is stretched with the same salt/iteration constants
        //            used in the earlier Dart/KMP migration to keep encrypted
        //            rows readable across migrated builds.
        using Rfc2898DeriveBytes pbkdf2 = new(
            Encoding.UTF8.GetBytes(stored),
            Encoding.UTF8.GetBytes(CompatibilitySalt),
            CompatibilityIterations,
            HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32);
    }
}
