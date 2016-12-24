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
        void DoGameAction(Action action);
        Task DoGameActionAsync(Action action);
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
