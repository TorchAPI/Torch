using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NLog;
using Sandbox.Definitions;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using Torch.Utils;

namespace Torch.Patches;

[PatchShim]
internal static class LoaderHook
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
    private static Stopwatch _stopwatch;

    [ReflectedMethodInfo(typeof(MyScriptManager), nameof(MyScriptManager.LoadData))]
    private static MethodInfo _compilerLoadData = null!;

    public static void Patch(PatchContext context)
    {
        var pattern = context.GetPattern(_compilerLoadData);
        pattern.AddPrefix(nameof(CompilePrefix));
        pattern.AddSuffix(nameof(CompileSuffix));

        var methods = typeof(MyDefinitionManager)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
        
        pattern = context.GetPattern(methods.First(b =>
            b.Name == "LoadDefinitions" && b.GetParameters()[0].ParameterType.Name.Contains("List")));
        pattern.AddPrefix(nameof(LoadDefinitionsPrefix));
        pattern.AddSuffix(nameof(LoadDefinitionsSuffix));
    }
    
    private static void CompilePrefix()
    {
        _stopwatch?.Reset();
        _stopwatch = Stopwatch.StartNew();
        Log.Info("Mod scripts compilation started");
    }

    private static void CompileSuffix()
    {
        _stopwatch.Stop();
        Log.Info($"Compilation finished. Took {_stopwatch.Elapsed:g}");
        _stopwatch = null;
    }

    private static void LoadDefinitionsPrefix(MyDefinitionManager __instance)
    {
        if (!__instance.Loading) 
            return;
        _stopwatch?.Reset();
        _stopwatch = Stopwatch.StartNew();
        Log.Info("Definitions loading started");
    }

    private static void LoadDefinitionsSuffix(MyDefinitionManager __instance)
    {
        if (!__instance.Loading) 
            return;
        _stopwatch.Stop();
        Log.Info($"Definitions load finished. Took {_stopwatch.Elapsed:g}");
        _stopwatch = null;
    }
}