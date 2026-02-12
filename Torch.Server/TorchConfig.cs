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

        private string _instancePath = Path.GetFullPath("Instance");
        private string _instanceName = "Instance";
        private bool _autostart;
        private bool _restartOnCrash;
        private bool _restartOnGameUpdate;
        private int _gameUpdateRestartDelayMins = 5;
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
        private bool _saveWindowChanges = true;
        private bool _startMinimized = false;
        private bool _minimizeOnServerStart = false;
        private int _windowWidth;
        private int _windowHeight;
        private int _windowX;
        private int _windowY;
        private bool _independentConsole = false;
        private bool _enableAsserts = false;
        private int _fontSize = 16;
        private UGCServiceType _ugcServiceType = UGCServiceType.Steam;
        private TorchBranchType _torchBranch = TorchBranchType.master;
        private bool _sendLogsToKeen;
        private bool _deleteMiniDumps = true;
        private string _loginToken;
        private bool bypassIsReloadableFlag;


        /// <inheritdoc />
        [Arg("instancename", "The name of the Torch instance.")]
        [Display(Name = "Instance Name", Description = "The name of the Torch instance.", GroupName = "Server")]
        public string InstanceName { get => _instanceName; set => Set(value, ref _instanceName); }

        /// <inheritdoc />
        [Arg("instancepath", "Server data folder where saves and mods are stored.")]
        [Display(Name = "Instance Path", Description = "Server data folder where saves and mods are stored.", GroupName = "Server")]
        public string InstancePath
        {
            get => _instancePath;
            set => Set(value, ref _instancePath);
        }

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
        
        /// <summary>
        /// Enable Game update detection. If enabled, server will restart when game updates are found.
        /// </summary>
        [Arg("gameupdatedetection", "Automatically restart the server if the game updates.")]
        [Display(Name = "Restart On Game Update", Description = "Automatically restart the server if the game updates.", GroupName = "Update Detection")]
        public bool RestartOnGameUpdate { get => _restartOnGameUpdate; set => Set(value, ref _restartOnGameUpdate); }

        /// <summary>
        /// How long (in minutes) to wait before restarting after a game update is detected.
        /// </summary>
        [Arg("gameupdaterestartdelay", "How long (in minutes) to wait before restarting after a game update is detected.")]
        [Display(Name = "Game Update Restart Delay", Description = "How long (in minutes) to wait before restarting after a game update is detected.", GroupName = "Update Detection")]
        public int GameUpdateRestartDelayMins { get => _gameUpdateRestartDelayMins; set => Set(value, ref _gameUpdateRestartDelayMins); }

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

        /// <inheritdoc />
        [Display(Name = "Update Plugins", Description = "Check every start for new versions of plugins.", GroupName = "Server")]
        public bool GetPluginUpdates { get => _getPluginUpdates; set => Set(value, ref _getPluginUpdates); }
        
        /// <inheritdoc />
        [Display(Name = "Bypass reloadable flag", Description = "Bypass the reloadable flag on plugins (forces true).", GroupName = "Server")]
        public bool BypassIsReloadableFlag { get => bypassIsReloadableFlag; set => Set(value, ref bypassIsReloadableFlag); }

        /// <inheritdoc />
        [Display(Name = "Watchdog Timeout", Description = "Watchdog timeout (in seconds).", GroupName = "Server")]
        public int TickTimeout { get => _tickTimeout; set => Set(value, ref _tickTimeout); }

        /// <inheritdoc />
        [Arg("plugins", "Starts Torch with the given plugin GUIDs (space delimited).")]
        public List<Guid> Plugins { get; set; } = new List<Guid>();

        [Arg("localplugins", "Loads all plugins from disk, ignores the plugins defined in config.")]
        [Display(Name = "Local Plugins", Description = "Loads all plugins from disk, ignores the plugins defined in config.", GroupName = "In-Game")]
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

        [Display(Name = "Save Window Changes", Description = "Save window size and location.", GroupName = "Window")]
        public bool SaveWindowChanges { get => _saveWindowChanges; set => Set(value, ref _saveWindowChanges); }
        
        [Display(Name = "Start Minimized", Description = "Start Torch minimized.", GroupName = "Window")]
        public bool StartMinimized { get => _startMinimized; set => Set(value, ref _startMinimized); }
        
        [Display(Name = "Minimize On Server Start", Description = "Minimize Torch window on server start.", GroupName = "Window")]
        public bool MinimizeOnServerStart { get => _minimizeOnServerStart; set => Set(value, ref _minimizeOnServerStart); }
        
        [Display(Name = "Width", Description = "Default window width.", GroupName = "Window")]
        public int WindowWidth { get => _windowWidth; set => Set(value, ref _windowWidth); }

        [Display(Name = "Height", Description = "Default window height", GroupName = "Window")]
        public int WindowHeight { get => _windowHeight; set => Set(value, ref _windowHeight); }
        
        [Display(Name = "WindowX", Description = "Default window X position", GroupName = "Window")]
        public int WindowX { get => _windowX; set => Set(value, ref _windowX); }
        
        [Display(Name = "WindowY", Description = "Default window Y position", GroupName = "Window")]
        public int WindowY { get => _windowY; set => Set(value, ref _windowY); }

        [Display(Name = "Font Size", Description = "Font size for logging text box. (default is 16)", GroupName = "Window")]
        public int FontSize { get => _fontSize; set => Set(value, ref _fontSize); }

        [Display(Name = "UGC Service Type", Description = "Service for downloading mods", GroupName = "Server")]
        public UGCServiceType UgcServiceType
        {
            get => _ugcServiceType;
            set => Set(value, ref _ugcServiceType);
        }

        [Display(Name = "Torch branch", Description = "Select what branch of torch you want to use.", GroupName = "Server")]
        public TorchBranchType BranchName
        {
            get => _torchBranch;
            set => Set(value, ref _torchBranch);
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
        
        [Arg("sendlogstokeen", "On crash, send debug data and logs to Keen.")]
        [Display(Name = "Send Logs To Keen", Description = "On crash, send debug data and logs to Keen.", GroupName = "Logging")]
        public bool SendLogsToKeen { get => _sendLogsToKeen; set => Set(value, ref _sendLogsToKeen); }
        
        [Arg("delteminidumps", "Delete mini dumps after they are created")]
        [Display(Name = "Delete Mini Dumps", Description = "Delete mini dumps after they are created", GroupName = "Logging")]
        public bool DeleteMiniDumps { get => _deleteMiniDumps; set => Set(value, ref _deleteMiniDumps); }

        [Arg("logintoken", "Steam GSLT")]
        [Display(Name = "Login Token", Description = "Steam GSLT (can be used if you have dynamic ip)", GroupName = "Server")]
        public string LoginToken { get => _loginToken; set => Set(value, ref _loginToken); }

        [Display(Name = "Overwrite global NLog config on update.", Description = "This should ALWAYS be true UNLESS you know what you are doing.  Breaking the default config may cause issues with logging.  Just, no.  Leave this alone.", GroupName = "Logging" )]
        public bool OverwriteGlobalNLogConfigOnUpdate { get; set; } = true;

        [Display(Name = "Force Overwrite Runscript", Description = "Always overwrite the SteamCMD runscript on startup. Disable only if you have a custom runscript.", GroupName = "Server")]
        public bool ForceOverwriteRunscript { get; set; } = true;

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
