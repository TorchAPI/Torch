using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Torch.Server.ViewModels;
using VRage.Game;

namespace Torch.Server.Views.Converters
{
    class ListConverterWorkshopId : IValueConverter
    {
        public Type Type { get; set; }

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IList list))
                throw new InvalidOperationException("Value is not the proper type.");

            var sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.AppendLine(((ModItemInfo) item).PublishedFileId.ToString());
            }

            return sb.ToString();
        }

        /// <inheritdoc />
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
                        list.Add(new MyObjectBuilder_Checkpoint.ModItem(id));
                }
            }

            return list;
        }
    }
}
