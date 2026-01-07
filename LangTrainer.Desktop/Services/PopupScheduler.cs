using System;
using System.Threading;
using Avalonia.Threading;

namespace LangTrainer.Desktop.Services;

public sealed class PopupScheduler : IDisposable
{
    private Timer? _timer;

    public void Start(TimeSpan interval, Action onTick)
    {
        Stop();

        _timer = new Timer(_ =>
        {
            // Ensure UI thread
            Dispatcher.UIThread.Post(() => onTick());
        }, null, interval, interval);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose()
    {
        Stop();
    }
}
