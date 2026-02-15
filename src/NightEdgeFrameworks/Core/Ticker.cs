using System;

namespace NightEdgeFrameworks.Core;

public class Ticker
{
    private EventPool _eventPool;

    public event Action<double> OnUpdate;
    public event Action<double> OnFixedUpdate;

    public Ticker(EventPool eventPool)
    {
        _eventPool = eventPool;
        OnUpdate += _eventPool.Update;
    }

    public void Update(double deltaTime) => OnUpdate?.Invoke(deltaTime);
    public void FixedUpdate(double deltaTime) => OnFixedUpdate?.Invoke(deltaTime);
}

public class NefxEngine{
    private Ticker _ticker;
    private double _lastFrameTime = 0.0;
    private double DeltaTime => DateTime.Now.Second - _lastFrameTime;
    public NefxEngine(Ticker ticker)
    {
        _ticker = ticker;
    }

    public void Run()
    {
        while (true)
        {
            Thread.Sleep(1);
            Task.Run(() => { _ticker.FixedUpdate(1 / 60.0); });
            _ticker.Update(0.01);
            _lastFrameTime = DateTime.Now.Second;
        }
    }
    public async Task RunAsync()
    {
        while (true)
        {
            await Task.Delay(10);
            await Task.Run(() =>
            {
                _ticker.FixedUpdate(1 / 60.0);
            });
            _ticker.Update(0.01);
            _lastFrameTime = DateTime.Now.Second;
        }
    }
}