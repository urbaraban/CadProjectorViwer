namespace CadProjector.Core.Editing;

/// <summary>One reversible change, already applied to the model when it reaches the history.</summary>
public interface IEditAction
{
    string Label { get; }

    void Apply();

    void Revert();

    /// <summary>True when before and after match, so the history can drop the entry.</summary>
    bool IsNoOp { get; }

    /// <summary>
    /// Swallow an earlier action on the same target so a continuous gesture stays a single
    /// history entry. Returns false when the two are not compatible.
    /// </summary>
    bool TryAbsorb(IEditAction earlier);
}

/// <summary>A change to one value, restored through the setter it was captured with.</summary>
public sealed class ValueEdit<T> : IEditAction
{
    private readonly Action<T> _set;
    private readonly T _after;
    private T _before;

    public ValueEdit(string label, Action<T> set, T before, T after)
    {
        Label = label;
        _set = set;
        _before = before;
        _after = after;
    }

    public string Label { get; }

    public bool IsNoOp => EqualityComparer<T>.Default.Equals(_before, _after);

    public void Apply() => _set(_after);

    public void Revert() => _set(_before);

    public bool TryAbsorb(IEditAction earlier)
    {
        if (earlier is not ValueEdit<T> prior)
            return false;
        _before = prior._before;
        return true;
    }
}

/// <summary>Several already-applied edits undone and redone together (multi-object snap, etc.).</summary>
public sealed class CompositeEdit : IEditAction
{
    private readonly IReadOnlyList<IEditAction> _steps;

    public CompositeEdit(string label, IEnumerable<IEditAction> steps)
    {
        Label = label;
        _steps = steps.ToList();
    }

    public string Label { get; }

    public bool IsNoOp => _steps.Count == 0 || _steps.All(s => s.IsNoOp);

    public void Apply()
    {
        foreach (var s in _steps)
            s.Apply();
    }

    public void Revert()
    {
        for (var i = _steps.Count - 1; i >= 0; i--)
            _steps[i].Revert();
    }

    public bool TryAbsorb(IEditAction earlier)
    {
        if (earlier is not CompositeEdit prior || prior._steps.Count != _steps.Count)
            return false;
        for (var i = 0; i < _steps.Count; i++)
        {
            if (!_steps[i].TryAbsorb(prior._steps[i]))
                return false;
        }

        return true;
    }
}

/// <summary>
/// Undo/redo stack. Actions arrive already applied — a drag mutates the model as it goes, and the
/// history only needs to know how to get back. Consecutive changes sharing a merge key collapse
/// into one entry, so a drag or a burst of spinner clicks undoes in a single step.
/// </summary>
public sealed class EditHistory
{
    private static readonly TimeSpan MergeWindow = TimeSpan.FromMilliseconds(800);

    private readonly List<IEditAction> _done = [];
    private readonly List<IEditAction> _undone = [];
    private string? _openKey;
    private DateTime _openedAt;

    public int Capacity { get; init; } = 200;

    public bool CanUndo => _done.Count > 0;
    public bool CanRedo => _undone.Count > 0;

    public string? UndoLabel => CanUndo ? _done[^1].Label : null;
    public string? RedoLabel => CanRedo ? _undone[^1].Label : null;

    public event Action? Changed;

    /// <summary>Records a change that the caller has already applied.</summary>
    public void Push(IEditAction action, string? mergeKey = null)
    {
        // Panels mirroring the model back into themselves produce plenty of these.
        if (action.IsNoOp)
            return;

        _undone.Clear();

        var mergeable = mergeKey is not null
            && _openKey == mergeKey
            && DateTime.UtcNow - _openedAt < MergeWindow
            && _done.Count > 0
            && action.TryAbsorb(_done[^1]);

        if (mergeable)
            _done[^1] = action;
        else
        {
            _done.Add(action);
            if (_done.Count > Capacity)
                _done.RemoveAt(0);
        }

        _openKey = mergeKey;
        _openedAt = DateTime.UtcNow;
        Changed?.Invoke();
    }

    /// <summary>Seals the current entry so the next change starts a new one (e.g. mouse released).</summary>
    public void Break() => _openKey = null;

    public bool Undo()
    {
        if (_done.Count == 0)
            return false;
        var action = _done[^1];
        _done.RemoveAt(_done.Count - 1);
        action.Revert();
        _undone.Add(action);
        Break();
        Changed?.Invoke();
        return true;
    }

    public bool Redo()
    {
        if (_undone.Count == 0)
            return false;
        var action = _undone[^1];
        _undone.RemoveAt(_undone.Count - 1);
        action.Apply();
        _done.Add(action);
        Break();
        Changed?.Invoke();
        return true;
    }

    public void Clear()
    {
        _done.Clear();
        _undone.Clear();
        Break();
        Changed?.Invoke();
    }
}
