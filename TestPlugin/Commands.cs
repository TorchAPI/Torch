using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Torch.Commands;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace TestPlugin
{
    public class Commands : CommandModule
    {
        [Command("Ban", "Bans a player from the game")]
        public void Ban()
        {
            Context.Torch.Multiplayer.SendMessage("Boop!");
        }

        [Command("Unban", "Unbans a player from the game")]
        public void Unban()
        {
            Context.Torch.Multiplayer.SendMessage("Beep!");
        }
    }
}
