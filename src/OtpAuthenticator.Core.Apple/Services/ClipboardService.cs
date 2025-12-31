using OtpAuthenticator.Core.Services.Interfaces;

#if MACCATALYST
using AppKit;
#else
using UIKit;
#endif

namespace OtpAuthenticator.Core.Apple.Services;

/// <summary>
/// Apple 플랫폼 클립보드 서비스
/// </summary>
public class ClipboardService : IClipboardService
{
    private CancellationTokenSource? _autoClearCts;

    public async Task CopyAsync(string text, int autoClearSeconds = 0)
    {
        CancelAutoClear();

#if MACCATALYST
        var pasteboard = NSPasteboard.GeneralPasteboard;
        pasteboard.ClearContents();
        pasteboard.SetStringForType(text, NSPasteboard.NSPasteboardTypeString);
#else
        UIPasteboard.General.String = text;
#endif

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
#if MACCATALYST
            var pasteboard = NSPasteboard.GeneralPasteboard;
            pasteboard.ClearContents();
#else
            UIPasteboard.General.String = string.Empty;
#endif
        }
        catch
        {
        }

        return Task.CompletedTask;
    }

    public Task<string?> GetTextAsync()
    {
        try
        {
#if MACCATALYST
            var pasteboard = NSPasteboard.GeneralPasteboard;
            var text = pasteboard.GetStringForType(NSPasteboard.NSPasteboardTypeString);
            return Task.FromResult<string?>(text);
#else
            return Task.FromResult<string?>(UIPasteboard.General.String);
#endif
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    public void CancelAutoClear()
    {
        _autoClearCts?.Cancel();
        _autoClearCts?.Dispose();
        _autoClearCts = null;
    }
}
