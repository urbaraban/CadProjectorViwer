using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace CadProjector.Logging.Services;

/// <summary>Singleton progress stack for long-running UI operations.</summary>
public sealed class CadProgress : NotifyBase
{
    public static CadProgress Inst { get; } = new();

    public static Guid Start(string name, int max, Action? cancelationCommand = null)
    {
        var task = new CadProgressTask
        {
            Name = name,
            Maximum = max,
            CancelationAction = cancelationCommand,
        };
        Inst.Add(task);
        return task.Uid;
    }

    /// <summary>Indeterminate waiter overlay with optional auto-timeout.</summary>
    public static Guid Waiter(
        string name,
        LogMessageStatus progressStatus = LogMessageStatus.Regular,
        int timeoutMs = 0,
        Action? cancelationCommand = null)
    {
        var task = new CadProgressTask(isBusy: true)
        {
            Name = name,
            Status = progressStatus,
            Maximum = 0,
            CancelationAction = cancelationCommand,
        };
        Inst.Add(task);

        if (timeoutMs > 0)
            _ = Task.Delay(timeoutMs).ContinueWith(_ => End(task.Uid));

        return task.Uid;
    }

    public static bool Set(Guid guid, int value)
    {
        if (Inst.Get(guid) is not CadProgressTask task) return false;
        Inst.SetValue(task, value);
        return true;
    }

    public static bool Set(Guid guid, int value, string message)
    {
        if (Inst.Get(guid) is not CadProgressTask task) return false;
        Inst.SetValue(task, value);
        Inst.SetMessage(task, message);
        return true;
    }

    public static bool Add(Guid guid, int value)
    {
        if (Inst.Get(guid) is not CadProgressTask task) return false;
        Inst.SetValue(task, task.Value + value);
        return true;
    }

    public static bool Next(Guid guid, string message)
    {
        if (Inst.Get(guid) is not CadProgressTask task) return false;
        Inst.SetValue(task, task.Value + 1);
        Inst.SetMessage(task, message);
        return true;
    }

    public static bool Next(Guid guid)
    {
        if (Inst.Get(guid) is not CadProgressTask task) return false;
        Inst.SetValue(task, task.Value + 1);
        return true;
    }

    public static void End(Guid guid) => Inst.EndProgress(guid);

    public static void Clear() => Inst.ClearAll();

    public event EventHandler? ProgressCollectionChanged;

    public ObservableCollection<CadProgressTask> Tasks { get; } = [];

    public CadProgressTask? LastProgress => Tasks.Count > 0 ? Tasks[^1] : null;

    public bool HasActive => Tasks.Count > 0;

    private SynchronizationContext _synchronizationContext;
    private readonly object _gate = new();

    private CadProgress()
    {
        _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
        Tasks.CollectionChanged += OnTasksChanged;
    }

    public void SetSynchronizationContext(SynchronizationContext? synchronizationContext)
        => _synchronizationContext = synchronizationContext ?? new SynchronizationContext();

    private void OnTasksChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ProgressCollectionChanged?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(LastProgress));
        OnPropertyChanged(nameof(HasActive));
    }

    private CadProgressTask? Get(Guid guid)
    {
        try
        {
            return Tasks.FirstOrDefault(x => x.Uid == guid);
        }
        catch
        {
            return null;
        }
    }

    private void Post(Action action)
        => _synchronizationContext.Post(_ => action(), null);

    internal void SetMessage(CadProgressTask task, string message)
        => Post(() => task.Name = message);

    internal void SetValue(CadProgressTask task, int value)
        => Post(() => task.Value = value);

    internal void Add(CadProgressTask progressTask)
    {
        lock (_gate)
        {
            Post(() => Tasks.Add(progressTask));
        }
    }

    public void EndProgress(Guid guid)
    {
        lock (_gate)
        {
            Post(() =>
            {
                try
                {
                    for (var i = 0; i < Tasks.Count; i++)
                    {
                        if (Tasks[i].Uid != guid) continue;
                        Tasks[i].End();
                        Tasks.RemoveAt(i);
                        return;
                    }
                }
                catch
                {
                    CadLogging.Post?.Invoke("Progress end failed", LogMessageStatus.Warning);
                }
            });
        }
    }

    public void ClearAll()
    {
        Post(() =>
        {
            lock (_gate)
            {
                try
                {
                    foreach (var task in Tasks)
                        task.End();
                    Tasks.Clear();
                }
                catch
                {
                    CadLogging.Post?.Invoke("Progress clear failed", LogMessageStatus.Warning);
                }
            }
        });
    }
}

public sealed class CadProgressTask : NotifyBase
{
    public string Label => Name;
    public string ProgressLabel => $"{Value}/{Maximum}";

    public bool IsBusy { get; }

    public bool IsIndeterminate => IsBusy || Maximum <= 0;

    public double Fraction => Maximum > 0 ? Math.Clamp((double)Value / Maximum, 0, 1) : 0;

    public LogMessageStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }
    private LogMessageStatus _status = LogMessageStatus.Regular;

    public Guid Uid { get; } = Guid.NewGuid();

    public int Value
    {
        get => _maximum > 0 ? Math.Min(_value, _maximum) : _value;
        set
        {
            _value = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Label));
            OnPropertyChanged(nameof(ProgressLabel));
            OnPropertyChanged(nameof(Fraction));
            OnPropertyChanged(nameof(IsIndeterminate));
        }
    }
    private int _value;

    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Label));
            OnPropertyChanged(nameof(ProgressLabel));
            OnPropertyChanged(nameof(Fraction));
            OnPropertyChanged(nameof(IsIndeterminate));
        }
    }
    private int _maximum = 100;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Label));
        }
    }
    private string _name = string.Empty;

    public ICommand CancelationCommand => new LoggingRelayCommand(
        _ => CancelationAction is not null,
        _ => CancelationAction?.Invoke());

    public Action? CancelationAction
    {
        get => _cancelationAction;
        set
        {
            _cancelationAction = value;
            OnPropertyChanged();
        }
    }
    private Action? _cancelationAction;

    internal CadProgressTask(bool isBusy = false, LogMessageStatus progressStatus = LogMessageStatus.Regular)
    {
        IsBusy = isBusy;
        if (progressStatus != LogMessageStatus.Regular)
            Status = progressStatus;
    }

    internal void End()
    {
        if (Maximum > 0)
            Value = Maximum;
    }
}
