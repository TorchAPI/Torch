using System;

namespace Torch.Views
{
    public class DisplayAttribute : Attribute
    {
        public string Name;
        public string Description;
        public string ToolTip;
        public string GroupName;
        public int Order;
        public bool Enabled = true;
        public bool Visible = true;
        public bool ReadOnly = false;
        public Type EditorType = null;

        public DisplayAttribute()
        { }

        public static implicit operator DisplayAttribute(System.ComponentModel.DataAnnotations.DisplayAttribute da)
        {
            if (da == null)
                return null;

            return new DisplayAttribute()
                   {
                       Name = da.GetName(),
                       Description = da.GetDescription(),
                       GroupName = da.GetGroupName(),
                       Order = da.GetOrder() ?? 0
                   };
        }
    }
}
