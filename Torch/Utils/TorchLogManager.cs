using System;
using System.IO;
using System.Linq;
using System.Reflection;
#if !NETFRAMEWORK
using System.Runtime.Loader;
#endif
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Torch.Utils;

public static class TorchLogManager
{
#if !NETFRAMEWORK
    private static AssemblyLoadContext LoadContext = new("TorchLog");
#endif

    public static LoggingConfiguration Configuration { get; private set; }

    public static void SetConfiguration(LoggingConfiguration configuration)
    {
        Configuration = configuration;
        LogManager.Configuration = configuration;
        LogManager.ReconfigExistingLoggers();
    }

    public static void RegisterTargets(string dir)
    {
        if (!Directory.Exists(dir)) return;
        
        foreach (var type in Directory.EnumerateFiles(dir, "*.dll")
#if NETFRAMEWORK
                     .Select(Assembly.LoadFile)
#else
                     .Select(LoadContext.LoadFromAssemblyPath)
#endif
                     .SelectMany(b => b.ExportedTypes)
                     .Where(b => b.GetCustomAttribute<TargetAttribute>() is { }))
        {
            Target.Register(type.GetCustomAttribute<TargetAttribute>()!.Name, type);
        }
    }

    public static void RestoreGlobalConfiguration()
    {
        SetConfiguration(Configuration);
    }
}