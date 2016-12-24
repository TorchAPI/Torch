using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using VRage.Utils;

namespace Torch
{
    public static class Logger
    {
        public const string Prefix = "[TORCH]";

        public static void Write(string message)
        {
            var msg = $"{Prefix}: {message}";
            MyLog.Default.WriteLineAndConsole(msg);
        }
    }
}
