// DarkParchmentUI/Logger.cs


using System;
using UnityModManagerNet;

namespace DarkParchmentUI
{
    internal sealed class Logger
    {
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        public Logger(UnityModManager.ModEntry.ModLogger logger) => _logger = logger;

        public void Log(string s) => _logger.Log(s);
        public void Warning(string s) => _logger.Warning(s);
        public void Error(string s) => _logger.Error(s);

        public void Error(Exception e)
        {
            if (e == null) return;
            _logger.Error($"{e.Message}\n{e.StackTrace}");
            if (e.InnerException != null) Error(e.InnerException);
        }
        // Intentionally no Debug logging in this build (keeps overhead minimal).
    }
}
