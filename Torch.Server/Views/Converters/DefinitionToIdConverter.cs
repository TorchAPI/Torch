using System;
using System.Globalization;
using System.Windows.Data;
using Sandbox.Definitions;
using VRage.Game;

namespace Torch.Server.Views.Converters
{
    public class DefinitionToIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ReSharper disable once PossibleNullReferenceException
            MyDefinitionId id = ((MyDefinitionBase) value).Id;
            string typeName = id.TypeId.ToString();
            if (typeName.StartsWith("MyObjectBuilder_"))
                typeName = typeName.Substring("MyObjectBuilder_".Length);
            string subtype = id.SubtypeName;
            return string.IsNullOrWhiteSpace(subtype) ? typeName : $"{typeName}: {subtype}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ReSharper disable once PossibleNullReferenceException
            string[] parts = value.ToString().Split(':');
            Type type;
            try
            {
                type = Type.GetType(parts[0]);
            }
            catch
            {
                type = Type.GetType("MyObjectBuilder_" + parts[0]);
            }
            return MyDefinitionManager.Static.GetDefinition(
                new MyDefinitionId(type, parts.Length > 1 ? parts[1].Trim() : ""));
        }
    }
}