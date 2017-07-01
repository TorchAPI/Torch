using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Torch
{
    /// <summary>
    /// Provides a method to notify an observer of changes to an object's properties.
    /// </summary>
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        /// <summary>
        /// Fires PropertyChanged for all properties.
        /// </summary>
        public void RefreshModel()
        {
            foreach (var propName in GetType().GetProperties().Select(x => x.Name))
                // ReSharper disable once ExplicitCallerInfoArgument
                OnPropertyChanged(propName);
        }
    }
}
