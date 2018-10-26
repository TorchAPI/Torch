using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Threading.Tasks;
using Torch.Server.ViewModels;
using NLog;

namespace Torch.Server.Views.Converters
{
    public class ModToListIdConverter : IMultiValueConverter
    {
        private static Logger Log = LogManager.GetLogger("TorchBase");

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //if (targetType != typeof(int))
            //    throw new NotSupportedException("ModToIdConverter can only convert mods into int values or vise versa!");
            var mod = (ModItemInfo) values[0];
            var theModList = (Collection<ModItemInfo>) values[1];
            return theModList.IndexOf(mod);
        }

        public object[] ConvertBack(object values, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ModToIdConverter can not convert back!");
        }
    }
}
