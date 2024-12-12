using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using Torch.Server.ViewModels;
using Torch.Utils;

namespace Torch.Server.Views.Converters
{
    class ListConverterWorkshopId : IValueConverter
    {
        public Type Type { get; set; }

        /// <summary>
        /// Converts a list of ModItemInfo objects into a list of their workshop IDs (PublishedFileIds).
        /// </summary>
        /// <param name="valueList">
        /// Expected to contain a list of ModItemInfo objects
        /// </param>
        /// <param name="targetType">This parameter will be ignored</param>
        /// <param name="parameter">This parameter will be ignored</param>
        /// <param name="culture"> This parameter will be ignored</param>
        /// <returns>A string containing the workshop ids of all mods, one per line</returns>
        public object Convert(object valueList, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(valueList is IList list))
                throw new InvalidOperationException("Value is not the proper type.");

            var sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.AppendLine(((ModItemInfo) item).PublishedFileId.ToString());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts a list of workshop ids into a list of ModItemInfo objects
        /// </summary>
        /// <param name="value">A string containing workshop ids separated by new lines</param>
        /// <param name="targetType">This parameter will be ignored</param>
        /// <param name="parameter">
        /// A list of ModItemInfos which should 
        /// contain the requestted mods
        /// (or they will be dropped)
        /// </param>
        /// <param name="culture">This parameter will be ignored</param>
        /// <returns>A list of ModItemInfo objects</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(Type));
            var mods = parameter as ICollection<ModItemInfo>;
            if (mods == null)
                throw new ArgumentException("parameter needs to be of type ICollection<ModItemInfo>!");
            var input = ((string)value).Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in input)
            {
                if( ulong.TryParse(item, out ulong id))
                {
                    var mod = mods.FirstOrDefault((m) => m.PublishedFileId == id);
                    if (mod != null)
                        list.Add(mod);
                    else
                        list.Add(ModItemUtils.Create(id));
                }
            }

            return list;
        }
    }
}
