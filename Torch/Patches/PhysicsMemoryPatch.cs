using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Havok;
using Sandbox;
using Sandbox.Engine.Physics;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;

namespace Torch.Patches
{
    [PatchShim]
    public static class PhysicsMemoryPatch
    {
        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(typeof(MyPhysics).GetMethod("StepWorldsInternal", BindingFlags.NonPublic | BindingFlags.Instance)).Prefixes.Add(typeof(PhysicsMemoryPatch).GetMethod(nameof(PrefixPhysics)));
        }

        public static bool NotifiedFailure { get; private set; }

        public static bool PrefixPhysics()
        {
            if (!HkBaseSystem.IsOutOfMemory)
                return true;

            if (NotifiedFailure)
                return false;

            NotifiedFailure = true;
            ModCommunication.SendMessageToClients(new NotificationMessage("Havok has run out of memory. Server will restart in 30 seconds!", 60000, MyFontEnum.Red));
            //save the session NOW before anything moves due to weird physics.
            MySession.Static.Save();
            //pause the game, for funsies
            MySandboxGame.IsPaused = true;
            
            //nasty hack
            Task.Run(() =>
                     {
                         Thread.Sleep(TimeSpan.FromSeconds(30));
                         TorchBase.Instance.Restart();
                     });

            return false;
        }
    }
}
