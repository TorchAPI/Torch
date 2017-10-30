using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Torch.API.Managers;
using Torch.Managers.Profiler;
using Torch.Server.ViewModels;

namespace Torch.Server.Views
{
    /// <summary>
    /// Interaction logic for ProfilerControl.xaml
    /// </summary>
    public partial class ProfilerControl : UserControl
    {
        public ProfilerControl()
        {
            InitializeComponent();
        }

        public void BindServer(TorchServer server)
        {
            DataContext = new ProfilerViewModel(server.Managers.GetManager<ProfilerManager>());
        }
    }
}
