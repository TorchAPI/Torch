using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
