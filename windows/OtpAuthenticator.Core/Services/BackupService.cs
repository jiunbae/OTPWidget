using System.Text.Json;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.Core.Services;

/// <summary>
/// 백업/복원 서비스
/// </summary>
public class BackupService : IBackupService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ISettingsService _settingsService;
    private readonly IEncryptionService _encryptionService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BackupService(
        IAccountRepository accountRepository,
        ISettingsService settingsService,
        IEncryptionService encryptionService)
    {
        _accountRepository = accountRepository;
        _settingsService = settingsService;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// 암호화된 백업 파일 생성
    /// </summary>
    public async Task ExportAsync(string filePath, string password, bool includeSettings = true)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password), "Password is required for encrypted backup");

        var backupData = await CreateBackupDataAsync(includeSettings);

        // JSON 직렬화
        string json = JsonSerializer.Serialize(backupData, JsonOptions);

        // 암호화
        var encrypted = _encryptionService.Encrypt(json, password);

        // 파일 저장
        string encryptedJson = JsonSerializer.Serialize(encrypted, JsonOptions);
        await File.WriteAllTextAsync(filePath, encryptedJson);
    }

    /// <summary>
    /// 암호화된 백업 파일에서 복원
    /// </summary>
    public async Task<int> ImportAsync(string filePath, string password, bool restoreSettings = false)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Backup file not found", filePath);

        // 파일 읽기
        string encryptedJson = await File.ReadAllTextAsync(filePath);
        var encrypted = JsonSerializer.Deserialize<EncryptedBackup>(encryptedJson, JsonOptions);

        if (encrypted == null)
            throw new InvalidDataException("Invalid backup file format");

        // 복호화
        string json = _encryptionService.Decrypt(encrypted, password);
        var backupData = JsonSerializer.Deserialize<BackupData>(json, JsonOptions);

        if (backupData == null)
            throw new InvalidDataException("Invalid backup data");

        // 체크섬 검증
        var calculatedChecksum = CalculateChecksum(backupData.Accounts);
        if (backupData.Checksum != calculatedChecksum)
            throw new InvalidDataException("Backup data integrity check failed");

        // 계정 복원
        int importedCount = 0;
        foreach (var account in backupData.Accounts)
        {
            // 새 ID 할당 (중복 방지)
            account.Id = Guid.NewGuid();
            await _accountRepository.AddAsync(account);
            importedCount++;
        }

        // 설정 복원 (선택)
        if (restoreSettings && backupData.Settings != null)
        {
            // 설정 복원 로직은 SettingsService에서 처리
        }

        return importedCount;
    }

    /// <summary>
    /// 백업 파일 미리보기
    /// </summary>
    public async Task<BackupMetadata?> PreviewBackupAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            string encryptedJson = await File.ReadAllTextAsync(filePath);
            var encrypted = JsonSerializer.Deserialize<EncryptedBackup>(encryptedJson, JsonOptions);

            if (encrypted == null)
                return null;

            return new BackupMetadata
            {
                Version = encrypted.Version,
                // 암호화된 상태에서는 상세 정보 확인 불가
                CreatedAt = DateTime.MinValue,
                DeviceName = "Encrypted",
                AccountCount = -1,
                HasSettings = false
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 평문 JSON으로 내보내기 (개발/디버그용)
    /// </summary>
    public async Task ExportPlainJsonAsync(string filePath, bool includeSettings = true)
    {
        var backupData = await CreateBackupDataAsync(includeSettings);
        string json = JsonSerializer.Serialize(backupData, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// 평문 JSON에서 가져오기
    /// </summary>
    public async Task<int> ImportPlainJsonAsync(string filePath, bool restoreSettings = false)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Backup file not found", filePath);

        string json = await File.ReadAllTextAsync(filePath);
        var backupData = JsonSerializer.Deserialize<BackupData>(json, JsonOptions);

        if (backupData == null)
            throw new InvalidDataException("Invalid backup data");

        int importedCount = 0;
        foreach (var account in backupData.Accounts)
        {
            account.Id = Guid.NewGuid();
            await _accountRepository.AddAsync(account);
            importedCount++;
        }

        return importedCount;
    }

    /// <summary>
    /// 백업 데이터 생성
    /// </summary>
    private async Task<BackupData> CreateBackupDataAsync(bool includeSettings)
    {
        var accounts = await _accountRepository.GetAllAsync();

        var backupData = new BackupData
        {
            Version = "1.0",
            CreatedAt = DateTime.UtcNow,
            DeviceName = Environment.MachineName,
            Accounts = accounts.ToList(),
            Settings = includeSettings ? _settingsService.Settings : null
        };

        // 체크섬 계산
        backupData.Checksum = CalculateChecksum(backupData.Accounts);

        return backupData;
    }

    /// <summary>
    /// 체크섬 계산
    /// </summary>
    private string CalculateChecksum(List<OtpAccount> accounts)
    {
        var data = string.Join("|", accounts.Select(a => $"{a.Issuer}:{a.AccountName}:{a.SecretKey}"));
        return _encryptionService.ComputeSha256Hash(data);
    }
}
