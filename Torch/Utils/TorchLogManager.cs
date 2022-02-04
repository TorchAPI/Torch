using System;
using System.IO;
using System.Runtime.Loader;
using NLog;
using NLog.Config;

namespace Torch.Utils;

public static class TorchLogManager
{
    private static AssemblyLoadContext LoadContext;

    public static LoggingConfiguration Configuration { get; private set; }

    public static void SetConfiguration(LoggingConfiguration configuration, string extensionsDir = null)
    {
        Configuration = configuration;
        LogManager.Setup()
            .SetupExtensions(builder =>
            {
                if (extensionsDir is null || !Directory.Exists(extensionsDir))
                    return;
                if (LoadContext is null)
                {
                    LoadContext = new("TorchLog");
                    foreach (var file in Directory.EnumerateFiles(extensionsDir, "*.dll", SearchOption.AllDirectories))
                    {
                        builder.RegisterAssembly(LoadContext.LoadFromAssemblyPath(file));
                    }
                    return;
                }
                foreach (var assembly in LoadContext.Assemblies)
                {
                    builder.RegisterAssembly(assembly);
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