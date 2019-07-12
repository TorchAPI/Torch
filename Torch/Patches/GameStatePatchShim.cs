using System;
using System.Reflection;
using Sandbox;
using Torch.API;
using Torch.Managers.PatchManager;
using Torch.Utils;

namespace Torch.Patches
{
    [PatchShim]
    internal static class GameStatePatchShim
    {
#pragma warning disable 649
        [ReflectedMethodInfo(typeof(MySandboxGame), nameof(MySandboxGame.Dispose))]
        private static MethodInfo _sandboxGameDispose;
        [ReflectedMethodInfo(typeof(MySandboxGame), "Initialize")]
        private static MethodInfo _sandboxGameInit;
#pragma warning restore 649

        internal static void Patch(PatchContext target)
        {
            ConstructorInfo ctor = typeof(MySandboxGame).GetConstructor(new[] { typeof(string[]) });
            if (ctor == null)
                throw new ArgumentException("Can't find constructor MySandboxGame(string[])");
            target.GetPattern(ctor).Prefixes.Add(MethodRef(PrefixConstructor));
            target.GetPattern(ctor).Suffixes.Add(MethodRef(SuffixConstructor));
            target.GetPattern(_sandboxGameInit).Prefixes.Add(MethodRef(PrefixInit));
            target.GetPattern(_sandboxGameInit).Suffixes.Add(MethodRef(SuffixInit));
            target.GetPattern(_sandboxGameDispose).Prefixes.Add(MethodRef(PrefixDispose));
            target.GetPattern(_sandboxGameDispose).Suffixes.Add(MethodRef(SuffixDispose));
        }

        private static MethodInfo MethodRef(Action a )
        {
            return a.Method;
        }

        private static void PrefixConstructor()
        {
            if (TorchBase.Instance is TorchBase tb)
                tb.GameState = TorchGameState.Creating;
        }

        private static void SuffixConstructor()
        {
            PatchManager.CommitInternal();
            if (TorchBase.Instance is TorchBase tb)
                tb.GameState = TorchGameState.Created;
        }

        private static void PrefixInit()
        {
            if (TorchBase.Instance is TorchBase tb)
                tb.GameState = TorchGameState.Loading;
        }

        private static void SuffixInit()
        {
            if (TorchBase.Instance is TorchBase tb)
                tb.GameState = TorchGameState.Loaded;
        }

        private static void PrefixDispose()
        {
            if (TorchBase.Instance is TorchBase tb)
                tb.GameState = TorchGameState.Unloading;
        }

        private static void SuffixDispose()
        {
            if (TorchBase.Instance is TorchBase tb)
                tb.GameState = TorchGameState.Unloaded;
        }
    }
}
