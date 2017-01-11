using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace Torch.Commands
{
    public class DefaultPermissionAttribute : Attribute
    {
        public MyPromoteLevel Level { get; }

        public DefaultPermissionAttribute(MyPromoteLevel level)
        {
            Level = level;
        }
    }
}
