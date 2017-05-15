using Sandbox.Game.Entities;
using Torch.Server.ViewModels.Entities;

namespace Torch.Server.ViewModels
{
    public class FloatingObjectViewModel : EntityViewModel
    {
        private MyFloatingObject Floating => (MyFloatingObject)Entity;

        public override string Name => $"{base.Name} ({Floating.Amount})";

        public FloatingObjectViewModel(MyFloatingObject floating) : base(floating) { }
    }
}
