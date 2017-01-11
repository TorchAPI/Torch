using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace Torch.API.ModAPI.Ingame
{
    public static class GridExtensions
    {

    }

    public static class PistonExtensions
    {
        public static IMyCubeGrid GetConnectedGrid(this IMyPistonBase pistonBase)
        {
            if (!pistonBase.IsAttached)
                return null;

            return ((Sandbox.ModAPI.IMyPistonBase)pistonBase).TopGrid;
        }
    }

    public static class RotorExtensions
    {
        public static IMyCubeGrid GetConnectedGrid(this IMyMotorStator rotorBase)
        {
            if (!rotorBase.IsAttached)
                return null;

            return ((Sandbox.ModAPI.IMyMotorStator)rotorBase).RotorGrid;
        }
    }
}
