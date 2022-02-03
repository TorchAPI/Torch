using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using NLog;
using Torch.API;
using Torch.Views;

namespace Torch.Server
{
    // TODO: redesign this gerbage
    public class TorchConfig : CommandLine, ITorchConfig, INotifyPropertyChanged
    {
        private static Logger _log = LogManager.GetLogger("Config");

        public bool ShouldUpdatePlugins => (GetPluginUpdates && !NoUpdate) || ForceUpdate;
        public bool ShouldUpdateTorch => (GetTorchUpdates && !NoUpdate) || ForceUpdate;
        
        private bool _autostart;
        private bool _restartOnCrash;
        private bool _noGui;
        private bool _getPluginUpdates = true;
        private bool _getTorchUpdates = true;
        private int _tickTimeout = 60;
        private bool _localPlugins;
        private bool _disconnectOnRestart;
        private string _chatName = "Server";
        private string _chatColor = "Red";
        private bool _enableWhitelist = false;
        private List<ulong> _whitelist = new List<ulong>();
        private int _windowWidth = 980;
        private int _windowHeight = 588;
        private bool _independentConsole = false;
        private bool _enableAsserts = false;
        private int _fontSize = 16;
        private UGCServiceType _ugcServiceType = UGCServiceType.Steam;
        private bool _entityManagerEnabled = true;

        /// <inheritdoc />
        [XmlIgnore, Arg("noupdate", "Disable automatically downloading game and plugin updates.")]
        public bool NoUpdate { get; set; }

        /// <inheritdoc />
        [XmlIgnore, Arg("forceupdate", "Manually check for and install updates.")]
        public bool ForceUpdate { get; set; }

        /// <summary>
        /// Permanent flag to ALWAYS automatically start the server
        /// </summary>
        [Display(Name = "Auto Start", Description = "Permanent flag to ALWAYS automatically start the server.", GroupName = "Server")]
        public bool Autostart { get => _autostart; set => Set(value, ref _autostart); }

        /// <summary>
        /// Temporary flag to automatically start the server only on the next run
        /// </summary>
        [Arg("autostart", "Start the server immediately.")]
        [XmlIgnore]
        public bool TempAutostart { get; set; }

        /// <inheritdoc />
        [Arg("restartoncrash", "Automatically restart the server if it crashes.")]
        [Display(Name = "Restart On Crash", Description = "Automatically restart the server if it crashes.", GroupName = "Server")]
        public bool RestartOnCrash { get => _restartOnCrash; set => Set(value, ref _restartOnCrash); }

        public string InstancePath { get; set; }

        /// <inheritdoc />
        [Arg("nogui", "Do not show the Torch UI.")]
        [Display(Name = "No GUI", Description = "Do not show the Torch UI.", GroupName = "Window")]
        public bool NoGui { get => _noGui; set => Set(value, ref _noGui); }

        /// <inheritdoc />
        [XmlIgnore, Arg("waitforpid", "Makes Torch wait for another process to exit.")]
        public string WaitForPID { get; set; }

        /// <inheritdoc />
        [Display(Name = "Update Torch", Description = "Check every start for new versions of torch.", GroupName = "Server")]
        public bool GetTorchUpdates { get => _getTorchUpdates; set => Set(value, ref _getTorchUpdates); }

        public string InstanceName { get; set; }

        /// <inheritdoc />
        [Display(Name = "Update Plugins", Description = "Check every start for new versions of plugins.", GroupName = "Server")]
        public bool GetPluginUpdates { get => _getPluginUpdates; set => Set(value, ref _getPluginUpdates); }

        /// <inheritdoc />
        [Display(Name = "Watchdog Timeout", Description = "Watchdog timeout (in seconds).", GroupName = "Server")]
        public int TickTimeout { get => _tickTimeout; set => Set(value, ref _tickTimeout); }

        /// <inheritdoc />
        [Arg("plugins", "Starts Torch with the given plugin GUIDs (space delimited).")]
        public List<Guid> Plugins { get; set; } = new List<Guid>();

