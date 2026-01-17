using System;
using System.Runtime.CompilerServices;

namespace Chatroom.AI.Utils;

public static class Assert
{
    public static void Argument(bool condition, [CallerArgumentExpression(nameof(condition))] string? expression = null)
    {
        if (!condition)
        {
            throw new ArgumentException($"Assertion failed: {expression}");
        }
    }

    public static void NotNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? expression = null)
        where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(expression, $"Value must not be null: {expression}");
        }
    }

    public static void NotNullOrEmpty(string value, [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentNullException($"String must not be null or empty! {expression}");
        }
    }

    public static void State(bool condition, [CallerArgumentExpression(nameof(condition))] string? expression = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Invalid state: {expression}");
        }
    }
}
