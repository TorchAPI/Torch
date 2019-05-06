using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Torch.API;

namespace Torch.Managers
{
    public class FilesystemManager : Manager
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Temporary directory for Torch that is cleared every time the program is started.
        /// </summary>
        public string TempDirectory { get; }

        /// <summary>
        /// Directory that contains the current Torch assemblies.
        /// </summary>
        public string TorchDirectory { get; }

        public FilesystemManager(ITorchBase torchInstance) : base(torchInstance)
        {
            var tmp = Path.Combine(Path.GetTempPath(), "Torch");
            var torch = new FileInfo(typeof(FilesystemManager).Assembly.Location).Directory.FullName;
            if (Path.GetPathRoot(tmp) == Path.GetPathRoot(torch))
            {
                TempDirectory = tmp;
            }
            else
            {
                TempDirectory = Directory.CreateDirectory(Path.Combine(torch, "tmp")).FullName;
                TorchDirectory = torch;

                _log.Info($"Clearing tmp directory at {TempDirectory}");
                ClearTemp();
            }
        }

        private void ClearTemp()
        {
            foreach (var file in Directory.GetFiles(TempDirectory, "*", SearchOption.AllDirectories))
                File.Delete(file);
        }

        /// <summary>
        /// Move the given file (if it exists) to a temporary directory that will be cleared the next time the application starts.
        /// </summary>
        public void SoftDelete(string path, string file)
        {
            string source = Path.Combine(path, file);
            if (!File.Exists(source))
                return;
            var rand = Path.GetRandomFileName();
            var dest = Path.Combine(TempDirectory, rand);
            File.Move(source, rand);
            string rsource = Path.Combine(path, rand);
            File.Move(rsource, dest);
        }
    }
}
