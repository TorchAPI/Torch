namespace Torch
{
    public interface ITorchConfig
    {
        //bool AutoRestart { get; set; }
        //int Autosave { get; set; }
        string InstanceName { get; set; }
        string InstancePath { get; set; }
        //bool LogChat { get; set; }
        bool RedownloadPlugins { get; set; }
        bool EnableAutomaticUpdates { get; set; }

        bool Save(string path = null);
    }
}