using System;
using System.Collections.Generic;

namespace NightEdgeFrameworks.Core;

// <summary>
/// 泛型最小堆，使用 List 作为底层存储。
/// 要求元素实现 IComparable<T>，或者传入比较委托。
/// </summary>
public class MinHeap<T>
{
    private readonly List<T> _elements;
    private readonly IComparer<T> _comparer;

    public int Count => _elements.Count;

    public MinHeap() : this(Comparer<T>.Default) { }

    public MinHeap(IComparer<T> comparer)
    {
        _elements = new List<T>();
        _comparer = comparer ?? Comparer<T>.Default;
    }

    /// <summary> 插入元素 </summary>
    public void Push(T item)
    {
        _elements.Add(item);
        SiftUp(_elements.Count - 1);
    }

    /// <summary> 获取堆顶元素，不移除 </summary>
    public T Peek()
    {
        if (_elements.Count == 0)
            throw new InvalidOperationException("Heap is empty.");
        return _elements[0];
    }

    /// <summary> 移除并返回堆顶元素 </summary>
    public T Pop()
    {
        if (_elements.Count == 0)
            throw new InvalidOperationException("Heap is empty.");

        T root = _elements[0];
        int lastIndex = _elements.Count - 1;
        _elements[0] = _elements[lastIndex];
        Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}, {lastIndex}, {_elements.Count - 1}");
        _elements.RemoveAt(lastIndex);
        if (_elements.Count > 0) 
            SiftDown(0);
        return root;
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (_comparer.Compare(_elements[index], _elements[parent]) >= 0)
                break;
            Swap(index, parent);
            index = parent;
        }
    }

    private void SiftDown(int index)
    {
        int last = _elements.Count - 1;
        while (true)
        {
            int left = index * 2 + 1;
            int right = index * 2 + 2;
            int smallest = index;

            if (left <= last && _comparer.Compare(_elements[left], _elements[smallest]) < 0)
                smallest = left;
            if (right <= last && _comparer.Compare(_elements[right], _elements[smallest]) < 0)
                smallest = right;

            if (smallest == index)
                break;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int i, int j)
    {
        T tmp = _elements[i];
        _elements[i] = _elements[j];
        _elements[j] = tmp;
    }
}

/// <summary>
/// 事件类，包含触发时间、回调、循环信息及取消标记。
/// 实现 IComparable<Event> 以便在最小堆中按 TriggerTime 排序。
/// </summary>
public class Event : IComparable<Event>
{
    /// <summary> 绝对触发时间（通常使用相对某个起点的秒数）</summary>
    public double TriggerTime { get; set; }

    /// <summary> 循环间隔（秒），仅当 IsLoop = true 时有效 </summary>
    public double Interval { get; set; }

    /// <summary> 是否为循环事件 </summary>
    public bool IsLoop { get; set; }

    /// <summary> 是否已被取消 </summary>
    public bool IsCanceled { get; set; }

    /// <summary> 事件触发时执行的回调 </summary>
    public Action Callback { get; set; }

    public int CompareTo(Event other)
    {
        if (other == null) return 1;
        return TriggerTime.CompareTo(other.TriggerTime);
    }
}

/// <summary>
/// 基于 DeltaTime 与最小堆的事件池。
/// </summary>
public class EventPool
{
    private readonly MinHeap<Event> _heap = new MinHeap<Event>();
    private double _currentTime;   // 当前逻辑时间（秒）

    /// <summary> 当前逻辑时间 </summary>
    public double CurrentTime => _currentTime;

    /// <summary>
    /// 添加一个一次性延迟事件。
    /// </summary>
    /// <param name="delay">延迟秒数</param>
    /// <param name="callback">回调</param>
    public void AddEvent(double delay, Action callback)
    {
        AddEvent(delay, callback, false, 0);
    }

    /// <summary>
    /// 添加一个循环事件。
    /// </summary>
    /// <param name="delay">首次触发延迟</param>
    /// <param name="interval">循环间隔</param>
    /// <param name="callback">回调</param>
    public void AddLoopEvent(double delay, double interval, Action callback)
    {
        AddEvent(delay, callback, true, interval);
    }

    private void AddEvent(double delay, Action callback, bool isLoop, double interval)
    {
        var evt = new Event
        {
            TriggerTime = _currentTime + delay,
            Callback = callback,
            IsLoop = isLoop,
            Interval = interval,
            IsCanceled = false
        };
        _heap.Push(evt);
    }

    /// <summary>
    /// 直接添加已构造好的 Event 对象（通常由外部管理）。
    /// </summary>
    public void AddEvent(Event evt)
    {
        _heap.Push(evt);
    }

    /// <summary>
    /// 取消事件。仅标记取消，实际移除发生在出堆时。
    /// </summary>
    public void CancelEvent(Event evt)
    {
        if (evt != null)
            evt.IsCanceled = true;
    }

    /// <summary>
    /// 推进时间，触发所有到期事件。
    /// </summary>
    /// <param name="deltaTime">时间增量（秒）</param>
    public void Update(double deltaTime)
    {
        _currentTime += deltaTime;

        while (_heap.Count > 0 && _heap.Peek().TriggerTime <= _currentTime)
        {
            Event evt = _heap.Pop();

            // 跳过已取消的事件
            if (evt.IsCanceled)
                continue;

            // 触发回调
            evt.Callback?.Invoke();

            // 循环事件重新入堆
            if (evt.IsLoop)
            {
                evt.TriggerTime = _currentTime + evt.Interval;
                _heap.Push(evt);
            }
        }
    }
}