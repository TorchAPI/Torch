using System;
using System.Reflection;
using Sandbox;
using Torch.Managers.PatchManager;
using Torch.Utils;

namespace Torch.Patches
{
    [PatchShim]
    public static class KeenAutoUpdatePatch
    {
        [ReflectedMethodInfo(typeof(MySandboxGame), "CheckAutoUpdateForDedicatedServer")]
        private static readonly MethodInfo _checkAutoUpdateMethod;
        
        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(_checkAutoUpdateMethod).Prefixes.Add(new Func<bool>(Prefix).Method);
        }

        private static bool Prefix() => false;
    }
}