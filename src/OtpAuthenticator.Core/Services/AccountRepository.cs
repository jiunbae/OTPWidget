using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.Core.Services;

/// <summary>
/// OTP 계정 저장소 구현
/// </summary>
public class AccountRepository : IAccountRepository
{
    private const string AccountsFileName = "accounts.dat";
    private const string SecretsPrefix = "secret_";

    private readonly ISecureStorageService _secureStorage;
    private List<OtpAccount> _accounts = new();
    private bool _isLoaded = false;

    public AccountRepository(ISecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
    }

    /// <summary>
    /// 데이터 로드 확인 및 초기 로드
    /// </summary>
    private async Task EnsureLoadedAsync()
    {
        if (_isLoaded)
            return;

        var accountsData = await _secureStorage.LoadEncryptedDataAsync<List<AccountData>>(AccountsFileName);

        if (accountsData != null)
        {
            _accounts = accountsData.Select(data =>
            {
                var account = data.ToAccount();
                // 비밀 키는 별도로 저장되어 있음
                account.SecretKey = _secureStorage.RetrieveSecret($"{SecretsPrefix}{account.Id}") ?? string.Empty;
                return account;
            }).ToList();
        }
        else
        {
            _accounts = new List<OtpAccount>();
        }

        _isLoaded = true;
    }

    /// <summary>
    /// 데이터 저장
    /// </summary>
    private async Task SaveAsync()
    {
        var accountsData = _accounts.Select(a => AccountData.FromAccount(a)).ToList();
        await _secureStorage.SaveEncryptedDataAsync(AccountsFileName, accountsData);
    }

    /// <summary>
    /// 모든 계정 조회
    /// </summary>
    public async Task<IReadOnlyList<OtpAccount>> GetAllAsync()
    {
        await EnsureLoadedAsync();
        return _accounts.OrderBy(a => a.SortOrder).ToList().AsReadOnly();
    }

    /// <summary>
    /// ID로 계정 조회
    /// </summary>
    public async Task<OtpAccount?> GetByIdAsync(Guid id)
    {
        await EnsureLoadedAsync();
        return _accounts.FirstOrDefault(a => a.Id == id);
    }

    /// <summary>
    /// 계정 추가
    /// </summary>
    public async Task<OtpAccount> AddAsync(OtpAccount account)
    {
        await EnsureLoadedAsync();

        if (account.Id == Guid.Empty)
            account.Id = Guid.NewGuid();

        account.CreatedAt = DateTime.UtcNow;
        account.SortOrder = _accounts.Count;

        // 비밀 키는 별도로 저장
        _secureStorage.StoreSecret($"{SecretsPrefix}{account.Id}", account.SecretKey);

        _accounts.Add(account);
        await SaveAsync();

        return account;
    }

    /// <summary>
    /// 계정 수정
    /// </summary>
    public async Task UpdateAsync(OtpAccount account)
    {
        await EnsureLoadedAsync();

        var index = _accounts.FindIndex(a => a.Id == account.Id);
        if (index < 0)
            throw new KeyNotFoundException($"Account with ID {account.Id} not found");

        // 비밀 키가 변경되었으면 업데이트
        var existingSecret = _secureStorage.RetrieveSecret($"{SecretsPrefix}{account.Id}");
        if (existingSecret != account.SecretKey)
        {
            _secureStorage.StoreSecret($"{SecretsPrefix}{account.Id}", account.SecretKey);
        }

        _accounts[index] = account;
        await SaveAsync();
    }

    /// <summary>
    /// 계정 삭제
    /// </summary>
    public async Task DeleteAsync(Guid id)
    {
        await EnsureLoadedAsync();

        var account = _accounts.FirstOrDefault(a => a.Id == id);
        if (account == null)
            return;

        // 비밀 키 삭제
        _secureStorage.RemoveSecret($"{SecretsPrefix}{id}");

        _accounts.Remove(account);
        await SaveAsync();
    }

    /// <summary>
    /// 정렬 순서 업데이트
    /// </summary>
    public async Task UpdateSortOrderAsync(IEnumerable<(Guid Id, int SortOrder)> orders)
    {
        await EnsureLoadedAsync();

        foreach (var (id, sortOrder) in orders)
        {
            var account = _accounts.FirstOrDefault(a => a.Id == id);
            if (account != null)
            {
                account.SortOrder = sortOrder;
            }
        }

        await SaveAsync();
    }

    /// <summary>
    /// 계정 개수
    /// </summary>
    public async Task<int> GetCountAsync()
    {
        await EnsureLoadedAsync();
        return _accounts.Count;
    }

    /// <summary>
    /// 즐겨찾기 계정 조회
    /// </summary>
    public async Task<IReadOnlyList<OtpAccount>> GetFavoritesAsync()
    {
        await EnsureLoadedAsync();
        return _accounts
            .Where(a => a.IsFavorite)
            .OrderBy(a => a.SortOrder)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// 데이터 새로고침 (외부에서 변경되었을 때)
    /// </summary>
    public void Refresh()
    {
        _isLoaded = false;
    }
}

/// <summary>
/// 계정 데이터 (비밀 키 제외, 저장용)
/// </summary>
internal class AccountData
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

    public static AccountData FromAccount(OtpAccount account)
    {
        return new AccountData
        {
            Id = account.Id,
            Issuer = account.Issuer,
            AccountName = account.AccountName,
            Type = account.Type,
            Algorithm = account.Algorithm,
            Digits = account.Digits,
            Period = account.Period,
            Counter = account.Counter,
            IconPath = account.IconPath,
            Color = account.Color,
            SortOrder = account.SortOrder,
            IsFavorite = account.IsFavorite,
            CreatedAt = account.CreatedAt,
            LastUsedAt = account.LastUsedAt,
            Notes = account.Notes
        };
    }

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
