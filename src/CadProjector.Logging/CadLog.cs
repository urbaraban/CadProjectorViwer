using System.Runtime.CompilerServices;
using CadProjector.Logging.Services;

namespace CadProjector.Logging;

/// <summary>Thin helpers over <see cref="CadLogging.Post"/> for library call sites.</summary>
public static class CadLog
{
    public static void Write(
        string message,
        LogMessageStatus status = LogMessageStatus.Regular,
        [CallerMemberName] string? sender = null)
        => CadLogging.Post?.Invoke(message, status, sender);

    public static void Info(string message, [CallerMemberName] string? sender = null)
        => CadLogging.Post?.Invoke(message, LogMessageStatus.Info, sender);

    public static void Good(string message, [CallerMemberName] string? sender = null)
        => CadLogging.Post?.Invoke(message, LogMessageStatus.Good, sender);

    public static void Warn(string message, [CallerMemberName] string? sender = null)
        => CadLogging.Post?.Invoke(message, LogMessageStatus.Warning, sender);

    public static void Error(string message, [CallerMemberName] string? sender = null)
        => CadLogging.Post?.Invoke(message, LogMessageStatus.Error, sender);
}
