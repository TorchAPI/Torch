using System.IO;
using Torch.API;
namespace Torch;

public class ApplicationContext : IApplicationContext
{
    public static IApplicationContext Current { get; private set; }
    public ApplicationContext(DirectoryInfo torchDirectory, DirectoryInfo gameFilesDirectory, DirectoryInfo gameBinariesDirectory, 
        DirectoryInfo instanceDirectory, string instanceName, bool isService)
    {
        TorchDirectory = torchDirectory;
        GameFilesDirectory = gameFilesDirectory;
        GameBinariesDirectory = gameBinariesDirectory;
        InstanceDirectory = instanceDirectory;
        InstanceName = instanceName;
        IsService = isService;
        Current = this;
    }

    /// <inheritdoc />
    public DirectoryInfo TorchDirectory { get; }
    /// <inheritdoc />
    public DirectoryInfo GameFilesDirectory { get; }
    /// <inheritdoc />
    public DirectoryInfo GameBinariesDirectory { get; }
    /// <inheritdoc />
    public DirectoryInfo InstanceDirectory { get; }
    /// <inheritdoc />
    public string InstanceName { get; }
    /// <inheritdoc />
    public bool IsService { get; }
}
