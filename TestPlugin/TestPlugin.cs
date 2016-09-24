using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Piston.API;
using PistonAPI;
using Sandbox;

namespace TestPlugin
{
    [Plugin("Test Plugin")]
    public class TestPlugin : IPistonPlugin
    {
        public void Dispose()
        {
            MySandboxGame.Log.WriteLineAndConsole("TestDispose");
        }

        public void Init(object gameInstance)
        {
            MySandboxGame.Log.WriteLineAndConsole("TestInit");
        }

        public void Update()
        {
            MySandboxGame.Log.WriteLineAndConsole("TestUpdate");
        }

        public void Reload()
        {
            MySandboxGame.Log.WriteLineAndConsole("TestReload");
        }
    }
}
