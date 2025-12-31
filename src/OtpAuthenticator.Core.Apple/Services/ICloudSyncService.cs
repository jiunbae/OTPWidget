using Foundation;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;
using System.Text.Json;

namespace OtpAuthenticator.Core.Apple.Services;

/// <summary>
/// iCloud 동기화 서비스
/// </summary>
public class ICloudSyncService : ICloudProvider
{
    private readonly IEncryptionService _encryptionService;
    private readonly IAccountRepository _accountRepository;
    private readonly NSFileManager _fileManager;

    private const string BackupFileName = "otp_backup.dat";
    private const string ContainerIdentifier = "iCloud.com.otpauthenticator";

    public string ProviderName => "iCloud";

    public ICloudSyncService(
        IEncryptionService encryptionService,
        IAccountRepository accountRepository)
    {
        _encryptionService = encryptionService;
        _accountRepository = accountRepository;
        _fileManager = NSFileManager.DefaultManager;
    }

    /// <summary>
    /// iCloud 사용 가능 여부 확인
    /// </summary>
    public Task<bool> IsAvailableAsync()
    {
        var containerUrl = _fileManager.GetUrlForUbiquityContainer(ContainerIdentifier);
        return Task.FromResult(containerUrl != null);
    }

    /// <summary>
    /// 인증 (iCloud는 자동 인증)
    /// </summary>
    public Task<bool> AuthenticateAsync()
    {
        return IsAvailableAsync();
    }

