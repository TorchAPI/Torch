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
    /// <summary>
    /// A converter to get the index of a ModItemInfo object within a collection of ModItemInfo objects
    /// </summary>
    public class ModToListIdConverter : IMultiValueConverter
    {
        private static Logger Log = LogManager.GetLogger("TorchBase");

        /// <summary>
        /// Converts a ModItemInfo object into its index within a Collection of ModItemInfo objects
        /// </summary>
        /// <param name="values">
        /// Expected to contain a ModItemInfo object at index 0
        /// and a Collection of ModItemInfo objects at index 1
        /// </param>
        /// <param name="targetType">This parameter will be ignored</param>
        /// <param name="parameter">This parameter will be ignored</param>
        /// <param name="culture"> This parameter will be ignored</param>
        /// <returns>the index of the mod within the provided mod list.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //if (targetType != typeof(int))
            //    throw new NotSupportedException("ModToIdConverter can only convert mods into int values or vise versa!");
            var mod = (ModItemInfo) values[0];
            var theModList = (Collection<ModItemInfo>) values[1];
            return theModList.IndexOf(mod);
        }

        /// <summary>
        /// It is not supported to reverse this converter
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>Raises a NotSupportedException</returns>
        public object[] ConvertBack(object values, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ModToIdConverter can not convert back!");
        }
    }
}
