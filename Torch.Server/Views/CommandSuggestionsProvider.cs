using System.Collections;
using System.Linq;
using AutoCompleteTextBox.Editors;
using Sandbox;
using Torch.API;
using Torch.API.Managers;
using Torch.Commands;

namespace Torch.Server.Views;

public class CommandSuggestionsProvider : ISuggestionProvider
{
    private readonly ITorchServer _server;
    private CommandManager _commandManager;

    public CommandSuggestionsProvider(ITorchServer server)
    {
        _server = server;
        if (_server.CurrentSession is null)
            _server.GameStateChanged += ServerOnGameStateChanged;
        else
            _commandManager = _server.CurrentSession.Managers.GetManager<CommandManager>();
    }

    private void ServerOnGameStateChanged(MySandboxGame game, TorchGameState newState)
    {
        if (_server.CurrentSession is { })
            _commandManager = _server.CurrentSession.Managers.GetManager<CommandManager>();
    }

    public IEnumerable GetSuggestions(string filter)
    {
        if (_commandManager is null || !_commandManager.IsCommand(filter))
            yield break;
        var args = filter[1..].Split(' ').ToList();
        var skip = _commandManager.Commands.GetNode(args, out var node);
        if (skip == -1)
            yield break;
        var lastArg = args.Last();
        
        foreach (var subcommandsKey in node.Subcommands.Keys)
        {
            if (lastArg != node.Name && !subcommandsKey.Contains(lastArg))
                continue;
            yield return $"!{string.Join(' ', node.GetPath())} {subcommandsKey}";
        }
    }
}