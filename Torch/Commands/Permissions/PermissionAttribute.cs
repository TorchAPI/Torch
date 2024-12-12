using System;
using VRage.Game.ModAPI;

namespace Torch.Commands.Permissions
{
    public class PermissionAttribute : Attribute
    {
        public MyPromoteLevel PromoteLevel { get; }

        public PermissionAttribute(MyPromoteLevel promoteLevel)
        {
            PromoteLevel = promoteLevel;
        }
    }
}
