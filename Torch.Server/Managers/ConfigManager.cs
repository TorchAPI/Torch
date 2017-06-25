using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Engine.Utils;
using Torch.API;
using Torch.API.Managers;
using Torch.Managers;
using Torch.Server.ViewModels;
using VRage.Game;

namespace Torch.Server.Managers
{
    //TODO
    public class ConfigManager : Manager
    {
        private const string CONFIG_NAME = "SpaceEngineers-Dedicated.cfg";
        public ConfigDedicatedViewModel DedicatedConfig { get; set; }
        public TorchConfig TorchConfig { get; set; }

        public ConfigManager(ITorchBase torchInstance) : base(torchInstance)
        {
            
        }

        /// <inheritdoc />
        public override void Init()
        {
            LoadInstance(Torch.Config.InstancePath);
        }

        public void LoadInstance(string path)
        {
            if (!Directory.Exists(path))
                throw new FileNotFoundException($"Instance directory not found at '{path}'");

            var configPath = Path.Combine(path, CONFIG_NAME);
            var config = new MyConfigDedicated<MyObjectBuilder_SessionSettings>(configPath);
            config.Load();
            DedicatedConfig = new ConfigDedicatedViewModel(config);
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
            var savesPath = Path.Combine(path, "Saves");
            Directory.CreateDirectory(savesPath);
            var modsPath = Path.Combine(path, "Mods");
            Directory.CreateDirectory(modsPath);
            LoadInstance(path);
        }
    }
}
