using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.API;

namespace Torch.Managers
{
    public class FilesystemManager : Manager
    {
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
            var temp = Path.Combine(Path.GetTempPath(), "Torch");
            TempDirectory = Directory.CreateDirectory(temp).FullName;
            var torch = new FileInfo(typeof(FilesystemManager).Assembly.Location).Directory.FullName;
            TorchDirectory = torch;

            ClearTemp();
        }

        private void ClearTemp()
        {
            foreach (var file in Directory.GetFiles(TempDirectory, "*", SearchOption.AllDirectories))
                File.Delete(file);
        }

        /// <summary>
        /// Move the given file (if it exists) to a temporary directory that will be cleared the next time the application starts.
        /// </summary>
        public void SoftDelete(string file)
        {
            if (!File.Exists(file))
                return;
            var rand = Path.GetRandomFileName();
            var dest = Path.Combine(TempDirectory, rand);
            File.Move(file, dest);
        }
    }
}
