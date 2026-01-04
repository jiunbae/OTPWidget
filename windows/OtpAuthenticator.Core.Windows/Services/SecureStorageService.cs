using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.Core.Windows.Services;

/// <summary>
/// Windows 보안 저장소 서비스 (PasswordVault + DPAPI)
/// </summary>
public class SecureStorageService : ISecureStorageService
{
    private const string ResourceName = "OtpAuthenticator";
    private readonly string _dataDirectory;
    private readonly Dictionary<string, string> _secretCache = new();

    public string DataDirectory => _dataDirectory;

    public SecureStorageService()
    {
        _dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OtpAuthenticator");

        Directory.CreateDirectory(_dataDirectory);
        Directory.CreateDirectory(Path.Combine(_dataDirectory, "secrets"));
    }

    public void StoreSecret(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        try
        {
            StoreInPasswordVault(key, value);
        }
        catch
        {
            StoreWithDpapi(key, value);
        }

        _secretCache[key] = value;
    }

    public string? RetrieveSecret(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        if (_secretCache.TryGetValue(key, out var cached))
            return cached;

        try
        {
            var value = RetrieveFromPasswordVault(key);
            if (value != null)
            {
                _secretCache[key] = value;
                return value;
            }
        }
        catch { }

        try
        {
            var value = RetrieveWithDpapi(key);
            if (value != null)
            {
                _secretCache[key] = value;
                return value;
            }
        }
        catch { }

        return null;
    }

    public void RemoveSecret(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        _secretCache.Remove(key);

        try { RemoveFromPasswordVault(key); } catch { }
        try { RemoveWithDpapi(key); } catch { }
    }

    public async Task SaveEncryptedDataAsync<T>(string filename, T data)
    {
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        byte[] plainBytes = Encoding.UTF8.GetBytes(json);
        byte[] encryptedBytes = ProtectedData.Protect(
            plainBytes,
            null,
            DataProtectionScope.CurrentUser);

        string filePath = Path.Combine(_dataDirectory, filename);
        await File.WriteAllBytesAsync(filePath, encryptedBytes);
    }

    public async Task<T?> LoadEncryptedDataAsync<T>(string filename)
    {
        string filePath = Path.Combine(_dataDirectory, filename);

        if (!File.Exists(filePath))
            return default;

        try
        {
            byte[] encryptedBytes = await File.ReadAllBytesAsync(filePath);
            byte[] plainBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser);

            string json = Encoding.UTF8.GetString(plainBytes);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    public Task DeleteEncryptedDataAsync(string filename)
    {
        string filePath = Path.Combine(_dataDirectory, filename);

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }

    #region PasswordVault Operations

    private void StoreInPasswordVault(string key, string value)
    {
        var vault = new global::Windows.Security.Credentials.PasswordVault();

        try
        {
            var existing = vault.Retrieve(ResourceName, key);
            vault.Remove(existing);
        }
        catch { }

        var credential = new global::Windows.Security.Credentials.PasswordCredential(
            ResourceName, key, value);
        vault.Add(credential);
    }

    private string? RetrieveFromPasswordVault(string key)
    {
        var vault = new global::Windows.Security.Credentials.PasswordVault();

        try
        {
            var credential = vault.Retrieve(ResourceName, key);
            credential.RetrievePassword();
            return credential.Password;
        }
        catch
        {
            return null;
        }
    }

    private void RemoveFromPasswordVault(string key)
    {
        var vault = new global::Windows.Security.Credentials.PasswordVault();

        try
        {
            var credential = vault.Retrieve(ResourceName, key);
            vault.Remove(credential);
        }
        catch { }
    }

    #endregion

    #region DPAPI Fallback

    private void StoreWithDpapi(string key, string value)
    {
        byte[] plainBytes = Encoding.UTF8.GetBytes(value);
        byte[] encryptedBytes = ProtectedData.Protect(
            plainBytes,
            Encoding.UTF8.GetBytes(key),
            DataProtectionScope.CurrentUser);

        string filePath = GetSecretFilePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllBytes(filePath, encryptedBytes);
    }

    private string? RetrieveWithDpapi(string key)
    {
        string filePath = GetSecretFilePath(key);

        if (!File.Exists(filePath))
            return null;

        byte[] encryptedBytes = File.ReadAllBytes(filePath);
        byte[] plainBytes = ProtectedData.Unprotect(
            encryptedBytes,
            Encoding.UTF8.GetBytes(key),
            DataProtectionScope.CurrentUser);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private void RemoveWithDpapi(string key)
    {
        string filePath = GetSecretFilePath(key);

        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    private string GetSecretFilePath(string key)
    {
        string safeKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(key))
            .Replace('/', '_')
            .Replace('+', '-');

        return Path.Combine(_dataDirectory, "secrets", $"{safeKey}.dat");
    }

    #endregion
}
