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
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRageMath;

namespace Torch.Managers
{
    public class EntityManager
    {
        private readonly ITorchBase _torch;
        private static readonly Logger Log = LogManager.GetLogger(nameof(EntityManager));

        public EntityManager(ITorchBase torch)
        {
            _torch = torch;
            _torch.SessionLoaded += () => InitConcealment(60000);
        }

        public void ExportGrid(IMyCubeGrid grid, string path)
        {
            var ob = grid.GetObjectBuilder(true);
            using (var f = File.Open(path, FileMode.CreateNew))
                MyObjectBuilderSerializer.SerializeXML(f, ob);
        }

        public void ImportGrid(string path, Vector3D position)
        {
            MyObjectBuilder_EntityBase gridOb;
            using (var f = File.OpenRead(path))
                MyObjectBuilderSerializer.DeserializeXML(f, out gridOb);

            var grid = MyEntities.CreateFromObjectBuilderParallel(gridOb);
            grid.PositionComp.SetPosition(position);
            MyEntities.Add(grid);
        }

        #region Concealment

        private readonly List<ConcealGroup> _concealGroups = new List<ConcealGroup>();
        private MyDynamicAABBTreeD _concealedAabbTree;

        public void GetConcealedGrids(List<IMyCubeGrid> grids)
        {
                _concealGroups.SelectMany(x => x.Grids).ForEach(grids.Add);
        }

        private Timer _concealTimer;
        private Timer _revealTimer;
        private volatile bool _concealInProgress;

        public void InitConcealment(double concealInterval)
        {
            Log.Info($"Initializing concealment to run every {concealInterval}ms");
            _concealedAabbTree = new MyDynamicAABBTreeD(MyConstants.GAME_PRUNING_STRUCTURE_AABB_EXTENSION);
            _concealTimer = new Timer(concealInterval);
            _concealTimer.Elapsed += ConcealTimerElapsed;
            _concealTimer.Start();
            _revealTimer = new Timer(1000);
            _revealTimer.Elapsed += RevealTimerElapsed;
            _revealTimer.Start();
            MySession.Static.Players.PlayerRequesting += RevealSpawns;
            MyMultiplayer.Static.ClientJoined += RevealCryoPod;
        }

        private void RevealTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _torch.Invoke(() => RevealNearbyGrids(MyMultiplayer.Static.ViewDistance));
        }

        private void RevealCryoPod(ulong steamId)
        {
            _torch.Invoke(() =>
            {
                Log.Debug(nameof(RevealCryoPod));
                for (var i = _concealGroups.Count - 1; i >= 0; i--)
                {
                    var group = _concealGroups[i];

                    if (group.IsCryoOccupied(steamId))
                    {
                        RevealGroup(group);
                        return;
                    }
                }
            });
        }

        private void RevealSpawns(PlayerRequestArgs args)
        {
            _torch.Invoke(() =>
            {
                Log.Debug(nameof(RevealSpawns));
                var identityId = MySession.Static.Players.TryGetIdentityId(args.PlayerId.SteamId);
                if (identityId == 0)
                    return;

                for (var i = _concealGroups.Count - 1; i >= 0; i--)
                {
                    var group = _concealGroups[i];

                    if (group.IsMedicalRoomAvailable(identityId))
                        RevealGroup(group);
                }
            });
        }

