using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Torch.Server
{
    public class NativeMethods
    {
        [DllImport("kernel32")]
        public static extern bool AllocConsole();

        [DllImport("kernel32")]
        public static extern bool FreeConsole();
    }
}
