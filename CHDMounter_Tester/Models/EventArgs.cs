namespace Tester.Models;

/// <summary>
///     Provides an event argument that carries a single typed value.
/// </summary>
/// <typeparam name="T">The type of the value carried by the event.</typeparam>
public class EventArgs<T> : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventArgs{T}" /> class.
    /// </summary>
    /// <param name="value">The value to carry with the event.</param>
    public EventArgs(T value)
    {
        Value = value;
    }

    /// <summary>
    ///     Gets the value carried by this event argument.
    /// </summary>
    public T Value { get; }
}