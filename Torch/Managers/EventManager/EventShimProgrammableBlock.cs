using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;
using Torch.Utils;

namespace Torch.Managers.EventManager
{
    internal static class EventShimProgrammableBlock
    {
        [ReflectedMethodInfo(typeof(MyProgrammableBlock), nameof(MyProgrammableBlock.ExecuteCode))]
        private static MethodInfo _programmableBlockExecuteCode;
        private static readonly EventList<ProgrammableBlockTryRunEvent> _tryRunEventList = new EventList<ProgrammableBlockTryRunEvent>();
        private static readonly EventList<ProgrammableBlockWasRunEvent> _wasRunEventList = new EventList<ProgrammableBlockWasRunEvent>();

        private static void Patch(PatchContext context)
        {
            var p = context.GetPattern(_programmableBlockExecuteCode);
            p.Prefixes.Add(
                typeof(EventShimProgrammableBlock).GetMethod(nameof(PrefixExecuteCode), BindingFlags.NonPublic));
            p.Suffixes.Add(
                typeof(EventShimProgrammableBlock).GetMethod(nameof(SuffixExecuteCode), BindingFlags.NonPublic));
        }

        private static bool PrefixExecuteCode(MyProgrammableBlock __instance, string argument)
        {
            var evt = new ProgrammableBlockTryRunEvent(__instance, argument);
            _tryRunEventList.RaiseEvent(ref evt);
            return !evt.Cancelled;
        }

        private static void SuffixExecuteCode(MyProgrammableBlock __instance, string argument, string response)
        {
            var evt = new ProgrammableBlockWasRunEvent(__instance, argument, response);
            _wasRunEventList.RaiseEvent(ref evt);
        }
    }

    public interface IBlockEvent : IEvent
    {
        MyCubeBlock Block { get; }
    }

    public interface IProgrammableBlockEvent : IBlockEvent
    {
        new MyProgrammableBlock Block { get; }
    }


    public struct ProgrammableBlockTryRunEvent : IProgrammableBlockEvent
    {
        internal ProgrammableBlockTryRunEvent(MyProgrammableBlock block, string arg)
        {
            Block = block;
            Argument = arg;
            Cancelled = false;
        }
        
        public bool Cancelled { get; set; }
        public string Argument { get; }
        public MyProgrammableBlock Block { get; }
        MyCubeBlock IBlockEvent.Block => Block;
    }

    public struct ProgrammableBlockWasRunEvent : IProgrammableBlockEvent
    {
        internal ProgrammableBlockWasRunEvent(MyProgrammableBlock block, string arg, string response)
        {
            Block = block;
            Argument = arg;
            Response = response;
        }

        public bool Cancelled => false;
        public string Argument { get; }
        public string Response { get; }
        public MyProgrammableBlock Block { get; }
        MyCubeBlock IBlockEvent.Block => Block;
    }
}
