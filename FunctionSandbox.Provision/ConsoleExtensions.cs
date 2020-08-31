using System;

namespace Provision
{
    public static class ConsoleExtensions
    {
        public static void WriteLine(this ConsoleColor foregroundColor, string value)
        {
            var originalForegroundColor = Console.ForegroundColor;

            Console.ForegroundColor = foregroundColor;
            Console.WriteLine(value);

            Console.ForegroundColor = originalForegroundColor;
        }

        public static void Write(this ConsoleColor foregroundColor, string value)
        {
            var originalForegroundColor = Console.ForegroundColor;
            Console.ForegroundColor = foregroundColor;
            Console.Write(value);
            Console.ForegroundColor = originalForegroundColor;
        }

    }
}