        private void ConcealTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_concealInProgress)
            {
                Log.Warn($"Concealment taking longer to complete than the conceal interval of {_concealTimer.Interval}ms");
                return;
            }

            _concealInProgress = true;
            Log.Debug("Running concealment");
            _torch.Invoke(() =>
            {
                if (MyAPIGateway.Session == null)
                    return;

                var viewDistance = MyMultiplayer.Static.ViewDistance;
                var concealDistance = viewDistance > 50000 ? viewDistance : 50000;

                ConcealDistantGrids(concealDistance * 2);
                _concealInProgress = false;
            });
        }

        private void ConcealEntity(IMyEntity entity)
        {
            if (entity != entity.GetTopMostParent())
                throw new InvalidOperationException("Can only conceal top-level entities.");

            MyGamePruningStructure.Remove((MyEntity)entity);
            entity.Physics?.Deactivate();
            UnregisterRecursive(entity);

            void UnregisterRecursive(IMyEntity e)
            {
                MyEntities.UnregisterForUpdate((MyEntity)e);
                foreach (var child in e.Hierarchy.Children)
                    UnregisterRecursive(child.Entity);
            }
        }

        private void RevealEntity(IMyEntity entity)
        {
            if (entity != entity.GetTopMostParent())
                throw new InvalidOperationException("Can only conceal top-level entities.");

            MyGamePruningStructure.Add((MyEntity)entity);
            entity.Physics?.Activate();
            RegisterRecursive(entity);

            void RegisterRecursive(IMyEntity e)
            {
                MyEntities.RegisterForUpdate((MyEntity)e);
                foreach (var child in e.Hierarchy.Children)
                    RegisterRecursive(child.Entity);
            }
        }

        private bool ConcealGroup(ConcealGroup group)
        {
            if (_concealGroups.Any(g => g.Id == group.Id))
                return false;
            Log.Info($"Concealing grids: {string.Join(", ", group.Grids.Select(g => g.DisplayName))}");
            group.ConcealTime = DateTime.Now;
            group.Grids.ForEach(ConcealEntity);
            Task.Run(() =>
            {
                group.UpdatePostConceal();
                var aabb = group.WorldAABB;
                group.ProxyId = _concealedAabbTree.AddProxy(ref aabb, group, 0);
                Log.Debug($"Group {group.Id} cached");
                _torch.Invoke(() => _concealGroups.Add(group));
            });
            return true;
        }

        private void RevealGroup(ConcealGroup group)
        {
            Log.Info($"Revealing grids: {string.Join(", ", group.Grids.Select(g => g.DisplayName))}");
            group.Grids.ForEach(RevealEntity);
            _concealGroups.Remove(group);
            _concealedAabbTree.RemoveProxy(group.ProxyId);
        }

        private readonly List<ConcealGroup> _intersectGroups = new List<ConcealGroup>();
        public void RevealGridsInSphere(BoundingSphereD sphere)
        {
            _concealedAabbTree.OverlapAllBoundingSphere(ref sphere, _intersectGroups);
            foreach (var group in _intersectGroups)
            {
                RevealGroup(group);
            }
            _intersectGroups.Clear();
        }

        public void RevealNearbyGrids(double distanceFromPlayers)
        {
            var playerSpheres = GetPlayerBoundingSpheres(distanceFromPlayers);
            foreach (var sphere in playerSpheres)
            {
                RevealGridsInSphere(sphere);
            }
        }

        public void ConcealDistantGrids(double distanceFromPlayers)
        {
            var playerSpheres = GetPlayerBoundingSpheres(distanceFromPlayers);

            foreach (var group in MyCubeGridGroups.Static.Physical.Groups)
            {
                var volume = group.GetWorldAABB();
                if (playerSpheres.Any(s => s.Contains(volume) != ContainmentType.Disjoint))
                    continue;

                ConcealGroup(new ConcealGroup(group));
            }
        }

        private List<BoundingSphereD> GetPlayerBoundingSpheres(double distance)
        {
            return ((MyPlayerCollection)MyAPIGateway.Multiplayer.Players).GetOnlinePlayers()
                .Where(p => p.Controller?.ControlledEntity != null)
                .Select(p => new BoundingSphereD(p.Controller.ControlledEntity.Entity.PositionComp.GetPosition(), distance)).ToList();
        }
    #endregion Concealment
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

    internal class ConcealGroup
    {
        /// <summary>
        /// Entity ID of the first grid in the group.
        /// </summary>
        public long Id { get; }
        public DateTime ConcealTime { get; set; }
        public BoundingBoxD WorldAABB { get; private set; }
        public List<MyCubeGrid> Grids { get; }
        public List<MyMedicalRoom> MedicalRooms { get; } = new List<MyMedicalRoom>();
        public List<MyCryoChamber> CryoChambers { get; } = new List<MyCryoChamber>();
        internal volatile int ProxyId = -1;

        public ConcealGroup(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group)
        {
            Grids = group.Nodes.Select(n => n.NodeData).ToList();
            Id = Grids.First().EntityId;
        }

        public void UpdatePostConceal()
        {
            UpdateAABB();
            CacheSpawns();
        }

        private void UpdateAABB()
        {
            var startPos = Grids.First().PositionComp.GetPosition();
            var box = new BoundingBoxD(startPos, startPos);

            foreach (var aabb in Grids.Select(g => g.PositionComp.WorldAABB))
                box.Include(aabb);

            WorldAABB = box;
        }

        private void CacheSpawns()
        {
            MedicalRooms.Clear();
            CryoChambers.Clear();

            foreach (var block in Grids.SelectMany(x => x.GetFatBlocks()))
            {
                if (block is MyMedicalRoom medical)
                    MedicalRooms.Add(medical);
                else if (block is MyCryoChamber cryo)
                    CryoChambers.Add(cryo);
            }
        }

        public bool IsMedicalRoomAvailable(long identityId)
        {
            foreach (var room in MedicalRooms)
            {
                if (room.HasPlayerAccess(identityId) && room.IsWorking)
                    return true;
            }

            return false;
        }

        public bool IsCryoOccupied(ulong steamId)
        {
            var currentIdField = typeof(MyCryoChamber).GetField("m_currentPlayerId", BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var chamber in CryoChambers)
            {
                var value = (MyPlayer.PlayerId?)currentIdField.GetValue(chamber);
                if (value?.SteamId == steamId)
                    return true;
            }

            return false;
        }
    }
}
