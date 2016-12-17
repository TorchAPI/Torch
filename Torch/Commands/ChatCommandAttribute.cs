using System;

namespace Torch.Commands
{
    public class ChatCommandAttribute : Attribute
    {
        public string Name { get; }
        public ChatCommandAttribute(string name)
        {
            Name = name;
        }
    }
}