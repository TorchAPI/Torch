using System;
using System.Reflection;

namespace Torch.Managers.PatchManager
{
    public class PatchException : Exception
    {
        public PatchException(string message, MemberInfo targetMember) : base(
            $"Patching exception in {targetMember.DeclaringType?.FullName}::{targetMember.Name}: {message}")
        {
        }
    }
}