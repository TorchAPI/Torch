using System.IO;
namespace Torch.API;

public interface IApplicationContext
{
    /// <summary>
    /// Directory contains torch binaries.
    /// </summary>
    public DirectoryInfo TorchDirectory { get; }
    /// <summary>
    /// Root directory for all game files.
    /// </summary>
    public DirectoryInfo GameFilesDirectory { get; }
    /// <summary>
    /// Directory contains game binaries.
    /// </summary>
    public DirectoryInfo GameBinariesDirectory { get; }
    /// <summary>
    /// Current instance directory.
    /// </summary>
    public DirectoryInfo InstanceDirectory { get; }
    /// <summary>
    /// Current instance name.
    /// </summary>
    public string InstanceName { get; }
    /// <summary>
    /// Application running in service mode.
    /// </summary>
    public bool IsService { get; }
}
