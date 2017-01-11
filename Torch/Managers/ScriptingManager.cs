using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.API;
using Torch.API.ModAPI.Ingame;
using VRage.Scripting;

namespace Torch.Managers
{
    public class ScriptingManager
    {
        private MyScriptWhitelist _whitelist;

        public void Init()
        {
            _whitelist = MyScriptCompiler.Static.Whitelist;
            MyScriptCompiler.Static.AddConditionalCompilationSymbols("TORCH");
            MyScriptCompiler.Static.AddReferencedAssemblies(typeof(ITorchBase).Assembly.Location);
            MyScriptCompiler.Static.AddImplicitIngameNamespacesFromTypes(typeof(GridExtensions));

            /*
            //dump whitelist
            var whitelist = new StringBuilder();
            foreach (var pair in MyScriptCompiler.Static.Whitelist.GetWhitelist())
            {
                var split = pair.Key.Split(',');
                whitelist.AppendLine("|-");
                whitelist.AppendLine($"|{pair.Value} || {split[0]} || {split[1]}");
            }
            Log.Info(whitelist);*/
        }

        public void UnwhitelistType(Type t)
        {
            throw new NotImplementedException();
        }
    }
}
