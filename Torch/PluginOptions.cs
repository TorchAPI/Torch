using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torch
{
    public class PluginOptions
    {
        public virtual string Save()
        {
            return null;
        }

        public virtual void Load(string data)
        {
            
        }
    }
}
