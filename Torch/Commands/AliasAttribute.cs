using System;

namespace Torch.Commands.Permissions
{
    public class AliasAttribute : Attribute
    {
        public string Alias { get; }

        public AliasAttribute(string alias)
        {
            Alias = alias;
        }
    }
}