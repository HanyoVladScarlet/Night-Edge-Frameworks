using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using NightEdgeFrameworks.Core;

namespace NightEdgeFrameworks;

public interface INefxGameEngine
{
  double CurrentTime { get; }
  double DeltaTime { get; }
  event Action OnUpdate;
  event Action OnFixedUpdate;
  event Action OnStart;
  event Action OnPause;
  event Action OnResume;
  event Action OnStop;

  T GetService<T>() where T : class;

  // 其他引擎公开方法...
  Task RunAsync(CancellationToken cancellationToken = default);
  Task PauseAsync(CancellationToken cancellationToken = default);
  Task ResumeAsync(CancellationToken cancellationToken = default);
}

public class NefxGameEngine : INefxGameEngine, IHostedService
{
  private readonly IServiceProvider _services;
  private readonly ILogger<NefxGameEngine> _logger;
  private CancellationTokenSource _cts;
  private Task _gameLoopTask;
  private double _currentTime;
  public double DeltaTime => _deltaTime;
  private double _deltaTime;

  private double _frameRate = 60.0;
  private Stopwatch? _stopwatch;
  private Stopwatch Stopwatch => _stopwatch ??= new();
  public double CurrentTime => _currentTime;

  #region Public Events

  public event Action OnUpdate;
  public event Action OnFixedUpdate;
  public event Action OnStart;
  public event Action OnPause;
  public event Action OnResume;
  public event Action OnStop;

  #endregion

  private static INefxGameEngine _instance;

  public static INefxGameEngine Instance => _instance;
// private Ticker _ticker;
  public NefxGameEngine(IServiceProvider services, ILogger<NefxGameEngine> logger = null)
  {
    _services = services;
    _logger = logger;
    _instance = this;
  }

  public static GameEngineBuilder CreateBuilder() => CreateBuilder(new ServiceCollection());

  public static GameEngineBuilder CreateBuilder(IServiceCollection services) =>
    new GameEngineBuilder(services, new ConfigurationBuilder());

  public T GetService<T>() where T : class
    => _services.GetService<T>();

  /// <summary>
  /// 直接启动游戏循环.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task RunAsync(CancellationToken cancellationToken = default) =>
    await StartAsync(cancellationToken);

  /// <summary>
  /// 直接退出游戏循环.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task PauseAsync(CancellationToken cancellationToken) => await StopAsync(cancellationToken);

  public Task StartAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("Game engine starting...");
    OnStart?.Invoke();
    Stopwatch.Start();
    _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    _gameLoopTask = Task.Run(RunGameLoop, _cts.Token);
    return Task.CompletedTask;
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("Game engine stopping...");
    OnStop?.Invoke();
    _cts?.Cancel();
    if (_gameLoopTask != null)
    {
      await _gameLoopTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
  }

  public async Task ResumeAsync(CancellationToken cancellationToken)
  {
    OnResume += () => _logger.LogInformation("Game engine resuming...");
    OnResume?.Invoke();
    _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    _gameLoopTask = Task.Run(RunGameLoop, _cts.Token);
    await Task.CompletedTask;
  }

  private void RunGameLoop()
  {
    while (!_cts.IsCancellationRequested)
    {
      var ticker = _services.GetService<Ticker>();
      Thread.Sleep(1000 / (int)_frameRate); // ~60 FPS
      ticker?.Update(DeltaTime);
      Stopwatch.Stop();
      _deltaTime = Stopwatch.Elapsed.TotalSeconds - _currentTime;
      _currentTime = Stopwatch.Elapsed.TotalSeconds;
      // 帧循环逻辑
      Stopwatch.Start();
    }
  }
}

public class GameEngineBuilder
{
  
  private readonly IConfigurationBuilder _configurationBuilder;
  private readonly IServiceCollection _services;
  private Action<ILoggingBuilder>? _loggingBuilderAction = default;
  private readonly List<Action<IServiceCollection>> _configureServicesActions = new();
  public GameEngineBuilder(IServiceCollection services, IConfigurationBuilder configurationBuilder)
  {
    _services = services;
    _configurationBuilder = configurationBuilder;
  }

  public GameEngineBuilder ConfigureServices(Action<IServiceCollection> configureDelegate)
  {
    if (configureDelegate == null) throw new ArgumentNullException(nameof(configureDelegate));
    _configureServicesActions.Add(configureDelegate);
    return this; // 支持链式调用
  }

  // 核心注册方法：将服务注册委托给容器
  public GameEngineBuilder RegisterService<TService, TImplementation>()
    where TService : class
    where TImplementation : class, TService
  {
    _services.AddSingleton<TService, TImplementation>();
    return this;
  }

  // 支持已创建实例的注册
  public GameEngineBuilder RegisterService<TService>(TService implementation)
    where TService : class
  {
    _services.AddSingleton<TService>(implementation);
    return this;
  }

  // 内部调用，完成引擎自身注册
  public INefxGameEngine Build()
  {
    // 1. 生成配置对象
    var configuration = _configurationBuilder.Build();
    _services.AddSingleton<IConfiguration>(configuration);

    // 2. 配置日志
    _services.AddLogging(builder =>
    {
      builder.AddConsole(options => options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ");
      // // 如果用户未配置任何日志，则添加默认控制台日志
      // if (_loggingBuilderAction == null)
      // {
      //   builder.AddConsole(options => options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ");
      // }
      // else
      // {
      //   _loggingBuilderAction(builder);
      // }
    });
    // 3. 注册 GameEngine 自身
    _services.AddSingleton<INefxGameEngine, NefxGameEngine>();
    _services.AddHostedService<NefxGameEngine>(sp => sp.GetRequiredService<INefxGameEngine>() as NefxGameEngine);
    // 4. 构建服务提供程序
    var serviceProvider = _services.BuildServiceProvider();
    var res = serviceProvider.GetRequiredService<INefxGameEngine>();
    
    // 5. 从容器中获取 GameEngine 实例（此时所有依赖已注入）
    return res;
  }
}

/// <summary>
/// 引擎构建器扩展，包括引擎的一些组件的启用
/// </summary>
public static class GameEngineBuilderExtensions
{
  public static GameEngineBuilder ConfigureServices(this GameEngineBuilder builder, Action<IServiceCollection> configure)
  {
    return builder;
  }
  public static GameEngineBuilder UseDefaultServices(this GameEngineBuilder builder)
  {
    return builder.RegisterService<Ticker, Ticker>()
      .RegisterService<EventPool, EventPool>();
  }
}

public static class GameEngineServiceCollectionExtensions
{
  /// <summary>
  /// 在使用Microsoft.Extensions.Hosting 创建游戏引擎时，可以使用此方法添加游戏引擎的默认服务
  /// </summary>
  /// <param name="services"></param>
  /// <param name="configure"></param>
  /// <returns></returns>
  public static IServiceCollection AddNefxGameEngine(
    this IServiceCollection services,
    Action<GameEngineBuilder> configure)
  {
    var builder = NefxGameEngine.CreateBuilder(services);
    configure(builder);
    builder.Build(); // 注册 GameEngine 本身及托管服务
    return services;
  }
}