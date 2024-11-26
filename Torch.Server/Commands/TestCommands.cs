using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace Torch.Server.Commands
{
    public class TestCommands : CommandModule
    {
        private TorchConfig Config => (TorchConfig)Context.Torch.Config;
        
        [Command("test")]
        [Alias("ThisISAnAlias")]
        [Permission(MyPromoteLevel.None)]
        public void Test()
        {
            Context.Respond("Hello World!");
        }
    }
}