using System.Text.Json;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.Widget;

/// <summary>
/// 위젯 데이터 제공자
/// </summary>
public class OtpWidgetDataProvider
{
    private readonly IOtpService _otpService;
    private List<OtpAccount> _cachedAccounts = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromSeconds(5);

    public OtpWidgetDataProvider()
    {
        _otpService = new OtpService();
    }

    /// <summary>
    /// 위젯 데이터 JSON 반환
    /// </summary>
    public string GetWidgetData(int index = 0)
    {
        RefreshAccountsIfNeeded();

        if (_cachedAccounts.Count == 0)
        {
            return GetEmptyData();
        }

        // 인덱스 범위 조정
        index = Math.Max(0, Math.Min(index, _cachedAccounts.Count - 1));
        var account = _cachedAccounts[index];

        try
        {
            var otpCode = _otpService.GenerateCode(account);
            var remainingSeconds = _otpService.GetRemainingSeconds(account.Period);
            var progress = (double)remainingSeconds / account.Period;

            // 코드 포맷팅 (3자리씩)
            var formattedCode = account.Digits == 6
                ? $"{otpCode[..3]} {otpCode[3..]}"
                : $"{otpCode[..4]} {otpCode[4..]}";

            var data = new
            {
                issuer = string.IsNullOrEmpty(account.Issuer) ? "OTP" : account.Issuer,
                accountName = account.AccountName,
                initial = account.Initial,
                otpCode = formattedCode,
                rawCode = otpCode,
                timeProgress = progress,
                remainingSeconds = remainingSeconds,
                progressColor = progress > 0.3 ? "accent" : "attention",
                accountCount = _cachedAccounts.Count,
                currentIndex = index + 1,
                hasNext = index < _cachedAccounts.Count - 1,
                hasPrev = index > 0
            };

            return JsonSerializer.Serialize(data);
        }
        catch
        {
            return GetEmptyData();
        }
    }

    /// <summary>
    /// 빈 데이터 반환
    /// </summary>
    private static string GetEmptyData()
    {
        var data = new
        {
            issuer = "OTP Authenticator",
            accountName = "No accounts",
            initial = "?",
            otpCode = "--- ---",
            rawCode = "",
            timeProgress = 1.0,
            remainingSeconds = 30,
            progressColor = "accent",
            accountCount = 0,
            currentIndex = 0,
            hasNext = false,
            hasPrev = false
        };

        return JsonSerializer.Serialize(data);
    }

    /// <summary>
    /// 계정 목록 새로고침
    /// </summary>
    private void RefreshAccountsIfNeeded()
    {
        if (DateTime.UtcNow - _lastRefresh < CacheExpiry)
            return;

        try
        {
            // 계정 파일에서 직접 로드 (서비스 의존성 최소화)
            var accounts = LoadAccountsFromFile();

            // 즐겨찾기 우선, 그 다음 정렬 순서
            _cachedAccounts = accounts
                .OrderByDescending(a => a.IsFavorite)
                .ThenBy(a => a.SortOrder)
                .ToList();

            _lastRefresh = DateTime.UtcNow;
        }
        catch
        {
            // 로드 실패 시 기존 캐시 유지
        }
    }

    /// <summary>
    /// 데이터 디렉토리 경로 가져오기 (MSIX 패키지 및 일반 실행 모두 지원)
    /// </summary>
    private static string GetDataDirectory()
    {
        // Environment.GetFolderPath는 MSIX 패키지에서 자동으로 가상화된 경로 반환
        // (LocalCache\Local 폴더로 리다이렉트됨)
        // Main App의 SecureStorageService와 동일한 위치 사용
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDir = Path.Combine(basePath, "OtpAuthenticator");

        // 디버깅용 로그 (이벤트 로그에 기록)
        try
        {
            System.Diagnostics.Debug.WriteLine($"[OtpWidget] Data directory: {dataDir}");
            System.Diagnostics.Debug.WriteLine($"[OtpWidget] Directory exists: {Directory.Exists(dataDir)}");
        }
        catch { }

        return dataDir;
    }

