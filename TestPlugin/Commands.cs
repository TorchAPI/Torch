using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;

namespace TestPlugin
{
    [Category("admin", "tools")]
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
