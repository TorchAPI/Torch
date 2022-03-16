#if !NETFRAMEWORK
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Meta;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;
using VRage.FileSystem;
using VRage.Scripting;

namespace Torch.Patches
{
    [PatchShim]
    public static class ScriptCompilerPatch
    {
        [ReflectedMethodInfo(typeof(MyScriptWhitelist), "Register", Parameters = new[] {typeof(MyWhitelistTarget), typeof(INamespaceSymbol), typeof(Type)})]
        private static MethodInfo Register1Method = null!;

        [ReflectedMethodInfo(typeof(MyScriptWhitelist), "Register", Parameters = new[] {typeof(MyWhitelistTarget), typeof(ITypeSymbol), typeof(Type)})]
        private static MethodInfo Register2Method = null!;
        
        public static void Patch(PatchContext context)
        {
            context.GetPattern(typeof(MyScriptWhitelist).GetConstructor(new[] {typeof(MyScriptCompiler)}))
                .AddPrefix(nameof(WhitelistCtorPrefix));
            context.GetPattern(Type.GetType("VRage.Scripting.MyVRageScriptingInternal, VRage.Scripting", true).GetMethod("Initialize"))
                .AddPrefix(nameof(InitializePrefix));
            context.GetPattern(typeof(MyScriptWhitelist).GetNestedType("MyWhitelistBatch", BindingFlags.NonPublic)!
                .GetMethod("AllowMembers")).AddPrefix(nameof(AllowMembersPrefix));
            context.GetPattern(Register1Method).AddTranspiler(nameof(RegisterTranspiler));
            context.GetPattern(Register2Method).AddTranspiler(nameof(RegisterTranspiler));
        }

        private static void WhitelistCtorPrefix(MyScriptCompiler scriptCompiler, MyScriptWhitelist __instance)
        {
            var basePath = new FileInfo(typeof(object).Assembly.Location).DirectoryName!;
            
            scriptCompiler.AddReferencedAssemblies(
                Path.Combine(basePath, "netstandard.dll"),
                Path.Combine(basePath, "mscorlib.dll"),
                Path.Combine(basePath, "System.Runtime.dll"),
                typeof(LinkedList<>).Assembly.Location,
                typeof(Regex).Assembly.Location,
                typeof(Enumerable).Assembly.Location,
                typeof(ConcurrentBag<>).Assembly.Location,
                typeof(ImmutableArray).Assembly.Location,
                typeof(PropertyChangedEventArgs).Assembly.Location,
                typeof(TypeConverter).Assembly.Location,
                typeof(TraceSource).Assembly.Location,
                typeof(RuntimeTypeModel).Assembly.Location,
                typeof(ProtoMemberAttribute).Assembly.Location,
                Path.Combine(MyFileSystem.ExePath, "Sandbox.Game.dll"),
                Path.Combine(MyFileSystem.ExePath, "Sandbox.Common.dll"),
                Path.Combine(MyFileSystem.ExePath, "Sandbox.Graphics.dll"),
                Path.Combine(MyFileSystem.ExePath, "VRage.dll"),
                Path.Combine(MyFileSystem.ExePath, "VRage.Library.dll"),
                Path.Combine(MyFileSystem.ExePath, "VRage.Math.dll"),
                Path.Combine(MyFileSystem.ExePath, "VRage.Game.dll"),
                Path.Combine(MyFileSystem.ExePath, "VRage.Render.dll"),
                Path.Combine(MyFileSystem.ExePath, "VRage.Input.dll"),
                Path.Combine(MyFileSystem.ExePath, "SpaceEngineers.ObjectBuilders.dll"),
                Path.Combine(MyFileSystem.ExePath, "SpaceEngineers.Game.dll"));
        }
        
        private static bool InitializePrefix(Thread updateThread, Type[] referencedTypes, string[] symbols)
        {
            MyModWatchdog.Init(updateThread);
            MyScriptCompiler.Static.AddImplicitIngameNamespacesFromTypes(referencedTypes);
            MyScriptCompiler.Static.AddConditionalCompilationSymbols(symbols);
            using var batch = MyScriptCompiler.Static.Whitelist.OpenBatch();
            // Dict and queue in different assemblies, microsoft being microsoft
            batch.AllowNamespaceOfTypes(MyWhitelistTarget.ModApi, typeof(ConcurrentQueue<>));
            return false;
        }
        
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
        private static void AllowMembersPrefix(ref MemberInfo[] members)
        {
            if (members.Any(b => b is null))
                members = members.Where(b => b is { }).ToArray();
        }

        private static IEnumerable<MsilInstruction> RegisterTranspiler(IEnumerable<MsilInstruction> instructions)
        {
            var ins = instructions.ToList();
            var throwIns = ins.FindAll(b => b.OpCode == OpCodes.Throw).Select(b => ins.IndexOf(b));
            foreach (var index in throwIns)
            {
                var i = index;
                do
                {
                    ins[i] = new(OpCodes.Nop);
                } while (ins[--i].OpCode.OperandType != OperandType.ShortInlineBrTarget);

                ins[index] = new(OpCodes.Ret);
            }

            return ins;
        }
    }
}
#endif