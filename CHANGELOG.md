# Torch 1.1.205.478
* Notes
    - This release makes significant changes to TorchConfig.xml. It has been renamed to Torch.cfg and has different options.
* Features
    - Plugins, Torch, and the DS can now all update automatically
    - Changed command prefix to !
    - Added manual save command (thanks to Maldark)
    - Added restart command
    - Improved instance creation: now creates an entire skeleton instance with blank config
    - Added instance name to console title
* Fixes
    - Optimized UI so it's snappier and freezes less often
    - Fixed NetworkManager.RaiseEvent overload that had an off-by-one bug
    - Fixed chat window so it automatically scrolls down

# Torch 1.0.182.329
    * Improved logging, logs now to go the Logs folder and aren't deleted on start
    * Fixed chat tab not enabling with -autostart
    * Fixed player list
    * Watchdog time-out is now configurable in TorchConfig.xml
    * Fixed infinario log spam
    * Fixed crash when sending empty message from chat tab
    * Fixed permissions on Torch commands
    * Changed plugin StoragePath to the current instance path (per-instance configs)