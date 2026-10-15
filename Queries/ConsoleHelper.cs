namespace OnlineStoreLINQ.Queries;

// Small helper to keep the printing code short.
public static class ConsoleHelper
{
    public static void Title(int number, string text)
    {
        Console.WriteLine();
        Console.WriteLine($"=== Question {number:00}: {text} ===");
    }

    public static void Title(string text)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {text} ===");
    }

    public static void IfEmpty(int count)
    {
        if (count == 0) Console.WriteLine("(no results)");
    }
}
