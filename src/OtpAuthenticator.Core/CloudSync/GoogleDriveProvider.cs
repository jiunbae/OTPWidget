using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace OtpAuthenticator.Core.CloudSync;

/// <summary>
/// Google Drive 클라우드 프로바이더
/// </summary>
public class GoogleDriveProvider : ICloudProvider
{
    // Google Cloud Console에서 OAuth 2.0 클라이언트 ID 발급 필요
    private const string ClientId = "YOUR_CLIENT_ID.apps.googleusercontent.com";
    private const string ClientSecret = "YOUR_CLIENT_SECRET";

    private const string AppFolderName = "OtpAuthenticator";
    private const string ApplicationName = "OTP Authenticator";

    private readonly string[] _scopes = { DriveService.Scope.DriveAppdata };

    private DriveService? _driveService;
    private string? _appFolderId;

    public string ProviderName => "GoogleDrive";

    /// <summary>
    /// 인증 여부 확인
    /// </summary>
    public Task<bool> IsAuthenticatedAsync()
    {
        return Task.FromResult(_driveService != null);
    }

    /// <summary>
    /// 인증 수행
    /// </summary>
    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = ClientId,
                    ClientSecret = ClientSecret
                },
                _scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(GetTokenStorePath(), true));

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });

            // 앱 폴더 ID 조회 또는 생성
            _appFolderId = await GetOrCreateAppFolderAsync();

            return _driveService != null && _appFolderId != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 로그아웃
    /// </summary>
    public Task SignOutAsync()
    {
        _driveService?.Dispose();
        _driveService = null;
        _appFolderId = null;

        // 토큰 파일 삭제
        try
        {
            var tokenPath = GetTokenStorePath();
            if (Directory.Exists(tokenPath))
            {
                Directory.Delete(tokenPath, true);
            }
        }
        catch { }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 파일 업로드
    /// </summary>
    public async Task<CloudFile?> UploadAsync(string fileName, byte[] data)
    {
        if (_driveService == null || _appFolderId == null)
        {
            if (!await AuthenticateAsync()) return null;
        }

        try
        {
            // 기존 파일 검색
            var existingFile = await FindFileAsync(fileName);

            using var stream = new MemoryStream(data);

            Google.Apis.Drive.v3.Data.File uploadedFile;

            if (existingFile != null)
            {
                // 기존 파일 업데이트
                var updateRequest = _driveService!.Files.Update(
                    new Google.Apis.Drive.v3.Data.File(),
                    existingFile.Id,
                    stream,
                    "application/octet-stream");

                var result = await updateRequest.UploadAsync();
                if (result.Status != Google.Apis.Upload.UploadStatus.Completed)
                    return null;

                uploadedFile = updateRequest.ResponseBody;
            }
            else
            {
                // 새 파일 생성
                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName,
                    Parents = new List<string> { _appFolderId! }
                };

                var createRequest = _driveService!.Files.Create(fileMetadata, stream, "application/octet-stream");
                createRequest.Fields = "id, name, modifiedTime, size";

                var result = await createRequest.UploadAsync();
                if (result.Status != Google.Apis.Upload.UploadStatus.Completed)
                    return null;

                uploadedFile = createRequest.ResponseBody;
            }

            return new CloudFile
            {
                Id = uploadedFile.Id,
                Name = uploadedFile.Name,
                ModifiedTime = uploadedFile.ModifiedTimeDateTimeOffset?.DateTime ?? DateTime.UtcNow,
                Size = uploadedFile.Size ?? 0
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
        if (_driveService == null)
        {
            if (!await AuthenticateAsync()) return null;
        }

        try
        {
            using var ms = new MemoryStream();
            await _driveService!.Files.Get(fileId).DownloadAsync(ms);
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
        return await FindFileAsync(fileName);
    }

    /// <summary>
    /// 파일 삭제
    /// </summary>
    public async Task<bool> DeleteAsync(string fileId)
    {
        if (_driveService == null)
        {
            if (!await AuthenticateAsync()) return false;
        }

        try
        {
            await _driveService!.Files.Delete(fileId).ExecuteAsync();
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
        var fileInfo = await FindFileAsync(fileName);
        return fileInfo?.ModifiedTime;
    }

    /// <summary>
    /// 앱 폴더 조회 또는 생성
    /// </summary>
    private async Task<string?> GetOrCreateAppFolderAsync()
    {
        if (_driveService == null) return null;

        try
        {
            // 앱 폴더 검색
            var listRequest = _driveService.Files.List();
            listRequest.Q = $"name = '{AppFolderName}' and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
            listRequest.Spaces = "appDataFolder";
            listRequest.Fields = "files(id, name)";

            var result = await listRequest.ExecuteAsync();

            if (result.Files.Count > 0)
            {
                return result.Files[0].Id;
            }

            // 폴더 생성
            var folderMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = AppFolderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string> { "appDataFolder" }
            };

            var createRequest = _driveService.Files.Create(folderMetadata);
            createRequest.Fields = "id";

            var folder = await createRequest.ExecuteAsync();
            return folder.Id;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 파일 검색
    /// </summary>
    private async Task<CloudFile?> FindFileAsync(string fileName)
    {
        if (_driveService == null || _appFolderId == null) return null;

        try
        {
            var listRequest = _driveService.Files.List();
            listRequest.Q = $"name = '{fileName}' and '{_appFolderId}' in parents and trashed = false";
            listRequest.Fields = "files(id, name, modifiedTime, size)";

            var result = await listRequest.ExecuteAsync();

            if (result.Files.Count == 0) return null;

            var file = result.Files[0];
            return new CloudFile
            {
                Id = file.Id,
                Name = file.Name,
                ModifiedTime = file.ModifiedTimeDateTimeOffset?.DateTime ?? DateTime.MinValue,
                Size = file.Size ?? 0
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 토큰 저장 경로
    /// </summary>
    private static string GetTokenStorePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OtpAuthenticator",
            "google_tokens");
    }
}
