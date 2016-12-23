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
        void GameAction(Action action);
        void BeginGameAction(Action action, Action<object> callback = null, object state = null);
        void Start();
        void Stop();
        void Init();
    }

    public interface ITorchServer : ITorchBase
    {
        bool IsRunning { get; }
        string[] RunArgs { get; set; }
    }

    public interface ITorchClient : ITorchBase
    {
        
    }
}
