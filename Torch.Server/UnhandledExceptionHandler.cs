using System;
using System.Diagnostics;
using System.Threading;
using NLog;
using VRage;

namespace Torch.Server;

internal class UnhandledExceptionHandler
{
    private readonly TorchConfig _config;
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

    public UnhandledExceptionHandler(TorchConfig config)
    {
        _config = config;
    }
    
    internal void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (Debugger.IsAttached)
            return;
        var ex = (Exception)e.ExceptionObject;
        Log.Fatal(ex.ToStringDemystified());
        LogManager.Flush();
        
        if (ApplicationContext.Current.IsService)
            Environment.Exit(1);
        
        if (_config.RestartOnCrash)
        {
            Console.WriteLine("Restarting in 5 seconds.");
            Thread.Sleep(5000);
            var exe = typeof(Program).Assembly.Location;
#if NETFRAMEWORK
            _config.WaitForPID = Process.GetCurrentProcess().Id.ToString();
#else
            _config.WaitForPID = Environment.ProcessId.ToString();
#endif
            Process.Start(exe, _config.ToString());
        }
        else
        {
            MyVRage.Platform.Windows.MessageBox(
                "Torch encountered a fatal error and needs to close. Please check the logs for details.",
                "Fatal exception", MessageBoxOptions.OkOnly);
        }

        Environment.Exit(1);
    } 
}