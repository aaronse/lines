namespace Lines;

internal static class Log
{
    internal static void Info(string message) => Console.WriteLine(message);

    internal static void Error(string message) => Console.Error.WriteLine(message);
}
