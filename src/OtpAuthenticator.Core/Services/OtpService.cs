using System.Security.Cryptography;
using System.Text;
using System.Web;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;
using OtpNet;

namespace OtpAuthenticator.Core.Services;

/// <summary>
/// OTP 코드 생성 및 관리 서비스
/// </summary>
public class OtpService : IOtpService
{
    /// <summary>
    /// OTP 코드 생성
    /// </summary>
    public string GenerateCode(OtpAccount account)
    {
        if (string.IsNullOrEmpty(account.SecretKey))
            throw new ArgumentException("Secret key is required", nameof(account));

        byte[] secretBytes = Base32Encoding.ToBytes(NormalizeSecretKey(account.SecretKey));
        var mode = GetOtpHashMode(account.Algorithm);

        return account.Type switch
        {
            OtpType.Totp => GenerateTotpCode(secretBytes, mode, account.Digits, account.Period),
            OtpType.Hotp => GenerateHotpCode(secretBytes, mode, account.Digits, account.Counter),
            _ => throw new ArgumentException($"Unknown OTP type: {account.Type}")
        };
    }

    /// <summary>
    /// TOTP 코드 생성
    /// </summary>
    private string GenerateTotpCode(byte[] secret, OtpHashMode mode, int digits, int period)
    {
        var totp = new Totp(secret, step: period, mode: mode, totpSize: digits);
        return totp.ComputeTotp();
    }

    /// <summary>
    /// HOTP 코드 생성
    /// </summary>
    private string GenerateHotpCode(byte[] secret, OtpHashMode mode, int digits, long counter)
    {
        var hotp = new Hotp(secret, mode: mode, hotpSize: digits);
        return hotp.ComputeHOTP(counter);
    }

    /// <summary>
    /// TOTP 남은 시간 계산
    /// </summary>
    public int GetRemainingSeconds(int period = 30)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return period - (int)(now % period);
    }

    /// <summary>
    /// HOTP 카운터 증가
    /// </summary>
    public void IncrementCounter(OtpAccount account)
    {
        if (account.Type != OtpType.Hotp)
            throw new InvalidOperationException("Counter can only be incremented for HOTP accounts");

        account.Counter++;
    }

    /// <summary>
    /// otpauth:// URI 파싱
    /// </summary>
    public OtpAccount? ParseOtpAuthUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return null;

        if (!uri.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            var uriObj = new Uri(uri);

            // 타입 파싱 (totp 또는 hotp)
            var type = uriObj.Host.ToUpperInvariant() switch
            {
                "TOTP" => OtpType.Totp,
                "HOTP" => OtpType.Hotp,
                _ => throw new ArgumentException($"Unknown OTP type: {uriObj.Host}")
            };

            // 레이블 파싱 (Issuer:AccountName 또는 AccountName)
            var label = Uri.UnescapeDataString(uriObj.AbsolutePath.TrimStart('/'));
            string issuer;
            string accountName;

            if (label.Contains(':'))
            {
                var parts = label.Split(':', 2);
                issuer = parts[0].Trim();
                accountName = parts[1].Trim();
            }
            else
            {
                issuer = string.Empty;
                accountName = label.Trim();
            }

            // 쿼리 파라미터 파싱
            var query = HttpUtility.ParseQueryString(uriObj.Query);

            var secret = query["secret"];
            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("Secret key is required");

            // issuer 파라미터가 있으면 우선 사용
            var queryIssuer = query["issuer"];
            if (!string.IsNullOrEmpty(queryIssuer))
                issuer = queryIssuer;

            var account = new OtpAccount
            {
                Type = type,
                Issuer = issuer,
                AccountName = accountName,
                SecretKey = NormalizeSecretKey(secret),
                Algorithm = ParseAlgorithm(query["algorithm"]),
                Digits = ParseInt(query["digits"], 6),
                Period = ParseInt(query["period"], 30),
                Counter = ParseLong(query["counter"], 0)
            };

            return account;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// otpauth:// URI 생성
    /// </summary>
    public string GenerateOtpAuthUri(OtpAccount account)
    {
        var type = account.Type == OtpType.Totp ? "totp" : "hotp";

        // 레이블 생성
        var label = string.IsNullOrEmpty(account.Issuer)
            ? Uri.EscapeDataString(account.AccountName)
            : $"{Uri.EscapeDataString(account.Issuer)}:{Uri.EscapeDataString(account.AccountName)}";

        var sb = new StringBuilder();
        sb.Append($"otpauth://{type}/{label}");
        sb.Append($"?secret={account.SecretKey}");

        if (!string.IsNullOrEmpty(account.Issuer))
            sb.Append($"&issuer={Uri.EscapeDataString(account.Issuer)}");

        if (account.Algorithm != HashAlgorithmType.Sha1)
            sb.Append($"&algorithm={account.Algorithm.ToString().ToUpperInvariant()}");

        if (account.Digits != 6)
            sb.Append($"&digits={account.Digits}");

        if (account.Type == OtpType.Totp && account.Period != 30)
            sb.Append($"&period={account.Period}");

        if (account.Type == OtpType.Hotp)
            sb.Append($"&counter={account.Counter}");

        return sb.ToString();
    }

    /// <summary>
    /// 비밀 키 유효성 검증
    /// </summary>
    public bool ValidateSecretKey(string secretKey)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
            return false;

        try
        {
            var normalized = NormalizeSecretKey(secretKey);
            var bytes = Base32Encoding.ToBytes(normalized);
            return bytes.Length >= 10; // 최소 80비트
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 랜덤 비밀 키 생성
    /// </summary>
    public string GenerateSecretKey(int length = 20)
    {
        var key = KeyGeneration.GenerateRandomKey(length);
        return Base32Encoding.ToString(key);
    }

    /// <summary>
    /// 비밀 키 정규화 (공백, 대시 제거, 대문자 변환)
    /// </summary>
    private static string NormalizeSecretKey(string secretKey)
    {
        return secretKey
            .Replace(" ", "")
            .Replace("-", "")
            .ToUpperInvariant();
    }

    /// <summary>
    /// 해시 알고리즘 문자열 파싱
    /// </summary>
    private static HashAlgorithmType ParseAlgorithm(string? algorithm)
    {
        return algorithm?.ToUpperInvariant() switch
        {
            "SHA256" => HashAlgorithmType.Sha256,
            "SHA512" => HashAlgorithmType.Sha512,
            _ => HashAlgorithmType.Sha1
        };
    }

    /// <summary>
    /// OtpNet 해시 모드로 변환
    /// </summary>
    private static OtpHashMode GetOtpHashMode(HashAlgorithmType algorithm)
    {
        return algorithm switch
        {
            HashAlgorithmType.Sha256 => OtpHashMode.Sha256,
            HashAlgorithmType.Sha512 => OtpHashMode.Sha512,
            _ => OtpHashMode.Sha1
        };
    }

    /// <summary>
    /// 정수 파싱 (기본값 지원)
    /// </summary>
    private static int ParseInt(string? value, int defaultValue)
    {
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Long 파싱 (기본값 지원)
    /// </summary>
    private static long ParseLong(string? value, long defaultValue)
    {
        return long.TryParse(value, out var result) ? result : defaultValue;
    }
}
