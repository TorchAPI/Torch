using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API
{
    public interface ITorchBase
    {
        event Action SessionLoading;
        event Action SessionLoaded;
        event Action SessionUnloading;
        event Action SessionUnloaded;
        IMultiplayer Multiplayer { get; }
        IPluginManager Plugins { get; }
        Version TorchVersion { get; }
        void Invoke(Action action);
        void InvokeBlocking(Action action);
        Task InvokeAsync(Action action);
        string[] RunArgs { get; set; }
        void Start();
        void Stop();
        void Init();
    }

    public interface ITorchServer : ITorchBase
    {
        bool IsRunning { get; }
        string InstancePath { get; }
    }

    public interface ITorchClient : ITorchBase
    {
        
    }
}
