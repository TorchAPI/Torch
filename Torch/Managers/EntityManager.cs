using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using NLog;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.ModAPI;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.ObjectBuilders.Private;
using VRage.Sync;
using VRageMath;


namespace Torch.Managers
{
    public class EntityManager : Manager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public EntityManager(ITorchBase torch) : base(torch)
        {

        }

        public void ExportGrid(IMyCubeGrid grid, string path)
        {

            var ob = grid.GetObjectBuilder(true);

            MyObjectBuilderSerializerKeen.SerializeXML(path, false,ob);
        }

        public void ImportGrid(string path, Vector3D position)
        {
            MyObjectBuilder_EntityBase gridOb;

            MyObjectBuilderSerializerKeen.DeserializeXML(path, out gridOb);

            var grid = MyEntities.CreateFromObjectBuilderParallel(gridOb);
            grid.PositionComp.SetPosition(position);
            MyEntities.Add(grid);
        }
    }

    public static class GroupExtensions
    {
        public static BoundingBoxD GetWorldAABB(this MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group)
        {
            var grids = group.Nodes.Select(n => n.NodeData);

            var startPos = grids.First().PositionComp.GetPosition();
            var box = new BoundingBoxD(startPos, startPos);

            foreach (var aabb in grids.Select(g => g.PositionComp.WorldAABB))
                box.Include(aabb);

            return box;
        }
    }
}