    /// <summary>
    /// 파일에서 계정 로드
    /// </summary>
    private List<OtpAccount> LoadAccountsFromFile()
    {
        try
        {
            var dataDir = GetDataDirectory();
            var dataPath = Path.Combine(dataDir, "accounts.dat");

            System.Diagnostics.Debug.WriteLine($"[OtpWidget] Loading accounts from: {dataPath}");

            if (!File.Exists(dataPath))
            {
                System.Diagnostics.Debug.WriteLine($"[OtpWidget] accounts.dat not found");
                return new List<OtpAccount>();
            }

            // DPAPI로 암호화된 파일 복호화
            var encryptedBytes = File.ReadAllBytes(dataPath);
            System.Diagnostics.Debug.WriteLine($"[OtpWidget] Read {encryptedBytes.Length} encrypted bytes");

            var plainBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                encryptedBytes,
                null,
                System.Security.Cryptography.DataProtectionScope.CurrentUser);

            var json = System.Text.Encoding.UTF8.GetString(plainBytes);
            System.Diagnostics.Debug.WriteLine($"[OtpWidget] Decrypted JSON length: {json.Length}");

            // AccountData 목록으로 역직렬화
            var accountDataList = JsonSerializer.Deserialize<List<AccountDataDto>>(json);
            if (accountDataList == null)
            {
                System.Diagnostics.Debug.WriteLine($"[OtpWidget] Deserialization returned null");
                return new List<OtpAccount>();
            }

            System.Diagnostics.Debug.WriteLine($"[OtpWidget] Found {accountDataList.Count} accounts");

            // OtpAccount로 변환하고 비밀 키 로드
            var accounts = new List<OtpAccount>();
            foreach (var data in accountDataList)
            {
                var account = data.ToAccount();

                // 비밀 키 로드 시도
                var secretKey = LoadSecretKey(account.Id);
                if (!string.IsNullOrEmpty(secretKey))
                {
                    account.SecretKey = secretKey;
                    accounts.Add(account);
                    System.Diagnostics.Debug.WriteLine($"[OtpWidget] Loaded account: {account.Issuer} - {account.AccountName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[OtpWidget] Failed to load secret for: {account.Id}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[OtpWidget] Total accounts with secrets: {accounts.Count}");
            return accounts;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OtpWidget] Error loading accounts: {ex.Message}");
            return new List<OtpAccount>();
        }
    }

    /// <summary>
    /// 비밀 키 로드
    /// </summary>
    private string? LoadSecretKey(Guid accountId)
    {
        try
        {
            // PasswordVault에서 로드 시도
            var vault = new Windows.Security.Credentials.PasswordVault();
            var credential = vault.Retrieve("OtpAuthenticator", $"secret_{accountId}");
            credential.RetrievePassword();
            return credential.Password;
        }
        catch
        {
            // DPAPI 폴백
            try
            {
                var key = $"secret_{accountId}";
                var safeKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key))
                    .Replace('/', '_')
                    .Replace('+', '-');

                var filePath = Path.Combine(
                    GetDataDirectory(),
                    "secrets",
                    $"{safeKey}.dat");

                if (!File.Exists(filePath))
                    return null;

                var encryptedBytes = File.ReadAllBytes(filePath);
                var plainBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                    encryptedBytes,
                    System.Text.Encoding.UTF8.GetBytes(key),
                    System.Security.Cryptography.DataProtectionScope.CurrentUser);

                return System.Text.Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return null;
            }
        }
    }
}

/// <summary>
/// 계정 데이터 DTO (파일 저장용)
/// </summary>
internal class AccountDataDto
{
    public Guid Id { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public OtpType Type { get; set; }
    public HashAlgorithmType Algorithm { get; set; }
    public int Digits { get; set; }
    public int Period { get; set; }
    public long Counter { get; set; }
    public string? IconPath { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? Notes { get; set; }

    public OtpAccount ToAccount()
    {
        return new OtpAccount
        {
            Id = Id,
            Issuer = Issuer,
            AccountName = AccountName,
            Type = Type,
            Algorithm = Algorithm,
            Digits = Digits,
            Period = Period,
            Counter = Counter,
            IconPath = IconPath,
            Color = Color,
            SortOrder = SortOrder,
            IsFavorite = IsFavorite,
            CreatedAt = CreatedAt,
            LastUsedAt = LastUsedAt,
            Notes = Notes
        };
    }
}
