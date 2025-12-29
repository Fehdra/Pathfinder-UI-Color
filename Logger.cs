// DarkParchmentUI/Logger.cs
// C# 7.3 compatible

using System;
using System.Diagnostics;
using System.Reflection;
using UnityModManagerNet;

namespace DarkParchmentUI
{
    internal class Logger
    {
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        public Logger(UnityModManager.ModEntry.ModLogger logger) => _logger = logger;

        public void Log(string s) => _logger.Log(s);
        public void Warning(string s) => _logger.Warning(s);
        public void Error(string s) => _logger.Error(s);

        public void Error(Exception e)
        {
            _logger.Error($"{e.Message}\n{e.StackTrace}");
            if (e.InnerException != null) Error(e.InnerException);
        }

        [Conditional("DEBUG")]
        public void Debug(string s) => _logger.Log(s);

        [Conditional("DEBUG")]
        public void Debug(MethodBase m, params object[] args)
            => _logger.Log($"{m.DeclaringType?.Name}.{m.Name}({string.Join(", ", args)})");
    }
}
