using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;
using Xunit;

// ReSharper disable UnusedMember.Local
namespace Torch.Tests
{
#pragma warning disable 414
    public class PatchTest
    {
        #region TestRunner

        private static readonly PatchManager _patchContext = new PatchManager(null);

        [Theory]
        [MemberData(nameof(Prefixes))]
        public void TestPrefix(TestBootstrap runner)
        {
            runner.TestPrefix();
        }

        [Theory]
        [MemberData(nameof(Transpilers))]
        public void TestTranspile(TestBootstrap runner)
        {
            runner.TestTranspile();
        }

        [Theory]
        [MemberData(nameof(Suffixes))]
        public void TestSuffix(TestBootstrap runner)
        {
            runner.TestSuffix();
        }

        [Theory]
        [MemberData(nameof(Combo))]
        public void TestCombo(TestBootstrap runner)
        {
            runner.TestCombo();
        }


        [Fact]
        public void TestTryCatchNop()
        {
            var ctx = _patchContext.AcquireContext();
            ctx.GetPattern(TryCatchTest._target).Transpilers.Add(_nopTranspiler);
            _patchContext.Commit();
            Assert.False(TryCatchTest.Target());
            Assert.True(TryCatchTest.FinallyHit);
            _patchContext.FreeContext(ctx);
            _patchContext.Commit();
        }

        [Fact]
        public void TestTryCatchCancel()
        {
            var ctx = _patchContext.AcquireContext();
            ctx.GetPattern(TryCatchTest._target).Transpilers.Add(TryCatchTest._removeThrowTranspiler);
            ctx.GetPattern(TryCatchTest._target).DumpTarget = @"C:\tmp\dump.txt";
            ctx.GetPattern(TryCatchTest._target).DumpMode = MethodRewritePattern.PrintModeEnum.Original | MethodRewritePattern.PrintModeEnum.Patched;
            _patchContext.Commit();
            Assert.True(TryCatchTest.Target());
            Assert.True(TryCatchTest.FinallyHit);
            _patchContext.FreeContext(ctx);
            _patchContext.Commit();
        }

        private static readonly MethodInfo _nopTranspiler = typeof(PatchTest).GetMethod(nameof(NopTranspiler), BindingFlags.Static | BindingFlags.NonPublic);

        private static IEnumerable<MsilInstruction> NopTranspiler(IEnumerable<MsilInstruction> input)
        {
            return input;
        }

        private class TryCatchTest
        {
            public static readonly MethodInfo _removeThrowTranspiler =
                typeof(TryCatchTest).GetMethod(nameof(RemoveThrowTranspiler), BindingFlags.Static | BindingFlags.NonPublic);

            private static IEnumerable<MsilInstruction> RemoveThrowTranspiler(IEnumerable<MsilInstruction> input)
            {
                foreach (var i in input)
                    if (i.OpCode == OpCodes.Throw)
                        yield return i.CopyWith(OpCodes.Pop);
                    else
                        yield return i;
            }

            public static readonly MethodInfo _target = typeof(TryCatchTest).GetMethod(nameof(Target), BindingFlags.Public | BindingFlags.Static);

            public static bool FinallyHit = false;

