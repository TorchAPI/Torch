using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Utils;
using Torch.Server.ViewModels;
using VRage.Game;

namespace Torch.Server.Managers
{
    public class DSConfigManager
    {
        public ConfigDedicatedViewModel Config { get; set; }
        private ConfigDedicatedViewModel _viewModel;

        public DSConfigManager()
        {
            //Config.
        }

        /// <summary>
        /// Creates a skeleton of a DS instance folder at the given directory.
        /// </summary>
        /// <param name="path"></param>
        public void CreateInstance(string path)
        {
            if (Directory.Exists(path))
                return;

            Directory.CreateDirectory(path);
            var saves = Path.Combine(path, "Saves");
            Directory.CreateDirectory(saves);
            var mods = Path.Combine(path, "Mods");
            Directory.CreateDirectory(mods);
        }
    }
}
