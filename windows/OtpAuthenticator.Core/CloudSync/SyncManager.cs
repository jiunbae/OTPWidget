using System.Text;
using System.Text.Json;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.Core.CloudSync;

/// <summary>
/// 클라우드 동기화 관리자
/// </summary>
public class SyncManager : ICloudSyncService
{
    private const string BackupFileName = "backup.otp";

    private readonly Dictionary<CloudProvider, ICloudProvider> _providers;
    private readonly IAccountRepository _accountRepository;
    private readonly ISettingsService _settingsService;
    private readonly IEncryptionService _encryptionService;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    /// <summary>
    /// 동기화 상태 변경 이벤트
    /// </summary>
    public event EventHandler<SyncEventArgs>? SyncStatusChanged;

    public SyncManager(
        IAccountRepository accountRepository,
        ISettingsService settingsService,
        IEncryptionService encryptionService)
    {
        _accountRepository = accountRepository;
        _settingsService = settingsService;
        _encryptionService = encryptionService;

        // 프로바이더 등록
        _providers = new Dictionary<CloudProvider, ICloudProvider>
        {
            { CloudProvider.OneDrive, new OneDriveProvider() },
            { CloudProvider.GoogleDrive, new GoogleDriveProvider() }
        };
    }

    /// <summary>
    /// 현재 프로바이더
    /// </summary>
    public ICloudProvider? CurrentProvider
    {
        get
        {
            var provider = _settingsService.Settings.CloudSync.Provider;
            return provider != CloudProvider.None && _providers.ContainsKey(provider)
                ? _providers[provider]
                : null;
        }
    }

