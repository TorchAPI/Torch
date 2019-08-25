using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using NLog;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using Steamworks;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Commands.Permissions;
using Torch.Managers;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Torch.Commands
{
    public class TorchCommands : CommandModule
    {
        private static bool _restartPending = false;
        private static bool _cancelRestart = false;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        [Command("whatsmyip")]
        [Permission(MyPromoteLevel.None)]
        public void GetIP(ulong steamId = 0)
        {
            if (steamId == 0)
                steamId = Context.Player.SteamUserId;

            VRage.GameServices.MyP2PSessionState statehack = new VRage.GameServices.MyP2PSessionState();
            VRage.Steam.MySteamService.Static.Peer2Peer.GetSessionState(steamId, ref statehack);
            var ip = new IPAddress(BitConverter.GetBytes(statehack.RemoteIP).Reverse().ToArray());
            Context.Respond($"Your IP is {ip}");
        }

        [Command("help", "Displays help for a command")]
        [Permission(MyPromoteLevel.None)]
        public void Help()
        {
            var commandManager = Context.Torch.CurrentSession?.Managers.GetManager<CommandManager>();
            if (commandManager == null)
            {
                Context.Respond("Must have an attached session to list commands");
                return;
            }
            commandManager.Commands.GetNode(Context.Args, out CommandTree.CommandNode node);

            if (node != null)
            {
                var command = node.Command;
                var children = node.Subcommands.Where(e => Context.Player == null || e.Value.Command?.MinimumPromoteLevel <= Context.Player.PromoteLevel).Select(x => x.Key);

                var sb = new StringBuilder();

                if (command?.MinimumPromoteLevel > Context.Player.PromoteLevel)
                {
                    Context.Respond("You are not authorized to use this command.");
                    return;
                }

                if (command != null)
                {
                    sb.AppendLine($"Syntax: {command.SyntaxHelp}");
                    sb.Append(command.HelpText);
                }

                if (node.Subcommands.Count() != 0)
                    sb.Append($"\nSubcommands: {string.Join(", ", children)}");

                Context.Respond(sb.ToString());
            }
            else
            {
                Context.Respond(
                    $"Command not found. Use the {commandManager.Prefix}longhelp command and check your Comms menu for a full list of commands.");
            }
        }

        [Command("longhelp", "Get verbose help. Will send a long message in a dialog window.")]
        [Permission(MyPromoteLevel.None)]
        public void LongHelp()
        {
            var commandManager = Context.Torch.CurrentSession?.Managers.GetManager<CommandManager>();
            if (commandManager == null)
            {
                Context.Respond("Must have an attached session to list commands");
                return;
            }
            commandManager.Commands.GetNode(Context.Args, out CommandTree.CommandNode node);

            if (node != null)
            {
                var command = node.Command;
                var children = node.Subcommands.Where(e => e.Value.Command?.MinimumPromoteLevel <= Context.Player.PromoteLevel).Select(x => x.Key);

                var sb = new StringBuilder();

                if (command != null && (Context.Player == null || command.MinimumPromoteLevel <= Context.Player.PromoteLevel))
                {
                    sb.AppendLine($"Syntax: {command.SyntaxHelp}");
                    sb.Append(command.HelpText);
                }

                if (node.Subcommands.Count() != 0)
                    sb.Append($"\nSubcommands: {string.Join(", ", children)}");

                Context.Respond(sb.ToString());
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var command in commandManager.Commands.WalkTree())
                {
                    if (command.IsCommand && (Context.Player == null || command.Command.MinimumPromoteLevel <= Context.Player.PromoteLevel))
                        sb.AppendLine($"{command.Command.SyntaxHelp}\n    {command.Command.HelpText}");
                }

                if (!Context.SentBySelf)
                {
                    var m = new DialogMessage("Torch Help", subtitle: "Available commands:", content: sb.ToString());
                    ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
                }
                else
                    Context.Respond($"Available commands: {sb}");
            }
        }

        [Command("ver", "Shows the running Torch version.")]
        [Permission(MyPromoteLevel.None)]
        public void Version()
        {
            var ver = Context.Torch.TorchVersion;
            Context.Respond($"Torch version: {ver} SE version: {MyFinalBuildConstants.APP_VERSION}");
        }

        [Command("plugins", "Lists the currently loaded plugins.")]
        [Permission(MyPromoteLevel.None)]
        public void Plugins()
        {
            var plugins = Context.Torch.Managers.GetManager<PluginManager>()?.Plugins.Select(p => p.Value.Name) ??
                          Enumerable.Empty<string>();
            Context.Respond($"Loaded plugins: {string.Join(", ", plugins)}");
        }

        [Command("stop", "Stops the server.")]
        public void Stop(bool save = true)
        {
            Context.Respond("Stopping server.");
            if (save)
                DoSave()?.ContinueWith((a, mod) =>
                {
                    ITorchBase torch = (mod as CommandModule)?.Context?.Torch;
                    Debug.Assert(torch != null);
                    torch.Stop();
                }, this, TaskContinuationOptions.RunContinuationsAsynchronously);
            else
                Context.Torch.Stop();
        }

        [Command("restart", "Restarts the server.")]
        public void Restart(int countdownSeconds = 10, bool save = true)
        {
            if (_restartPending)
            {
                Context.Respond("A restart is already pending.");
                return;
            }
        
            _restartPending = true;
            
            Task.Run(() =>
            {
                var countdown = RestartCountdown(countdownSeconds, save).GetEnumerator();
                while (countdown.MoveNext())
                {
                    Thread.Sleep(1000);
                }
            });
        }

        [Command("notify", "Shows a message as a notification in the middle of all players' screens.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Notify(string message, int disappearTimeMs = 2000, string font = "White")
        {
            ModCommunication.SendMessageToClients(new NotificationMessage(message, disappearTimeMs, font));
        }

        [Command("restart cancel", "Cancel a pending restart.")]
        public void CancelRestart()
        {
            if (_restartPending)
                _cancelRestart = true;
            else
                Context.Respond("A restart is not pending.");
        }

        private IEnumerable RestartCountdown(int countdown, bool save)
        {
            for (var i = countdown; i >= 0; i--)
            {
                if (_cancelRestart)
                {
                    Context.Torch.CurrentSession.Managers.GetManager<IChatManagerClient>()
                           .SendMessageAsSelf($"Restart cancelled.");

                    _restartPending = false;
                    _cancelRestart = false;
                    yield break;
                }
                    
                if (i >= 60 && i % 60 == 0)
                {
                    Context.Torch.CurrentSession.Managers.GetManager<IChatManagerClient>()
                        .SendMessageAsSelf($"Restarting server in {i / 60} minute{Pluralize(i / 60)}.");
                    yield return null;
                }
                else if (i > 0)
                {
                    if (i < 11)
                        Context.Torch.CurrentSession.Managers.GetManager<IChatManagerClient>()
                            .SendMessageAsSelf($"Restarting server in {i} second{Pluralize(i)}.");
                    yield return null;
                }
                else
                {
                    if (save)
                    {
                        Log.Info("Savin game before restart.");
                        Context.Torch.CurrentSession.Managers.GetManager<IChatManagerClient>()
                           .SendMessageAsSelf($"Saving game before restart.");
                    }

                    Log.Info("Restarting server.");
                    Context.Torch.Invoke(() => Context.Torch.Restart(save));
                        
                    yield break;
                }
            }
        }

        private string Pluralize(int num)
        {
            return num == 1 ? "" : "s";
        }

        /// <summary>
        /// Initializes a save of the game.
        /// Caller id defaults to 0 in the case of triggering the chat command from server.
        /// </summary>
        [Command("save", "Saves the game.")]
        public void Save()
        {
            Context.Respond("Saving game.");
            DoSave();
        }

        private Task DoSave()
        {
            Task<GameSaveResult> task = Context.Torch.Save(60 * 1000, true);
            if (task == null)
            {
                Context.Respond("Save failed, a save is already in progress");
                return null;
            }
            return task.ContinueWith((taskCapture, state) =>
            {
                CommandContext context = (state as CommandModule)?.Context;
                Debug.Assert(context != null);
                switch (taskCapture.Result)
                {
                    case GameSaveResult.Success:
                        context.Respond("Saved game.");
                        break;
                    case GameSaveResult.GameNotReady:
                        context.Respond("Save failed: Game was not ready.");
                        break;
                    case GameSaveResult.TimedOut:
                        context.Respond("Save failed: Save timed out.");
                        break;
                    case GameSaveResult.FailedToTakeSnapshot:
                        context.Respond("Save failed: unable to take snapshot");
                        break;
                    case GameSaveResult.FailedToSaveToDisk:
                        context.Respond("Save failed: unable to save to disk");
                        break;
                    case GameSaveResult.UnknownError:
                        context.Respond("Save failed: unknown reason");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }, this, TaskContinuationOptions.RunContinuationsAsynchronously);
        }

        [Command("uptime", "Check how long the server has been online.")]
        public void Uptime()
        {
            Context.Respond(((ITorchServer)Context.Torch).ElapsedPlayTime.ToString());
        }
    }
}
