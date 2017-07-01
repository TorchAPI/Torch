namespace Torch
{
    public interface ITorchConfig
    {
        string InstanceName { get; set; }
        string InstancePath { get; set; }
        bool RedownloadPlugins { get; set; }
        bool AutomaticUpdates { get; set; }
        bool RestartOnCrash { get; set; }
        int TickTimeout { get; set; }

        bool Save(string path = null);
    }
}