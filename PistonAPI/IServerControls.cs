using System.Windows.Controls;

namespace PistonAPI
{
    public interface IServerControls
    {
        bool AddGUITab(string name, UserControl control);
        bool RemoveGUITab(string name);
    }
}