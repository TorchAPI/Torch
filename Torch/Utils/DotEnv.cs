using System;
using System.IO;
using System.Linq;
using System.Text;
using NLog;

namespace Torch.Utils
{
    public static class DotEnv
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public static void Load()
        {
            const string filePath = ".env";
            if (!File.Exists(filePath)) return;

            var txt = File.ReadAllText(filePath, Encoding.Default);
            var newline = new[] { "\r\n", Environment.NewLine };
            var equal = new[] { "=" };
            foreach (var line in txt.Split(newline, StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = line.Split(equal, StringSplitOptions.None);
                if (pair.Length == 0) continue;

                var key = pair[0].Trim();
                if (pair.Length == 1)
                {
                    Environment.SetEnvironmentVariable(key, null);
                    Log.Info($"{key} (no value)");
                    continue;
                }

                var value = pair[1].Trim();
                Log.Info($"{key} = {Mask(value)} (masked)");
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        static string Mask(string value)
        {
            return string.Join("", Enumerable.Repeat('*', value.Length));
        }
    }
}