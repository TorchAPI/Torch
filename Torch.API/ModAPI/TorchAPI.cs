using System.Runtime.CompilerServices;

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
