using System.Security.Cryptography;
using System.Text;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.Core.Services;

/// <summary>
/// AES-256 암호화 서비스
/// </summary>
public class EncryptionService : IEncryptionService
{
    private const int KeySize = 256;
    private const int SaltSize = 32;
    private const int IvSize = 16;
    private const int Iterations = 100000;

    /// <summary>
    /// AES-256-CBC로 데이터 암호화
    /// </summary>
    public EncryptedBackup Encrypt(string plainText, string password)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentNullException(nameof(plainText));
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password));

        // 랜덤 Salt, IV 생성
        byte[] salt = GenerateRandomBytes(SaltSize);
        byte[] iv = GenerateRandomBytes(IvSize);

        // PBKDF2로 키 유도
        byte[] key = DeriveKey(password, salt);

        // AES 암호화
        byte[] cipherBytes;
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        }

        // HMAC-SHA256으로 무결성 검증값 생성
        string hmac = ComputeHmac(cipherBytes, key);

        return new EncryptedBackup
        {
            Version = "1.0",
            Salt = Convert.ToBase64String(salt),
            IV = Convert.ToBase64String(iv),
            Data = Convert.ToBase64String(cipherBytes),
            Hmac = hmac
        };
    }

    /// <summary>
    /// 암호화된 데이터 복호화
    /// </summary>
    public string Decrypt(EncryptedBackup encrypted, string password)
    {
        if (encrypted == null)
            throw new ArgumentNullException(nameof(encrypted));
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password));

        byte[] salt = Convert.FromBase64String(encrypted.Salt);
        byte[] iv = Convert.FromBase64String(encrypted.IV);
        byte[] cipherBytes = Convert.FromBase64String(encrypted.Data);

        // 키 유도
        byte[] key = DeriveKey(password, salt);

        // HMAC 검증
        string expectedHmac = ComputeHmac(cipherBytes, key);
        if (!ConstantTimeEquals(
            Encoding.UTF8.GetBytes(encrypted.Hmac),
            Encoding.UTF8.GetBytes(expectedHmac)))
        {
            throw new CryptographicException("Data integrity verification failed. Invalid password or corrupted data.");
        }

        // AES 복호화
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// SHA256 해시 계산
    /// </summary>
    public string ComputeSha256Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        using var sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(inputBytes);
        return BytesToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// PBKDF2로 키 유도
    /// </summary>
    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(KeySize / 8);
    }

    /// <summary>
    /// HMAC-SHA256 계산
    /// </summary>
    private static string ComputeHmac(byte[] data, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        byte[] hash = hmac.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// 랜덤 바이트 생성
    /// </summary>
    private static byte[] GenerateRandomBytes(int size)
    {
        byte[] bytes = new byte[size];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return bytes;
    }

    /// <summary>
    /// 바이트 배열을 16진수 문자열로 변환
    /// </summary>
    private static string BytesToHexString(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    /// <summary>
    /// 상수 시간 비교 (타이밍 공격 방지)
    /// </summary>
    private static bool ConstantTimeEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;

        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        return result == 0;
    }
}
