using System;
using System.Collections.Concurrent;

namespace NightEdgeFrameworks.Core;

/// <summary>
/// 静态类，通过泛型方法为任意类型提供单例实例。
/// </summary>
public static class Singleton
{
    // 存储类型与单例实例的映射，线程安全
    private static readonly ConcurrentDictionary<Type, object> _instances = new ConcurrentDictionary<Type, object>();

    /// <summary>
    /// 获取指定类型的单例实例（要求类型有无参构造函数）。
    /// </summary>
    public static T GetInstance<T>() where T : new()
    {
        return (T)_instances.GetOrAdd(typeof(T), _ => new T());
    }

    /// <summary>
    /// 获取指定类型的单例实例，使用自定义工厂方法创建。
    /// </summary>
    public static T GetInstance<T>(Func<T> factory)
    {
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        return (T)_instances.GetOrAdd(typeof(T), _ => factory());
    }

    /// <summary>
    /// 清除所有已注册的单例（主要用于测试）。
    /// </summary>
    public static void Clear()
    {
        _instances.Clear();
    }
}