using Sandbox.Game.Entities;

namespace Torch.Server.ViewModels.Entities
{
    public class FloatingObjectViewModel : EntityViewModel
    {
        private MyFloatingObject Floating => (MyFloatingObject)Entity;

        public string Amount
        {
            get
            {
                if (Floating == null)
                    return "nil";
                var amt = (int)Floating.Amount.Value;
                return amt.ToString();
            }
        }

        public FloatingObjectViewModel(MyFloatingObject floating, EntityTreeViewModel tree) : base(floating, tree) { }

        public FloatingObjectViewModel()
        {
            
        }
    }
}
