namespace Torch.API
{
    public interface IEnvironmentInfo
    {
        EnvironmentType Type { get; }
    }

    public enum EnvironmentType
    {
        DedicatedServer,
        GameClient
    }
}