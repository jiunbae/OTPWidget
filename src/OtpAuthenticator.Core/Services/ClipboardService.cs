using OtpAuthenticator.Core.Services.Interfaces;
using Windows.ApplicationModel.DataTransfer;

namespace OtpAuthenticator.Core.Services;

/// <summary>
/// 클립보드 서비스
/// </summary>
public class ClipboardService : IClipboardService
{
    private CancellationTokenSource? _autoClearCts;

    /// <summary>
    /// 텍스트를 클립보드에 복사
    /// </summary>
    public async Task CopyAsync(string text, int autoClearSeconds = 0)
    {
        // 기존 타이머 취소
        CancelAutoClear();

        var dataPackage = new DataPackage();
        dataPackage.SetText(text);

        // 클립보드 히스토리에서 제외 (Windows 10 1809+)
        dataPackage.Properties.Add("ExcludeFromClipboardHistory", true);

        Clipboard.SetContent(dataPackage);

        // 자동 삭제 설정
        if (autoClearSeconds > 0)
        {
            _autoClearCts = new CancellationTokenSource();
            var token = _autoClearCts.Token;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(autoClearSeconds), token);
                if (!token.IsCancellationRequested)
                {
                    await ClearAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // 타이머가 취소됨 (정상)
            }
        }
    }

    /// <summary>
    /// 클립보드 내용 삭제
    /// </summary>
    public Task ClearAsync()
    {
        try
        {
            Clipboard.Clear();
        }
        catch
        {
            // 클립보드 삭제 실패 무시
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 클립보드에서 텍스트 가져오기
    /// </summary>
    public async Task<string?> GetTextAsync()
    {
        try
        {
            var content = Clipboard.GetContent();
            if (content.Contains(StandardDataFormats.Text))
            {
                return await content.GetTextAsync();
            }
        }
        catch
        {
            // 클립보드 접근 실패
        }

        return null;
    }

    /// <summary>
    /// 자동 삭제 타이머 취소
    /// </summary>
    public void CancelAutoClear()
    {
        _autoClearCts?.Cancel();
        _autoClearCts?.Dispose();
        _autoClearCts = null;
    }
}
