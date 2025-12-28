namespace OtpAuthenticator.Core.Models;

/// <summary>
/// OTP 계정 정보를 저장하는 모델
/// </summary>
public class OtpAccount
{
    /// <summary>
    /// 고유 식별자
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 발급자 (Google, GitHub, Microsoft 등)
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 계정명 (이메일, 사용자명 등)
    /// </summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Base32 인코딩된 비밀 키
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// OTP 타입 (TOTP 또는 HOTP)
    /// </summary>
    public OtpType Type { get; set; } = OtpType.Totp;

    /// <summary>
    /// 해시 알고리즘
    /// </summary>
    public HashAlgorithmType Algorithm { get; set; } = HashAlgorithmType.Sha1;

    /// <summary>
    /// OTP 코드 자릿수 (6 또는 8)
    /// </summary>
    public int Digits { get; set; } = 6;

    /// <summary>
    /// TOTP 주기 (초 단위, 기본 30초)
    /// </summary>
    public int Period { get; set; } = 30;

    /// <summary>
    /// HOTP 카운터
    /// </summary>
    public long Counter { get; set; } = 0;

    /// <summary>
    /// 아이콘 경로 또는 Base64 이미지
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// 테마 색상 (HEX)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// 정렬 순서
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 즐겨찾기 여부
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// 계정 생성 시간
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 마지막 사용 시간
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 메모
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 표시용 이름 (Issuer: AccountName 형식)
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(Issuer)
        ? AccountName
        : $"{Issuer}: {AccountName}";

    /// <summary>
    /// 표시용 이니셜 (Issuer의 첫 글자)
    /// </summary>
    public string Initial => string.IsNullOrEmpty(Issuer)
        ? (string.IsNullOrEmpty(AccountName) ? "?" : AccountName[..1].ToUpperInvariant())
        : Issuer[..1].ToUpperInvariant();
}

/// <summary>
/// OTP 타입
/// </summary>
public enum OtpType
{
    /// <summary>
    /// Time-based One-Time Password (RFC 6238)
    /// </summary>
    Totp,

    /// <summary>
    /// HMAC-based One-Time Password (RFC 4226)
    /// </summary>
    Hotp
}

/// <summary>
/// 해시 알고리즘 타입
/// </summary>
public enum HashAlgorithmType
{
    Sha1,
    Sha256,
    Sha512
}
