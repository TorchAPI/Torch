using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;

namespace Torch.Commands
{
    public class CommandTree
    {
        public DictionaryReader<string, CommandNode> Root { get; }
        private readonly Dictionary<string, CommandNode> _root = new Dictionary<string, CommandNode>();

        public CommandTree()
        {
            Root = new DictionaryReader<string, CommandNode>(_root);
        }

        public bool AddCommand(Command command)
        {
            var root = command.Path.First();

            if (!_root.ContainsKey(root))
            {
                _root.Add(root, new CommandNode(root));
            }

            var node = _root[root];

            for (var i = 1; i < command.Path.Count; i++)
            {
                var current = command.Path[i];
                if (node.Subcommands.ContainsKey(current))
                {
                    node = node.Subcommands[current];
                    continue;
                }

                var newNode = new CommandNode(current);
                node.AddNode(newNode);
                node = newNode;
            }

            if (!node.IsEmpty)
                return false;

            node.Command = command;
            return true;
        }

        /// <summary>
        /// Get a command node from the tree.
        /// </summary>
        /// <param name="path">Path to the command node.</param>
        /// <param name="commandNode"></param>
        /// <returns>The index of the first argument in the path or -1 if the node doesn't exist.</returns>
        public int GetNode(List<string> path, out CommandNode commandNode)
        {
            commandNode = null;
            var root = path.FirstOrDefault();
            if (root == null)
                return -1;

            if (!_root.ContainsKey(root))
                return -1;

            var node = _root[root];
            var i = 1;
            for (; i < path.Count; i++)
            {
                var current = path[i];
                if (node.Subcommands.ContainsKey(current))
                {
                    node = node.Subcommands[current];
                }
                else
                {
                    break;
                }
            }

            commandNode = node;
            return i;
        }

        public Command GetCommand(List<string> path, out List<string> args)
        {
            args = new List<string>();
            var skip = GetNode(path, out CommandNode node);
            args.AddRange(path.Skip(skip));
            return node.Command;
        }

        public Command GetCommand(string commandText, out string argText)
        {
            var split = commandText.Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries).ToList();
            var skip = GetNode(split, out CommandNode node);
            if (skip == -1)
            {
                argText = "";
                return null;
            }

            if (split.Count > skip)
            {
                var substringIndex = commandText.IndexOf(split[skip]);
                if (substringIndex <= commandText.Length)
                {
                    argText = commandText.Substring(substringIndex);
                    return node.Command;
                }
            }

            argText = "";
            return node.Command;
        }

        public string GetTreeString()
        {
            var indent = 0;
            var sb = new StringBuilder();
            foreach (var node in _root)
            {
                DebugNode(node.Value, sb, ref indent);
            }

            return sb.ToString();
        }

        public IEnumerable<CommandNode> WalkTree(CommandNode root = null)
        {
            foreach (var node in root?.GetChildren() ?? _root.Values)
            {
                yield return node;

                foreach (var child in WalkTree(node))
                    yield return child;
            }
        }

        public bool DeleteNode(CommandNode node)
        {
            if (node.Parent != null)
            {
                node.Parent.RemoveNode(node);
                return true;
            }
            if (node.Command?.Path != null)
                return _root.Remove(node.Command.Path.First());

            return false;
        }

        private void DebugNode(CommandNode commandNode, StringBuilder sb, ref int indent)
        {
            sb.AppendLine(new string(' ', indent) + commandNode.Name);
            indent += 2;

            foreach (var n in commandNode.Subcommands)
            {
                DebugNode(n.Value, sb, ref indent);
            }

            indent -= 2;
        }

        public class CommandNode
        {
            public CommandNode Parent { get; private set; }
            public DictionaryReader<string, CommandNode> Subcommands { get; }
            private readonly Dictionary<string, CommandNode> _subcommands = new Dictionary<string, CommandNode>();

            public string Name { get; }
            public Command Command { get; set; }
            public bool IsCommand => Command != null;
            public bool IsEmpty => !IsCommand && _subcommands.Count == 0;

            public CommandNode(string name)
            {
                Subcommands = new DictionaryReader<string, CommandNode>(_subcommands);
                Name = name;
            }

            public CommandNode(Command command)
            {
                Name = command.Name;
                Command = command;
            }

            public bool TryGetChild(string name, out CommandNode node)
            {
                return Subcommands.TryGetValue(name, out node);
            }

            public IEnumerable<CommandNode> GetChildren()
            {
                return _subcommands.Values;
            }

            public List<string> GetPath()
            {
                var path = new List<string> {Name};
                if (Parent != null)
                    path.InsertRange(0, Parent.GetPath());

                return path;
            }

            public void AddNode(CommandNode commandNode)
            {
                commandNode.Parent = this;
                _subcommands.Add(commandNode.Name, commandNode);
            }

            public void RemoveNode(CommandNode commandNode)
            {
                commandNode.Parent = null;
                _subcommands.Remove(commandNode.Name);
            }
        }
    }
}
