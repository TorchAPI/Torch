using Torch.API;

namespace Torch.Commands
{
    public class ChatCommandModule
    {
        public ITorchPlugin Plugin { get; set; }
        public ITorchServer Server { get; set; }
    }
}