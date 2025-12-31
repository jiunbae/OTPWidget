using OtpAuthenticator.Core.Services.Interfaces;
using Windows.ApplicationModel.DataTransfer;

namespace OtpAuthenticator.Core.Windows.Services;

/// <summary>
/// Windows 클립보드 서비스
/// </summary>
public class ClipboardService : IClipboardService
{
    private CancellationTokenSource? _autoClearCts;

    public async Task CopyAsync(string text, int autoClearSeconds = 0)
    {
        CancelAutoClear();

        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        dataPackage.Properties.Add("ExcludeFromClipboardHistory", true);

        Clipboard.SetContent(dataPackage);

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
            }
        }
    }

    public Task ClearAsync()
    {
        try
        {
            Clipboard.Clear();
        }
        catch
        {
        }

        return Task.CompletedTask;
    }

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
        }

        return null;
    }

    public void CancelAutoClear()
    {
        _autoClearCts?.Cancel();
        _autoClearCts?.Dispose();
        _autoClearCts = null;
    }
}
