using System;
using System.Collections.Generic;
using Sandbox.Game.World;
using VRage.Game.ModAPI;

namespace Torch.Commands.Permissions
{
    public class PermissionGroup
    {
        public List<ulong> Members { get; }
        public List<Permission> Permissions { get; }

        public void AddMember(PermissionUser user)
        {
            Members.Add(user.SteamID);
            user.Groups.Add(this);
        }

        public void RemoveMember(PermissionUser user)
        {
            user.Groups.Remove(this);
            Members.Remove(user.SteamID);
        }
    }

    public class PermissionUser
    {
        public ulong SteamID { get; }
        public List<PermissionGroup> Groups { get; }
        public List<Permission> Permissions { get; }

        public void AddToGroup(PermissionGroup group)
        {
            group.Members.Add(SteamID);
            Groups.Add(group);
        }

        public void RemoveFromGroup(PermissionGroup group)
        {
            group.Members.Remove(SteamID);
            Groups.Remove(group);
        }

        public void HasPermission()
        {
            var promoteLevel = MySession.Static.GetUserPromoteLevel(SteamID);
        }
    }

    public class Permission
    {
        public string[] Path { get; }
        public bool Allow { get; }
        public Dictionary<string, Permission> Children { get; } = new Dictionary<string, Permission>();
    }

    public class PermissonsSystem
    {
        public Dictionary<string, PermissionGroup> Groups { get; } = new Dictionary<string, PermissionGroup>();
        public Dictionary<long, PermissionUser> Users { get; } = new Dictionary<long, PermissionUser>();

        public void GenerateDefaultGroups()
        {
            foreach (var name in Enum.GetNames(typeof(MyPromoteLevel)))
            {
                 Groups.Add(name, new PermissionGroup());
            }
        }
    }
}
