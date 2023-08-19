namespace TaskWeaverLib;
public class TaskWeaver : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Func<CancellationToken, Task> _periodicFunc;
    private readonly Func<Task>? _completionFunc;
    private readonly TimeSpan _delay;
    private PeriodicTimer? _timer;
    private Task? _task;
    private CancellationTokenSource? _cts;

    public TaskWeaver(Func<CancellationToken, Task> periodicFunc, Func<Task>? completionFunc = null, TimeSpan? delay = null)
    {
        _periodicFunc = periodicFunc ?? throw new ArgumentNullException(nameof(periodicFunc));
        _completionFunc = completionFunc;
        _delay = delay ?? TimeSpan.FromSeconds(1);
    }

    public async Task StartAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_timer is null && _cts is null)
            {
                _timer = new PeriodicTimer(_delay);
                _cts = new CancellationTokenSource();
                _task = Task.Run(async () =>
                {
                    while (await _timer.WaitForNextTickAsync(_cts.Token))
                    {
                        await _periodicFunc(_cts.Token);
                    }
                });
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StopAsync(bool forceStop = false)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_task is null)
                throw new InvalidOperationException("Can not stop a worker that has not yet been started!");

            if (forceStop)
                _cts?.Cancel();

            _timer?.Dispose();

            if (_task is not null)
            {
                await _task;
                _task.Dispose(); // Dispose the task
            }

            if (_completionFunc is not null)
                await _completionFunc();
        }
        finally
        {
            _cts?.Dispose();
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _semaphore.Dispose();
            _cts?.Dispose();
            _timer?.Dispose();
        }
    }
}
