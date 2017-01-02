using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.API
{
    public interface ITorchBase
    {
        event Action SessionLoaded;
        IMultiplayer Multiplayer { get; }
        IPluginManager Plugins { get; }
        ILogger Log { get; set; }
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
    }

    public interface ITorchClient : ITorchBase
    {
        
    }
}
