using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

//Needed so Torch can set the instance here without exposing anything bad to mods or creating a circular dependency.
[assembly: InternalsVisibleTo("Torch")]
namespace Torch.API.ModAPI
{
    /// <summary>
    /// Entry point for mods to access Torch.
    /// </summary>
    public static class TorchAPI
    {
        internal static ITorchBase Instance;
    }
}
