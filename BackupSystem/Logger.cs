using System;

public static class Logger
{
    private static readonly object _lock = new();

    public static void Info(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[INFO] {message}");
            Console.ResetColor();
        }
    }

    public static void Success(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[OK] {message}");
            Console.ResetColor();
        }
    }

    public static void Error(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}");
            Console.ResetColor();
        }
    }

    public static void ListEntry(string source, string target)
    {
        lock (_lock)
        {
            Console.WriteLine($" * Źródło: '{source}' -> Cel: '{target}'");
        }
    }
}