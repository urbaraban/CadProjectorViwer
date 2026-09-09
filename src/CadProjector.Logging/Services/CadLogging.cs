using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CadProjector.Logging.Services;

/// <summary>Singleton in-memory log ring, marshalled onto the UI SynchronizationContext.</summary>
public sealed class CadLogging : ObservableCollection<CadLogMessage>
{
    public event EventHandler<CadLogMessage>? LogAdded;

    private SynchronizationContext _synchronizationContext =
        SynchronizationContext.Current ?? new SynchronizationContext();

    public static PostDelegate? Post { get; set; }

    public delegate void PostDelegate(
        string message,
        LogMessageStatus alert = LogMessageStatus.Regular,
        [CallerMemberName] string? sender = null);

    public static CadLogging Instance { get; } = new();

    public int MessageCount
    {
        get => _messageCount;
        set
        {
            _messageCount = value;
            TrimOver();
        }
    }
    private int _messageCount = 500;

    public bool IsReverse
    {
        get => _isReverse;
        set
        {
            _isReverse = value;
            ReverseCollection(_isReverse);
        }
    }
    private bool _isReverse;

    public CadLogMessage? SelectedMessage
    {
        get => _selectedMessage;
        set
        {
            _selectedMessage = value;
            UpdateTimeDelta();
        }
    }
    private CadLogMessage? _selectedMessage;

    private CadLogging()
    {
        Post += Add;
    }

    public void SetSynchronizationContext(SynchronizationContext? synchronizationContext)
        => _synchronizationContext = synchronizationContext ?? new SynchronizationContext();

    public void ClearAll()
    {
        _synchronizationContext.Post(_ => Clear(), null);
    }

    private void Add(string message, LogMessageStatus alerts, string? sender)
    {
        _synchronizationContext.Post(_ =>
        {
            var msg = new CadLogMessage(message, sender ?? string.Empty, alerts)
            {
                DeltaTime = GetTimeDelta,
            };

            if (!IsReverse)
                Insert(0, msg);
            else
                Add(msg);

            TrimOne();
            LogAdded?.Invoke(this, msg);
        }, null);
    }

    private void ReverseCollection(bool reverse)
    {
        var collection = reverse
            ? this.OrderBy(x => x.DateTime).ToArray()
            : this.OrderByDescending(x => x.DateTime).ToArray();

        Clear();
        foreach (var item in collection)
            Add(item);
    }

    private void TrimOver()
    {
        while (Count > MessageCount)
        {
            if (!IsReverse)
                RemoveAt(Count - 1);
            else
                RemoveAt(0);
        }
    }

    private void TrimOne()
    {
        if (Count <= MessageCount) return;
        if (!IsReverse)
            RemoveAt(Count - 1);
        else
            RemoveAt(0);
    }

    private TimeSpan GetTimeDelta(CadLogMessage logMessage)
    {
        for (var i = 0; i < Count; i++)
        {
            if (this[i].Uid != logMessage.Uid) continue;
            var index = i + (IsReverse ? -1 : 1);
            if (index < 0 || index >= Count)
                return TimeSpan.Zero;
            return this[i].DateTime - this[index].DateTime;
        }
        return TimeSpan.Zero;
    }

    private void UpdateTimeDelta()
    {
        foreach (var msg in this)
            msg.UpdateTimeDelta();
    }
}

public sealed class CadLogMessage : NotifyBase
{
    internal delegate TimeSpan DeltaTimeDelegate(CadLogMessage message);
    internal DeltaTimeDelegate? DeltaTime { get; set; }

    public Guid Uid { get; } = Guid.NewGuid();
    public DateTime DateTime { get; } = DateTime.Now;
    public string Message { get; }
    public string Sender { get; }
    public LogMessageStatus Alerts { get; }

    public string TimeText => DateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    public string TimeDelta
    {
        get
        {
            var seconds = (DeltaTime?.Invoke(this) ?? TimeSpan.Zero).TotalMilliseconds / 1000.0;
            return $"{Math.Round(seconds, 3).ToString(CultureInfo.InvariantCulture)} s";
        }
    }

    public CadLogMessage(string message, string sender)
    {
        Message = message;
        Sender = sender;
        Alerts = LogMessageStatus.Regular;
    }

    public CadLogMessage(string message, string sender, LogMessageStatus alerts)
        : this(message, sender)
    {
        Alerts = alerts;
    }

    public void UpdateTimeDelta() => OnPropertyChanged(nameof(TimeDelta));

    public override string ToString() => $"{TimeText} {Message}";
}
