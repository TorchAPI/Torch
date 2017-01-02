using Torch.API;

namespace Torch.Commands
{
    public class CommandModule
    {
        public ITorchPlugin Plugin { get; set; }
        public ITorchBase Server { get; set; }
    }
}