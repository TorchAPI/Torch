using VRage.Game;

namespace Torch.API.Managers;

public interface IInstanceManager : IManager
{
    IWorld SelectedWorld { get; } 
    void LoadInstance(string path, bool validate = true);
    void SelectCreatedWorld(string worldPath);
    void SelectWorld(string worldPath, bool modsOnly = true);
    void ImportSelectedWorldConfig();
    void SaveConfig();
}

public interface IWorld
{
    string FolderName { get; }
    string WorldPath { get; }
    MyObjectBuilder_SessionSettings KeenSessionSettings { get; }
    MyObjectBuilder_Checkpoint KeenCheckpoint { get; }
}