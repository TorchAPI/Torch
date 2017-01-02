using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Torch.API;
using VRage.Utils;

namespace Torch
{
    public class Logger : ILogger
    {
        public string Prefix = "[TORCH]";
        private StringBuilder _sb = new StringBuilder();
        private string _path;

        public Logger(string path)
        {
            _path = path;
            if (File.Exists(_path))
                File.Delete(_path);
        }

        public void Write(string message)
        {
            var msg = $"{GetInfo()}: {message}";
            Console.WriteLine(msg);
            _sb.AppendLine(msg);
        }

        public void WriteExceptionAndThrow(Exception e)
        {
            WriteException(e);
            throw e;
        }

        public void WriteException(Exception e)
        {
            _sb.AppendLine($"{GetInfo()}: {e.Message}");

            foreach (var line in e.StackTrace.Split('\n'))
                _sb.AppendLine($"\t{line}");
        }

        private string GetInfo()
        {
            return $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()} {Prefix}";
        }

        public void Flush()
        {
            File.AppendAllText(_path, _sb.ToString());
            _sb.Clear();
        }

        ~Logger()
        {
            Flush();
        }
    }
}
