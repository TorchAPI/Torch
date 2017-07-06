using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.API.Managers;
using VRage.Game.ModAPI;

namespace Torch.API
{
    public interface ITorchBase
    {
        event Action SessionLoading;
        event Action SessionLoaded;
        event Action SessionUnloading;
        event Action SessionUnloaded;
        ITorchConfig Config { get; }
        IMultiplayerManager Multiplayer { get; }
        IPluginManager Plugins { get; }
        Version TorchVersion { get; }
        void Invoke(Action action);
        void InvokeBlocking(Action action);
        Task InvokeAsync(Action action);
        string[] RunArgs { get; set; }
        bool IsOnGameThread();
        void Start();
        void Stop();
        void Save(long callerId);
        void Init();
        T GetManager<T>() where T : class, IManager;
    }

    public interface ITorchServer : ITorchBase
    {
        string InstancePath { get; }
    }

    public interface ITorchClient : ITorchBase
    {
        
    }
}
