using Torch.Commands;

namespace Torch.Server.Commands
{
    [Category("whitelist")]
    public class WhitelistCommands : CommandModule
    {
        private TorchConfig Config => (TorchConfig)Context.Torch.Config;

        [Command("on", "Enables the whitelist.")]
        public void On()
        {
            if (!Config.EnableWhitelist)
            {
                Config.EnableWhitelist = true;
                Context.Respond("Whitelist enabled.");
                Config.Save();
            }
            else
                Context.Respond("Whitelist is already enabled.");
        }

        [Command("off", "Disables the whitelist")]
        public void Off()
        {
            if (Config.EnableWhitelist)
            {
                Config.EnableWhitelist = false;
                Context.Respond("Whitelist disabled.");
                Config.Save();
            }
            else
                Context.Respond("Whitelist is already disabled.");
        }

        [Command("add", "Add a Steam ID to the whitelist.")]
        public void Add(ulong steamId)
        {
            if (!Config.Whitelist.Contains(steamId))
            {
                Config.Whitelist.Add(steamId);
                Context.Respond($"Added {steamId} to the whitelist.");
                Config.Save();
            }
            else
                Context.Respond($"{steamId} is already whitelisted.");
        }

        [Command("remove", "Remove a Steam ID from the whitelist.")]
        public void Remove(ulong steamId)
        {
            if (Config.Whitelist.Remove(steamId))
            {
                Context.Respond($"Removed {steamId} from the whitelist.");
                Config.Save();
            }
            else
                Context.Respond($"{steamId} is not whitelisted.");
        }
    }
}