            public static bool Target()
            {
                FinallyHit = false;
                try
                {
                    try
                    {
                        // shim to prevent compiler optimization
                        if ("test".Length > "".Length)
                            throw new Exception();
                        return true;
                    }
                    catch (IOException ioe)
                    {
                        return false;
                    }
                    catch (Exception e)
                    {
                        return false;
                    }
                    finally
                    {
                        FinallyHit = true;
                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }

        [Fact]
        public void TestAsyncNop()
        {
            var candidates = new List<Type>();
            var nestedTypes = typeof(PatchTest).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var nested in nestedTypes)
                if (nested.Name.StartsWith("<" + nameof(TestAsyncMethod) + ">"))
                {
                    var good = false;
                    foreach (var itf in nested.GetInterfaces())
                        if (itf.FullName == typeof(IAsyncStateMachine).FullName)
                        {
                            good = true;
                            break;
                        }

                    if (good)
                        candidates.Add(nested);
                }

            if (candidates.Count != 1)
                throw new Exception("Couldn't find async worker");

            var method = candidates[0].GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (method == null)
                throw new Exception("Failed to find state machine move next instruction, cannot proceed");

            var ctx = _patchContext.AcquireContext();
            ctx.GetPattern(method).Transpilers.Add(_nopTranspiler);
            ctx.GetPattern(method).DumpTarget = @"C:\tmp\dump.txt";
            ctx.GetPattern(method).DumpMode = MethodRewritePattern.PrintModeEnum.Original | MethodRewritePattern.PrintModeEnum.Patched;
            _patchContext.Commit();
            
            Assert.Equal("TEST", TestAsyncMethod().Result);
            _patchContext.FreeContext(ctx);
            _patchContext.Commit();
        }
        
        private async Task<string> TestAsyncMethod()
        {
            var first = await Task.Run(() => "TE");
            var last = await Task.Run(() => "ST");
            return await Task.Run(() => first + last);
        }

        public class TestBootstrap
        {
            public bool HasPrefix => _prefixMethod != null;
            public bool HasTranspile => _transpileMethod != null;
            public bool HasSuffix => _suffixMethod != null;

            private readonly MethodInfo _prefixMethod, _prefixAssert;
            private readonly MethodInfo _suffixMethod, _suffixAssert;
            private readonly MethodInfo _transpileMethod, _transpileAssert;
            private readonly MethodInfo _targetMethod, _targetAssert;
            private readonly MethodInfo _resetMethod;
            private readonly object _instance;
            private readonly object[] _targetParams;
            private readonly Type _type;

            public TestBootstrap(Type t)
            {
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
                _type = t;
                _prefixMethod = t.GetMethod("Prefix", flags);
                _prefixAssert = t.GetMethod("AssertPrefix", flags);
                _suffixMethod = t.GetMethod("Suffix", flags);
                _suffixAssert = t.GetMethod("AssertSuffix", flags);
                _transpileMethod = t.GetMethod("Transpile", flags);
                _transpileAssert = t.GetMethod("AssertTranspile", flags);
                _targetMethod = t.GetMethod("Target", flags);
                _targetAssert = t.GetMethod("AssertNormal", flags);
                _resetMethod = t.GetMethod("Reset", flags);
                if (_targetMethod == null)
                    throw new Exception($"{t.FullName} must have a method named Target");
                if (_targetAssert == null)
                    throw new Exception($"{t.FullName} must have a method named AssertNormal");
                _instance = !_targetMethod.IsStatic ? Activator.CreateInstance(t) : null;
                _targetParams = (object[]) t.GetField("_targetParams", flags)?.GetValue(null) ?? new object[0];
            }

            private void Invoke(MethodBase i, params object[] args)
            {
                if (i == null) return;
                i.Invoke(i.IsStatic ? null : _instance, args);
            }

            private void Invoke()
            {
                _targetMethod.Invoke(_instance, _targetParams);
                Invoke(_targetAssert);
            }

            public void TestPrefix()
            {
                Invoke(_resetMethod);
                PatchContext context = _patchContext.AcquireContext();
                context.GetPattern(_targetMethod).Prefixes.Add(_prefixMethod);
                _patchContext.Commit();

                Invoke();
                Invoke(_prefixAssert);

                _patchContext.FreeContext(context);
                _patchContext.Commit();
            }

            public void TestSuffix()
            {
                Invoke(_resetMethod);
                PatchContext context = _patchContext.AcquireContext();
                context.GetPattern(_targetMethod).Suffixes.Add(_suffixMethod);
                _patchContext.Commit();

                Invoke();
                Invoke(_suffixAssert);

                _patchContext.FreeContext(context);
                _patchContext.Commit();
            }

            public void TestTranspile()
            {
                Invoke(_resetMethod);
                PatchContext context = _patchContext.AcquireContext();
                context.GetPattern(_targetMethod).Transpilers.Add(_transpileMethod);
                _patchContext.Commit();

                Invoke();
                Invoke(_transpileAssert);

                _patchContext.FreeContext(context);
                _patchContext.Commit();
            }

            public void TestCombo()
            {
                Invoke(_resetMethod);
                PatchContext context = _patchContext.AcquireContext();
                if (_prefixMethod != null)
                    context.GetPattern(_targetMethod).Prefixes.Add(_prefixMethod);
                if (_transpileMethod != null)
                    context.GetPattern(_targetMethod).Transpilers.Add(_transpileMethod);
                if (_suffixMethod != null)
                    context.GetPattern(_targetMethod).Suffixes.Add(_suffixMethod);
                _patchContext.Commit();

                Invoke();
                Invoke(_prefixAssert);
                Invoke(_transpileAssert);
                Invoke(_suffixAssert);

                _patchContext.FreeContext(context);
                _patchContext.Commit();
            }

            public override string ToString()
            {
                return _type.Name;
            }
        }

        private class PatchTestAttribute : Attribute
        {
        }

        private static readonly List<TestBootstrap> _patchTest;

        static PatchTest()
        {
            TestUtils.Init();
            foreach (Type type in typeof(PatchManager).Assembly.GetTypes())
                if (type.Namespace?.StartsWith(typeof(PatchManager).Namespace ?? "") ?? false)
                    ReflectedManager.Process(type);

            _patchTest = new List<TestBootstrap>();
            foreach (Type type in typeof(PatchTest).GetNestedTypes(BindingFlags.NonPublic))
                if (type.GetCustomAttribute(typeof(PatchTestAttribute)) != null)
                    _patchTest.Add(new TestBootstrap(type));
        }

        public static IEnumerable<object[]> Prefixes => _patchTest.Where(x => x.HasPrefix).Select(x => new object[] {x});
        public static IEnumerable<object[]> Transpilers => _patchTest.Where(x => x.HasTranspile).Select(x => new object[] {x});
        public static IEnumerable<object[]> Suffixes => _patchTest.Where(x => x.HasSuffix).Select(x => new object[] {x});
        public static IEnumerable<object[]> Combo => _patchTest.Where(x => x.HasPrefix || x.HasTranspile || x.HasSuffix).Select(x => new object[] {x});

        #endregion

        #region PatchTests

        [PatchTest]
        private class StaticNoRetNoParm
        {
            private static bool _prefixHit, _normalHit, _suffixHit, _transpileHit;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Prefix()
            {
                _prefixHit = true;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Target()
            {
                _normalHit = true;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Suffix()
            {
                _suffixHit = true;
            }

            public static IEnumerable<MsilInstruction> Transpile(IEnumerable<MsilInstruction> instructions)
            {
                yield return new MsilInstruction(OpCodes.Ldnull);
                yield return new MsilInstruction(OpCodes.Ldc_I4_1);
                yield return new MsilInstruction(OpCodes.Stfld).InlineValue(typeof(StaticNoRetNoParm).GetField("_transpileHit",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                foreach (MsilInstruction i in instructions)
                    yield return i;
            }

            public static void Reset()
            {
                _prefixHit = _normalHit = _suffixHit = _transpileHit = false;
            }

            public static void AssertTranspile()
            {
                Assert.True(_transpileHit, "Failed to transpile");
            }

            public static void AssertSuffix()
            {
                Assert.True(_suffixHit, "Failed to suffix");
            }

            public static void AssertNormal()
            {
                Assert.True(_normalHit, "Failed to execute normally");
            }

            public static void AssertPrefix()
            {
                Assert.True(_prefixHit, "Failed to prefix");
            }
        }

        [PatchTest]
        private class StaticNoRetParam
        {
            private static bool _prefixHit, _normalHit, _suffixHit;
            private static readonly object[] _targetParams = {"test", 1, new StringBuilder("test1")};

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Prefix(string str, int i, StringBuilder o)
            {
                Assert.Equal(_targetParams[0], str);
                Assert.Equal(_targetParams[1], i);
                Assert.Equal(_targetParams[2], o);
                _prefixHit = true;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Target(string str, int i, StringBuilder o)
            {
                _normalHit = true;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Suffix(string str, int i, StringBuilder o)
            {
                Assert.Equal(_targetParams[0], str);
                Assert.Equal(_targetParams[1], i);
                Assert.Equal(_targetParams[2], o);
                _suffixHit = true;
            }

            public static void Reset()
            {
                _prefixHit = _normalHit = _suffixHit = false;
            }

            public static void AssertSuffix()
            {
                Assert.True(_suffixHit, "Failed to suffix");
            }

            public static void AssertNormal()
            {
                Assert.True(_normalHit, "Failed to execute normally");
            }

            public static void AssertPrefix()
            {
                Assert.True(_prefixHit, "Failed to prefix");
            }
        }

        [PatchTest]
        private class StaticNoRetParamReplace
        {
            private static bool _prefixHit, _normalHit, _suffixHit;
            private static readonly object[] _targetParams = {"test", 1, new StringBuilder("stest1")};
            private static readonly object[] _replacedParams = {"test2", 2, new StringBuilder("stest2")};
            private static object[] _calledParams;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Prefix(ref string str, ref int i, ref StringBuilder o)
            {
                Assert.Equal(_targetParams[0], str);
                Assert.Equal(_targetParams[1], i);
                Assert.Equal(_targetParams[2], o);
                str = (string) _replacedParams[0];
                i = (int) _replacedParams[1];
                o = (StringBuilder) _replacedParams[2];
                _prefixHit = true;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Target(string str, int i, StringBuilder o)
            {
                _calledParams = new object[] {str, i, o};
                _normalHit = true;
            }

            public static void Reset()
            {
                _prefixHit = _normalHit = _suffixHit = false;
            }

            public static void AssertNormal()
            {
                Assert.True(_normalHit, "Failed to execute normally");
            }

            public static void AssertPrefix()
            {
                Assert.True(_prefixHit, "Failed to prefix");
                for (var i = 0; i < 3; i++)
                    Assert.Equal(_replacedParams[i], _calledParams[i]);
            }
        }

        [PatchTest]
        private class StaticCancelExec
        {
            private static bool _prefixHit, _normalHit, _suffixHit;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static bool Prefix()
            {
                _prefixHit = true;
                return false;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Target()
            {
                _normalHit = true;
            }

            public static void Reset()
            {
                _prefixHit = _normalHit = _suffixHit = false;
            }

            public static void AssertNormal()
            {
                Assert.False(_normalHit, "Executed normally when canceled");
            }

            public static void AssertPrefix()
            {
                Assert.True(_prefixHit, "Failed to prefix");
            }
        }

        #endregion
    }
#pragma warning restore 414
}