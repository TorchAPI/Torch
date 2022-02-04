using System;
using System.IO;
using System.Runtime.Loader;
using NLog;
using NLog.Config;

namespace Torch.Utils;

public static class TorchLogManager
{
    private static readonly AssemblyLoadContext LoadContext = new("TorchLog");

    public static LoggingConfiguration Configuration { get; private set; }

    public static void SetConfiguration(LoggingConfiguration configuration, string extensionsDir = null)
    {
        Configuration = configuration;
        LogManager.Setup()
            .SetupExtensions(builder =>
            {
                if (extensionsDir is null || !Directory.Exists(extensionsDir))
                    return;
                foreach (var file in Directory.EnumerateFiles(extensionsDir, "*.dll", SearchOption.AllDirectories))
                {
                    builder.RegisterAssembly(LoadContext.LoadFromAssemblyPath(file));
                }
            })
            .SetupLogFactory(builder => builder.SetThrowConfigExceptions(true))
            .LoadConfiguration(configuration);
        LogManager.ReconfigExistingLoggers();
    }

    public static void RestoreGlobalConfiguration()
    {
        LogManager.Configuration = Configuration;
        LogManager.ReconfigExistingLoggers();
    }
}