    /// <summary>
    /// 동기화 수행
    /// </summary>
    public async Task<SyncResult> SyncAsync(string? encryptionPassword = null)
    {
        var settings = _settingsService.Settings.CloudSync;

        if (!settings.Enabled || settings.Provider == CloudProvider.None)
            return SyncResult.ProviderNotFound;

        if (!_providers.TryGetValue(settings.Provider, out var provider))
            return SyncResult.ProviderNotFound;

        await _syncLock.WaitAsync();

        try
        {
            OnSyncStatusChanged(SyncStatus.Syncing, "Connecting...");

            // 인증 확인
            if (!await provider.IsAuthenticatedAsync())
            {
                OnSyncStatusChanged(SyncStatus.Syncing, "Authenticating...");

                if (!await provider.AuthenticateAsync())
                {
                    OnSyncStatusChanged(SyncStatus.Error, "Authentication failed");
                    return SyncResult.AuthenticationFailed;
                }
            }

            OnSyncStatusChanged(SyncStatus.Syncing, "Checking for changes...");

            // 원격 파일 정보 조회
            var remoteFile = await provider.GetFileInfoAsync(BackupFileName);
            var localModified = await GetLocalModifiedTimeAsync();

            // 동기화 방향 결정
            if (remoteFile == null)
            {
                // 원격에 파일 없음 - 업로드
                OnSyncStatusChanged(SyncStatus.Syncing, "Uploading...");
                await UploadAsync(provider, encryptionPassword);
            }
            else if (localModified == null || remoteFile.ModifiedTime > localModified)
            {
                // 원격이 더 최신 - 다운로드
                OnSyncStatusChanged(SyncStatus.Syncing, "Downloading...");
                await DownloadAsync(provider, remoteFile.Id, encryptionPassword);
            }
            else if (localModified > remoteFile.ModifiedTime)
            {
                // 로컬이 더 최신 - 업로드
                OnSyncStatusChanged(SyncStatus.Syncing, "Uploading...");
                await UploadAsync(provider, encryptionPassword);
            }
            else
            {
                // 동일 - 변경 없음
                OnSyncStatusChanged(SyncStatus.Completed, "Already up to date");
            }

            // 마지막 동기화 시간 업데이트
            _settingsService.Settings.CloudSync.LastSyncTime = DateTime.UtcNow;
            await _settingsService.SaveAsync();

            OnSyncStatusChanged(SyncStatus.Completed, "Sync completed");
            return SyncResult.Success;
        }
        catch (Exception ex)
        {
            OnSyncStatusChanged(SyncStatus.Error, ex.Message);
            return SyncResult.Error;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// 강제 업로드
    /// </summary>
    public async Task<SyncResult> ForceUploadAsync(string? encryptionPassword = null)
    {
        var provider = CurrentProvider;
        if (provider == null) return SyncResult.ProviderNotFound;

        await _syncLock.WaitAsync();

        try
        {
            if (!await provider.IsAuthenticatedAsync())
            {
                if (!await provider.AuthenticateAsync())
                    return SyncResult.AuthenticationFailed;
            }

            OnSyncStatusChanged(SyncStatus.Syncing, "Force uploading...");
            await UploadAsync(provider, encryptionPassword);

            _settingsService.Settings.CloudSync.LastSyncTime = DateTime.UtcNow;
            await _settingsService.SaveAsync();

            OnSyncStatusChanged(SyncStatus.Completed, "Upload completed");
            return SyncResult.Success;
        }
        catch (Exception ex)
        {
            OnSyncStatusChanged(SyncStatus.Error, ex.Message);
            return SyncResult.Error;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// 강제 다운로드
    /// </summary>
    public async Task<SyncResult> ForceDownloadAsync(string? encryptionPassword = null)
    {
        var provider = CurrentProvider;
        if (provider == null) return SyncResult.ProviderNotFound;

        await _syncLock.WaitAsync();

        try
        {
            if (!await provider.IsAuthenticatedAsync())
            {
                if (!await provider.AuthenticateAsync())
                    return SyncResult.AuthenticationFailed;
            }

            var remoteFile = await provider.GetFileInfoAsync(BackupFileName);
            if (remoteFile == null)
            {
                OnSyncStatusChanged(SyncStatus.Error, "No backup found in cloud");
                return SyncResult.Error;
            }

            OnSyncStatusChanged(SyncStatus.Syncing, "Force downloading...");
            await DownloadAsync(provider, remoteFile.Id, encryptionPassword);

            _settingsService.Settings.CloudSync.LastSyncTime = DateTime.UtcNow;
            await _settingsService.SaveAsync();

            OnSyncStatusChanged(SyncStatus.Completed, "Download completed");
            return SyncResult.Success;
        }
        catch (Exception ex)
        {
            OnSyncStatusChanged(SyncStatus.Error, ex.Message);
            return SyncResult.Error;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// 업로드
    /// </summary>
    private async Task UploadAsync(ICloudProvider provider, string? password)
    {
        var accounts = await _accountRepository.GetAllAsync();

        var backupData = new BackupData
        {
            Version = "1.0",
            CreatedAt = DateTime.UtcNow,
            DeviceName = Environment.MachineName,
            Accounts = accounts.ToList()
        };

        // 체크섬 계산
        var accountsJson = JsonSerializer.Serialize(backupData.Accounts);
        backupData.Checksum = _encryptionService.ComputeSha256Hash(accountsJson);

        string json = JsonSerializer.Serialize(backupData);
        byte[] data;

        if (!string.IsNullOrEmpty(password))
        {
            // 암호화
            var encrypted = _encryptionService.Encrypt(json, password);
            var encryptedJson = JsonSerializer.Serialize(encrypted);
            data = Encoding.UTF8.GetBytes(encryptedJson);
        }
        else
        {
            data = Encoding.UTF8.GetBytes(json);
        }

        await provider.UploadAsync(BackupFileName, data);
    }

    /// <summary>
    /// 다운로드
    /// </summary>
    private async Task DownloadAsync(ICloudProvider provider, string fileId, string? password)
    {
        var data = await provider.DownloadAsync(fileId);
        if (data == null)
            throw new Exception("Failed to download backup");

        string json = Encoding.UTF8.GetString(data);
        BackupData? backupData;

        // 암호화된 백업인지 확인
        try
        {
            var encrypted = JsonSerializer.Deserialize<EncryptedBackup>(json);
            if (encrypted != null && !string.IsNullOrEmpty(encrypted.Data))
            {
                if (string.IsNullOrEmpty(password))
                    throw new Exception("Backup is encrypted. Password required.");

                var decrypted = _encryptionService.Decrypt(encrypted, password);
                backupData = JsonSerializer.Deserialize<BackupData>(decrypted);
            }
            else
            {
                backupData = JsonSerializer.Deserialize<BackupData>(json);
            }
        }
        catch (JsonException)
        {
            backupData = JsonSerializer.Deserialize<BackupData>(json);
        }

        if (backupData == null)
            throw new Exception("Invalid backup data");

        // 계정 복원 (기존 계정 유지, 새 계정 추가)
        var existingAccounts = await _accountRepository.GetAllAsync();
        var existingKeys = existingAccounts
            .Select(a => $"{a.Issuer}:{a.AccountName}:{a.SecretKey}")
            .ToHashSet();

        foreach (var account in backupData.Accounts)
        {
            var key = $"{account.Issuer}:{account.AccountName}:{account.SecretKey}";
            if (!existingKeys.Contains(key))
            {
                account.Id = Guid.NewGuid();
                await _accountRepository.AddAsync(account);
            }
        }
    }

    /// <summary>
    /// 로컬 수정 시간 조회
    /// </summary>
    private Task<DateTime?> GetLocalModifiedTimeAsync()
    {
        return Task.FromResult(_settingsService.Settings.CloudSync.LastSyncTime);
    }

    /// <summary>
    /// 상태 변경 이벤트 발생
    /// </summary>
    private void OnSyncStatusChanged(SyncStatus status, string message)
    {
        SyncStatusChanged?.Invoke(this, new SyncEventArgs(status, message));
    }
}

/// <summary>
/// 클라우드 동기화 서비스 인터페이스
/// </summary>
public interface ICloudSyncService
{
    event EventHandler<SyncEventArgs>? SyncStatusChanged;
    Task<SyncResult> SyncAsync(string? encryptionPassword = null);
    Task<SyncResult> ForceUploadAsync(string? encryptionPassword = null);
    Task<SyncResult> ForceDownloadAsync(string? encryptionPassword = null);
}

/// <summary>
/// 동기화 이벤트 인자
/// </summary>
public class SyncEventArgs : EventArgs
{
    public SyncStatus Status { get; }
    public string Message { get; }

    public SyncEventArgs(SyncStatus status, string message)
    {
        Status = status;
        Message = message;
    }
}
