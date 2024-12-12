using System.Runtime.InteropServices;

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
