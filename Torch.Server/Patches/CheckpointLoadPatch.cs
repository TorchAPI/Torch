using System.Reflection;
using NLog;
using Sandbox.Engine.Networking;
using Torch.API.Managers;
using Torch.Managers.PatchManager;
using Torch.Server.Managers;
using Torch.Utils;
using VRage.Game;

namespace Torch.Patches;

[PatchShim]
public static class CheckpointLoadPatch
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();
    
    [ReflectedMethodInfo(typeof(MyLocalCache), "LoadCheckpoint")]
    private static MethodInfo LoadCheckpointMethod = null!;
    
    public static void Patch(PatchContext context)
    {
        context.GetPattern(LoadCheckpointMethod).AddPrefix();
    }

    private static bool Prefix(ref MyObjectBuilder_Checkpoint __result)
    {
#pragma warning disable CS0618
        var world = TorchBase.Instance.Managers.GetManager<InstanceManager>().DedicatedConfig.SelectedWorld;
#pragma warning restore CS0618
        if (world is null)
        {
            Log.Error("Selected world is null");
            return false;
        }

        world.KeenCheckpoint.Settings = world.WorldConfiguration.Settings;
        world.KeenCheckpoint.Mods = world.WorldConfiguration.Mods;

        __result = world.Checkpoint;
        return false;
    }
}