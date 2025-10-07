using System;
using System.IO;
using System.Text;

namespace Torch.Utils
{
    public static class DotEnv
    {
        public static void Load()
        {
            const string filePath = ".env";
            if (!File.Exists(filePath)) return;

            var txt = File.ReadAllText(filePath, Encoding.Default);
            var newline = new[] { "\r\n", Environment.NewLine };
            var equal = new[] { "=" };
            foreach (var cmp in txt.Split(newline, StringSplitOptions.RemoveEmptyEntries))
            {
                var strArray = cmp.Split(equal, StringSplitOptions.None);
                switch (strArray.Length)
                {
                    case 0:
                        continue;
                    case 1:
                        Environment.SetEnvironmentVariable(strArray[0].Trim(), null);
                        continue;
                    default:
                        Environment.SetEnvironmentVariable(strArray[0].Trim(), strArray[1].Trim());
                        continue;
                }
            }
        }
    }
}