    /// <summary>
    /// 백업 업로드
    /// </summary>
    public async Task<bool> UploadBackupAsync(string encryptedData)
    {
        try
        {
            var containerUrl = _fileManager.GetUrlForUbiquityContainer(ContainerIdentifier);
            if (containerUrl == null)
                return false;

            var documentsUrl = containerUrl.Append("Documents", true);
            var fileUrl = documentsUrl.Append(BackupFileName, false);

            // Documents 폴더 생성
            _fileManager.CreateDirectory(documentsUrl, true, null, out _);

            // 데이터 저장
            var data = NSData.FromString(encryptedData, NSStringEncoding.UTF8);
            return data.Save(fileUrl, true, out _);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 백업 다운로드
    /// </summary>
    public async Task<string?> DownloadBackupAsync()
    {
        try
        {
            var containerUrl = _fileManager.GetUrlForUbiquityContainer(ContainerIdentifier);
            if (containerUrl == null)
                return null;

            var fileUrl = containerUrl.Append("Documents", true).Append(BackupFileName, false);

            // 파일 다운로드 시작 (iCloud에서 가져오기)
            _fileManager.StartDownloadingUbiquitousItem(fileUrl, out _);

            // 파일이 로컬에 있는지 확인
            if (!_fileManager.FileExists(fileUrl.Path!))
                return null;

            var data = NSData.FromUrl(fileUrl);
            if (data == null)
                return null;

            return NSString.FromData(data, NSStringEncoding.UTF8)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 백업 존재 여부 확인
    /// </summary>
    public Task<bool> BackupExistsAsync()
    {
        try
        {
            var containerUrl = _fileManager.GetUrlForUbiquityContainer(ContainerIdentifier);
            if (containerUrl == null)
                return Task.FromResult(false);

            var fileUrl = containerUrl.Append("Documents", true).Append(BackupFileName, false);
            return Task.FromResult(_fileManager.FileExists(fileUrl.Path!));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 마지막 동기화 시간
    /// </summary>
    public Task<DateTime?> GetLastSyncTimeAsync()
    {
        try
        {
            var containerUrl = _fileManager.GetUrlForUbiquityContainer(ContainerIdentifier);
            if (containerUrl == null)
                return Task.FromResult<DateTime?>(null);

            var fileUrl = containerUrl.Append("Documents", true).Append(BackupFileName, false);

            if (!_fileManager.FileExists(fileUrl.Path!))
                return Task.FromResult<DateTime?>(null);

            var attrs = _fileManager.GetAttributes(fileUrl.Path!, out _);
            var modDate = attrs?.ModificationDate;

            return Task.FromResult<DateTime?>((DateTime?)modDate);
        }
        catch
        {
            return Task.FromResult<DateTime?>(null);
        }
    }

    /// <summary>
    /// 백업 삭제
    /// </summary>
    public Task<bool> DeleteBackupAsync()
    {
        try
        {
            var containerUrl = _fileManager.GetUrlForUbiquityContainer(ContainerIdentifier);
            if (containerUrl == null)
                return Task.FromResult(false);

            var fileUrl = containerUrl.Append("Documents", true).Append(BackupFileName, false);

            if (!_fileManager.FileExists(fileUrl.Path!))
                return Task.FromResult(true);

            return Task.FromResult(_fileManager.Remove(fileUrl, out _));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 전체 동기화 수행
    /// </summary>
    public async Task<SyncResult> SyncAsync(string password)
    {
        try
        {
            var isAvailable = await IsAvailableAsync();
            if (!isAvailable)
            {
                return new SyncResult
                {
                    Success = false,
                    Message = "iCloud is not available"
                };
            }

            // 현재 계정 가져오기
            var localAccounts = await _accountRepository.GetAllAsync();

            // iCloud 백업 확인
            var cloudData = await DownloadBackupAsync();
            List<OtpAccount>? cloudAccounts = null;

            if (!string.IsNullOrEmpty(cloudData))
            {
                try
                {
                    var decryptedJson = _encryptionService.Decrypt(cloudData, password);
                    var backupData = JsonSerializer.Deserialize<BackupData>(decryptedJson);
                    cloudAccounts = backupData?.Accounts;
                }
                catch
                {
                    // 복호화 실패 - 비밀번호 오류 또는 손상된 데이터
                }
            }

            // 병합 로직
            var mergedAccounts = MergeAccounts(localAccounts.ToList(), cloudAccounts ?? new List<OtpAccount>());

            // 로컬 저장
            foreach (var account in mergedAccounts)
            {
                var existing = await _accountRepository.GetByIdAsync(account.Id);
                if (existing == null)
                {
                    await _accountRepository.AddAsync(account);
                }
                else
                {
                    await _accountRepository.UpdateAsync(account);
                }
            }

            // iCloud 업로드
            var backupJson = JsonSerializer.Serialize(new BackupData
            {
                Accounts = mergedAccounts,
                CreatedAt = DateTime.UtcNow,
                Version = "1.0"
            });

            var encryptedBackup = _encryptionService.Encrypt(backupJson, password);
            await UploadBackupAsync(encryptedBackup);

            return new SyncResult
            {
                Success = true,
                Message = "Sync completed successfully",
                AccountsUploaded = mergedAccounts.Count,
                AccountsDownloaded = cloudAccounts?.Count ?? 0
            };
        }
        catch (Exception ex)
        {
            return new SyncResult
            {
                Success = false,
                Message = $"Sync failed: {ex.Message}"
            };
        }
    }

    private List<OtpAccount> MergeAccounts(List<OtpAccount> local, List<OtpAccount> cloud)
    {
        var merged = new Dictionary<string, OtpAccount>();

        // 로컬 계정 추가
        foreach (var account in local)
        {
            merged[account.Id] = account;
        }

        // 클라우드 계정 병합 (최신 것 우선)
        foreach (var cloudAccount in cloud)
        {
            if (merged.TryGetValue(cloudAccount.Id, out var localAccount))
            {
                // 더 최근에 수정된 것 사용
                if (cloudAccount.CreatedAt > localAccount.CreatedAt)
                {
                    merged[cloudAccount.Id] = cloudAccount;
                }
            }
            else
            {
                merged[cloudAccount.Id] = cloudAccount;
            }
        }

        return merged.Values.ToList();
    }
}

/// <summary>
/// 동기화 결과
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int AccountsUploaded { get; set; }
    public int AccountsDownloaded { get; set; }
}
