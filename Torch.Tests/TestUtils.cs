using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Utils;

namespace Torch.Tests
{
    public sealed class TestUtils
    {
        public static void Init()
        {
            if (_torchResolver == null)
                _torchResolver = new TorchAssemblyResolver(GetGameBinaries());
        }

        private static string GetGameBinaries()
        {
            string dir = Environment.CurrentDirectory;
            while (!string.IsNullOrWhiteSpace(dir))
            {
                string gameBin = Path.Combine(dir, "GameBinaries");
                if (Directory.Exists(gameBin))
                    return gameBin;

                dir = Path.GetDirectoryName(dir);
            }
            throw new Exception("GetGameBinaries failed to find a folder named GameBinaries in the directory tree");
        }

        private static TorchAssemblyResolver _torchResolver;
    }
}
