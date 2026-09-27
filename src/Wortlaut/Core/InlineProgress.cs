namespace Wortlaut.Core;

/// <summary>
/// <see cref="IProgress{T}"/> that invokes the handler synchronously on the reporting thread.
/// Used to forward updates inside the core; the UI uses <see cref="Progress{T}"/> to get onto its thread.
/// </summary>
internal sealed class InlineProgress<T>(Action<T> handler) : IProgress<T>
{
    public void Report(T value) => handler(value);
}
