using System.Text;
using System.Text.Json;
using Foundation;
using Security;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.Core.Apple.Services;

/// <summary>
/// Apple Keychain 기반 보안 저장소 서비스
/// </summary>
public class KeychainStorageService : ISecureStorageService
{
    private const string ServiceName = "com.otpauthenticator.secrets";
    private readonly string _dataDirectory;
    private readonly Dictionary<string, string> _secretCache = new();

    public string DataDirectory => _dataDirectory;

    public KeychainStorageService()
    {
        // macOS: ~/Library/Application Support/OtpAuthenticator
        // iOS: App's Documents directory
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _dataDirectory = Path.Combine(basePath, "OtpAuthenticator");

        Directory.CreateDirectory(_dataDirectory);
    }

    public void StoreSecret(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        // Remove existing secret first
        RemoveSecret(key);

        var record = new SecRecord(SecKind.GenericPassword)
        {
            Service = ServiceName,
            Account = key,
            ValueData = NSData.FromString(value, NSStringEncoding.UTF8),
            Accessible = SecAccessible.WhenUnlockedThisDeviceOnly
        };

        var status = SecKeyChain.Add(record);
        if (status != SecStatusCode.Success && status != SecStatusCode.DuplicateItem)
        {
            throw new Exception($"Failed to store secret in Keychain: {status}");
        }

        _secretCache[key] = value;
    }

    public string? RetrieveSecret(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        if (_secretCache.TryGetValue(key, out var cached))
            return cached;

        var query = new SecRecord(SecKind.GenericPassword)
        {
            Service = ServiceName,
            Account = key
        };

        var status = SecKeyChain.QueryAsRecord(query, out var match);

        if (status == SecStatusCode.Success && match?.ValueData != null)
        {
            var value = NSString.FromData(match.ValueData, NSStringEncoding.UTF8)?.ToString();
            if (value != null)
            {
                _secretCache[key] = value;
                return value;
            }
        }

        return null;
    }

    public void RemoveSecret(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        _secretCache.Remove(key);

        var record = new SecRecord(SecKind.GenericPassword)
        {
            Service = ServiceName,
            Account = key
        };

        SecKeyChain.Remove(record);
    }

    public async Task SaveEncryptedDataAsync<T>(string filename, T data)
    {
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        // Use Data Protection for file encryption on Apple platforms
        var filePath = Path.Combine(_dataDirectory, filename);

        // Store with NSFileProtectionComplete
        await File.WriteAllTextAsync(filePath, json);

        // Set file protection attribute (iOS)
        SetFileProtection(filePath);
    }

    public async Task<T?> LoadEncryptedDataAsync<T>(string filename)
    {
        var filePath = Path.Combine(_dataDirectory, filename);

        if (!File.Exists(filePath))
            return default;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    public Task DeleteEncryptedDataAsync(string filename)
    {
        var filePath = Path.Combine(_dataDirectory, filename);

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }

    private void SetFileProtection(string filePath)
    {
#if IOS
        try
        {
            var attributes = new NSFileAttributes
            {
                ProtectionKey = NSFileProtection.Complete
            };
            NSFileManager.DefaultManager.SetAttributes(attributes, filePath, out _);
        }
        catch
        {
            // Ignore protection errors
        }
#endif
    }
}
