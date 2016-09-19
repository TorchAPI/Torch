using System.Windows.Controls;

namespace Piston.API
{
    public interface IServerControls
    {
        bool AddGUITab(string name, UserControl control);
        bool RemoveGUITab(string name);
    }
}