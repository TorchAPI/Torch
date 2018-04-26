using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch.Views
{
    public class DisplayAttribute : Attribute
    {
        public string Name;
        public string Description;
        public string ToolTip;
        public string GroupName;
        public int? Order;
        public bool? Enabled;
        public bool? Visible;
        public bool? ReadOnly;

        public DisplayAttribute()
        { }

        public static implicit operator DisplayAttribute(System.ComponentModel.DataAnnotations.DisplayAttribute da)
        {
            return new DisplayAttribute()
                   {
                       Name = da.GetName(),
                       Description = da.GetDescription(),
                       GroupName = da.GetGroupName(),
                       Order = da.GetOrder()
                   };
        }
    }
}
