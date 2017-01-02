using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Library.Collections;

namespace Torch.Commands
{
    public class CommandTree
    {
        private Dictionary<string, Node> RootNodes { get; } = new Dictionary<string, Node>();

        public bool AddCommand(Command command)
        {
            var root = command.Path.First();
            var node = RootNodes.ContainsKey(root) ? RootNodes[root] : new Node(root);

            for (var i = 1; i < command.Path.Length; i++)
            {
                var current = command.Path[i];
                if (node.Nodes.ContainsKey(current))
                {
                    node = node.Nodes[current];
                    continue;
                }

                var newNode = new Node(current);
                node.AddNode(newNode);
                node = newNode;
            }

            if (!node.IsEmpty)
                return false;

            node.Command = command;
            return true;
        }

        public InvokeResult Invoke(ulong steamId, string[] command)
        {
            var root = command.First();
            if (!RootNodes.ContainsKey(root))
                return InvokeResult.NoCommand;

            var node = RootNodes[root];
            var args = new string[0];
            for (var i = 1; i < command.Length; i++)
            {
                var current = command[i];
                if (node.Nodes.ContainsKey(current))
                {
                    node = node.Nodes[current];
                    continue;
                }

                args = new string[command.Length - i];
                Array.Copy(command, i, args, 0, args.Length);
            }

            if (!node.IsCommand)
                return InvokeResult.NoCommand;

            //check permission here

            var context = new CommandContext(steamId, args);
            node.Command.Invoke(context);
            return InvokeResult.Success;
        }

        private class Node
        {
            public Dictionary<string, Node> Nodes { get; } = new Dictionary<string, Node>();

            public string Name { get; }
            public Command Command { get; set; }
            public bool IsCommand => Command != null;
            public bool IsEmpty => !IsCommand && Nodes.Count == 0;

            public Node(string name, Command command = null)
            {
                Name = name;
                Command = command;
            }

            public void AddNode(Node node)
            {
                Nodes.Add(node.Name, node);
            }
        }

        public enum InvokeResult
        {
            Success,
            NoCommand,
            NoPermission
        }
    }
}
