using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API
{
    public interface IChatItem
    {
        IPlayer Player { get; }
        string Message { get; }
        DateTime Time { get; }
    }
}