        [Arg("localplugins", "Loads all pluhins from disk, ignores the plugins defined in config.")]
        [Display(Name = "Local Plugins", Description = "Loads all pluhins from disk, ignores the plugins defined in config.", GroupName = "In-Game")]
        public bool LocalPlugins { get => _localPlugins; set => Set(value, ref _localPlugins); }

        [Arg("disconnect", "When server restarts, all clients are rejected to main menu to prevent auto rejoin.")]
        [Display(Name = "Auto Disconnect", Description = "When server restarts, all clients are rejected to main menu to prevent auto rejoin.", GroupName = "In-Game")]
        public bool DisconnectOnRestart { get => _disconnectOnRestart; set => Set(value, ref _disconnectOnRestart); }

        [Display(Name = "Chat Name", Description = "Default name for chat from gui, broadcasts etc..", GroupName = "In-Game")]
        public string ChatName { get => _chatName; set => Set(value, ref _chatName); }

        [Display(Name = "Chat Color", Description = "Default color for chat from gui, broadcasts etc.. (Red, Blue, White, Green)", GroupName = "In-Game")]
        public string ChatColor { get => _chatColor; set => Set(value, ref _chatColor); }

        [Display(Name = "Enable Whitelist", Description = "Enable Whitelist to prevent random players join while maintance, tests or other.", GroupName = "In-Game")]
        public bool EnableWhitelist { get => _enableWhitelist; set => Set(value, ref _enableWhitelist); }

        [Display(Name = "Whitelist", Description = "Collection of whitelisted steam ids.", GroupName = "In-Game")]
        public List<ulong> Whitelist { get => _whitelist; set => Set(value, ref _whitelist); }

        [Display(Name = "Width", Description = "Default window width.", GroupName = "Window")]
        public int WindowWidth { get => _windowWidth; set => Set(value, ref _windowWidth); }

        [Display(Name = "Height", Description = "Default window height", GroupName = "Window")]
        public int WindowHeight { get => _windowHeight; set => Set(value, ref _windowHeight); }

        [Display(Name = "Font Size", Description = "Font size for logging text box. (default is 16)", GroupName = "Window")]
        public int FontSize { get => _fontSize; set => Set(value, ref _fontSize); }

        [Display(Name = "UGC Service Type", Description = "Service for downloading mods", GroupName = "Server")]
        public UGCServiceType UgcServiceType
        {
            get => _ugcServiceType;
            set => Set(value, ref _ugcServiceType);
        }

        public string LastUsedTheme { get; set; } = "Torch Theme";

        //Prevent reserved players being written to disk, but allow it to be read
        //remove this when ReservedPlayers is removed
        private bool ShouldSerializeReservedPlayers() => false;

        [Arg("console", "Keeps a separate console window open after the main UI loads.")]
        [Display(Name = "Independent Console", Description = "Keeps a separate console window open after the main UI loads.", GroupName = "Window")]
        public bool IndependentConsole { get => _independentConsole; set => Set(value, ref _independentConsole); }

        [XmlIgnore]
        [Arg("testplugin", "Path to a plugin to debug. For development use only.")]
        public string TestPlugin { get; set; }

        [Arg("asserts", "Enable Keen's assert logging.")]
        [Display(Name = "Enable Asserts", Description = "Enable Keen's assert logging.", GroupName = "Server")]
        public bool EnableAsserts { get => _enableAsserts; set => Set(value, ref _enableAsserts); }

        [Display(Name = "Enable Entity Manager", Description = "Enable Entity Manager tab. (can affect performance)",
            GroupName = "Server")]
        public bool EntityManagerEnabled
        {
            get => _entityManagerEnabled;
            set => Set(value, ref _entityManagerEnabled);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public TorchConfig() { }

        protected void Set<T>(T value, ref T field, [CallerMemberName] string callerName = default)
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(callerName));
        }

        // for backward compatibility
        public void Save(string path = null) => Initializer.Instance?.ConfigPersistent?.Save(path);
    }
}
