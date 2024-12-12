using System.Linq;
using NLog;
using Sandbox.Game.Entities;
using Torch.API;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRage.ObjectBuilders;
using VRage.ObjectBuilders.Private;
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
