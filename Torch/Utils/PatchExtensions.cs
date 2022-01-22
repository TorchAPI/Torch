using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Torch.Managers.PatchManager;

namespace Torch.Utils;

public static class PatchExtensions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AddPrefix(this MethodRewritePattern pattern, string methodName = "Prefix")
    {
        pattern.Prefixes.Add(GetPatchingMethod(methodName));
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AddSuffix(this MethodRewritePattern pattern, string methodName = "Suffix")
    {
        pattern.Suffixes.Add(GetPatchingMethod(methodName));
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AddTranspiler(this MethodRewritePattern pattern, string methodName = "Transpiler")
    {
        pattern.Transpilers.Add(GetPatchingMethod(methodName));
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AddPostTranspiler(this MethodRewritePattern pattern, string methodName = "PostTranspiler")
    {
        pattern.PostTranspilers.Add(GetPatchingMethod(methodName));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static MethodInfo GetPatchingMethod(string name)
    {
        if (new StackFrame(2, false).GetMethod()?.DeclaringType is { } type)
            return type.GetMethod(name,
                       BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) ??
                   throw new MissingMethodException(type.FullName, name);
        throw new InvalidOperationException("Unable to retrieve previous stackframe method");
    }
}