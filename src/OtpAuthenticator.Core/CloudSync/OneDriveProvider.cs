using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace OtpAuthenticator.Core.CloudSync;

/// <summary>
/// OneDrive 클라우드 프로바이더
/// </summary>
public class OneDriveProvider : ICloudProvider
{
    // Azure AD 앱 등록 필요 (Microsoft Partner Center)
    // 실제 배포 시 이 값들을 설정 파일이나 환경 변수에서 로드해야 함
    private const string ClientId = "YOUR_CLIENT_ID"; // Azure AD에서 발급받은 Client ID
    private const string TenantId = "consumers"; // 개인 Microsoft 계정용

    private const string AppFolderName = "OtpAuthenticator";
    private const string BackupFileName = "backup.otp";

    private readonly string[] _scopes = { "Files.ReadWrite.AppFolder", "User.Read" };

    private IPublicClientApplication? _msalClient;
    private GraphServiceClient? _graphClient;
    private string? _accessToken;

    public string ProviderName => "OneDrive";

    public OneDriveProvider()
    {
        InitializeMsalClient();
    }

    private void InitializeMsalClient()
    {
        _msalClient = PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, TenantId)
            .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
            .Build();
    }

    /// <summary>
    /// 인증 여부 확인
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        if (_msalClient == null) return false;

        try
        {
            var accounts = await _msalClient.GetAccountsAsync();
            return accounts.Any();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 인증 수행
    /// </summary>
    public async Task<bool> AuthenticateAsync()
    {
        if (_msalClient == null)
        {
            InitializeMsalClient();
            if (_msalClient == null) return false;
        }

        try
        {
            AuthenticationResult result;

            var accounts = await _msalClient.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            try
            {
                // Silent 인증 시도
                result = await _msalClient
                    .AcquireTokenSilent(_scopes, firstAccount)
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                // Interactive 인증 필요
                result = await _msalClient
                    .AcquireTokenInteractive(_scopes)
                    .ExecuteAsync();
            }

            _accessToken = result.AccessToken;

            // GraphServiceClient 초기화 (Graph SDK v5)
            _graphClient = new GraphServiceClient(
                new TokenAuthenticationProvider(_accessToken));

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 로그아웃
    /// </summary>
    public async Task SignOutAsync()
    {
        if (_msalClient == null) return;

        var accounts = await _msalClient.GetAccountsAsync();
        foreach (var account in accounts)
        {
            await _msalClient.RemoveAsync(account);
        }

        _graphClient = null;
        _accessToken = null;
    }

    /// <summary>
    /// 파일 업로드
    /// </summary>
    public async Task<CloudFile?> UploadAsync(string fileName, byte[] data)
    {
        if (_graphClient == null)
        {
            if (!await AuthenticateAsync()) return null;
        }

        try
        {
            using var stream = new MemoryStream(data);

            // AppFolder에 업로드 (특수 폴더 - 앱 전용)
            var driveItem = await _graphClient!.Me.Drive.Special.AppRoot
                .ItemWithPath($"{AppFolderName}/{fileName}")
                .Content
                .PutAsync(stream);

            if (driveItem == null) return null;

            return new CloudFile
            {
                Id = driveItem.Id ?? string.Empty,
                Name = driveItem.Name ?? fileName,
                ModifiedTime = driveItem.LastModifiedDateTime?.DateTime ?? DateTime.UtcNow,
                Size = driveItem.Size ?? 0
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 파일 다운로드
    /// </summary>
    public async Task<byte[]?> DownloadAsync(string fileId)
    {
        if (_graphClient == null)
        {
            if (!await AuthenticateAsync()) return null;
        }

        try
        {
            var stream = await _graphClient!.Me.Drive.Items[fileId]
                .Content
                .GetAsync();

            if (stream == null) return null;

            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 파일 정보 조회
    /// </summary>
    public async Task<CloudFile?> GetFileInfoAsync(string fileName)
    {
        if (_graphClient == null)
        {
            if (!await AuthenticateAsync()) return null;
        }

        try
        {
            var driveItem = await _graphClient!.Me.Drive.Special.AppRoot
                .ItemWithPath($"{AppFolderName}/{fileName}")
                .GetAsync();

            if (driveItem == null) return null;

            return new CloudFile
            {
                Id = driveItem.Id ?? string.Empty,
                Name = driveItem.Name ?? fileName,
                ModifiedTime = driveItem.LastModifiedDateTime?.DateTime ?? DateTime.MinValue,
                Size = driveItem.Size ?? 0
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 파일 삭제
    /// </summary>
    public async Task<bool> DeleteAsync(string fileId)
    {
        if (_graphClient == null)
        {
            if (!await AuthenticateAsync()) return false;
        }

        try
        {
            await _graphClient!.Me.Drive.Items[fileId].DeleteAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 파일 마지막 수정 시간 조회
    /// </summary>
    public async Task<DateTime?> GetLastModifiedAsync(string fileName)
    {
        var fileInfo = await GetFileInfoAsync(fileName);
        return fileInfo?.ModifiedTime;
    }
}

/// <summary>
/// Token 기반 인증 프로바이더 (Graph SDK v5 Kiota 호환용)
/// </summary>
internal class TokenAuthenticationProvider : IAuthenticationProvider
{
    private readonly string _accessToken;

    public TokenAuthenticationProvider(string accessToken)
    {
        _accessToken = accessToken;
    }

    public Task AuthenticateRequestAsync(
        RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        request.Headers.Add("Authorization", $"Bearer {_accessToken}");
        return Task.CompletedTask;
    }
}
