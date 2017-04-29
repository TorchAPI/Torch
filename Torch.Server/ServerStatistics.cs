using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Collections;

namespace Torch.Server
{
    public class ServerStatistics
    {
        public RollingAverage SimSpeed { get; } = new RollingAverage(30);
    }
}
