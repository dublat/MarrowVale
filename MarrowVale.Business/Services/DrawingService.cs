using MarrowVale.Business.Contracts;
using MarrowVale.Common.Contracts;
using Microsoft.Extensions.Logging;
using System;

namespace MarrowVale.Business.Services
{
    public class DrawingService : IDrawingService
    {
        private readonly ILogger<DrawingService> _logger;
        private readonly IGlobalItemsProvider _globalItemsProvider;
        public DrawingService(ILoggerFactory loggerFactory, IGlobalItemsProvider globalItemsProvider)
        {
            _logger = loggerFactory.CreateLogger<DrawingService>();
            _globalItemsProvider = globalItemsProvider;
        }

        public void PrintArtCentered(string[] art)
        {
            if (OperatingSystem.IsWindows() && !Console.IsOutputRedirected) Console.WindowWidth = _globalItemsProvider.WindowWidth;
            Console.WriteLine(Environment.NewLine);
            foreach (var line in art)
            {
                if (!Console.IsOutputRedirected)
                    Console.SetCursorPosition(Math.Max(0, (Console.WindowWidth - line.Length) / 2), Console.CursorTop);
                Console.WriteLine(line);
            }

            Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}");
        }

        public void PrintArt(string[] art)
        {
            if (OperatingSystem.IsWindows() && !Console.IsOutputRedirected) Console.WindowWidth = _globalItemsProvider.WindowWidth;
            Console.WriteLine(Environment.NewLine);
            foreach (var line in art)
            {
                if (!Console.IsOutputRedirected)
                    Console.SetCursorPosition(Math.Max(0, (Console.WindowWidth - line.Length) / 2), Console.CursorTop);
                Console.WriteLine(line);
            }

            Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}");
        }
    }
}
