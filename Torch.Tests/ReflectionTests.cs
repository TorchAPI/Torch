using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.API;
using Torch.Client;
using Torch.Managers;
using Xunit;
using Xunit.Abstractions;

namespace Torch.Tests
{
    public class ReflectionTests
    {
        private static string GetGameBinaries()
        {
            string dir = Environment.CurrentDirectory;
            while (!string.IsNullOrWhiteSpace(dir))
            {
                string gameBin = Path.Combine(dir, "GameBinaries");
                if (Directory.Exists(gameBin))
                    return gameBin;

                dir = Path.GetDirectoryName(dir);
            }
            throw new Exception("GetGameBinaries failed to find a folder named GameBinaries in the directory tree");
        }

        private static readonly TorchAssemblyResolver _torchResolver =
            new TorchAssemblyResolver(GetGameBinaries());

        #region Binding
        [Theory]
        [MemberData(nameof(Getters))]
        public void TestBindingGetter(FieldRef field)
        {
            Assert.True(ReflectionManager.Process(field.Field));
            if (field.Field.IsStatic)
                Assert.NotNull(field.Field.GetValue(null));
        }

        [Theory]
        [MemberData(nameof(Setters))]
        public void TestBindingSetter(FieldRef field)
        {
            Assert.True(ReflectionManager.Process(field.Field));
            if (field.Field.IsStatic)
                Assert.NotNull(field.Field.GetValue(null));
        }

        [Theory]
        [MemberData(nameof(Invokers))]
        public void TestBindingInvoker(FieldRef field)
        {
            Assert.True(ReflectionManager.Process(field.Field));
            if (field.Field.IsStatic)
                Assert.NotNull(field.Field.GetValue(null));
        }
        #endregion

        #region Results
        #region Dummy
        private class ReflectionTestTarget
        {
            public int TestField;
            public int TestProperty { get; set; }

            /// <summary>
            /// Return true when greater or equal than 0
            /// </summary>
            public bool TestCall(int k)
            {
                return k >= 0;
            }

            public static int TestFieldStatic;
            public static int TestPropertyStatic { get; set; }

            /// <summary>
            /// Return true when greater or equal than 0
            /// </summary>
            public static bool TestCallStatic(int k)
            {
                return k >= 0;
            }
        }

        private class ReflectionTestBinding
        {
            [ReflectedGetter(Name = "TestField")]
            public static Func<ReflectionTestTarget, int> TestFieldGetter;
            [ReflectedSetter(Name = "TestField")]
            public static Action<ReflectionTestTarget, int> TestFieldSetter;

            [ReflectedGetter(Name = "TestProperty")]
            public static Func<ReflectionTestTarget, int> TestPropertyGetter;
            [ReflectedSetter(Name = "TestProperty")]
            public static Action<ReflectionTestTarget, int> TestPropertySetter;

            [ReflectedMethod]
            public static Func<ReflectionTestTarget, int, bool> TestCall;


            [ReflectedGetter(Name = "TestFieldStatic", Type = typeof(ReflectionTestTarget))]
            public static Func<int> TestStaticFieldGetter;
            [ReflectedSetter(Name = "TestFieldStatic", Type = typeof(ReflectionTestTarget))]
            public static Action<int> TestStaticFieldSetter;

            [ReflectedGetter(Name = "TestPropertyStatic", Type = typeof(ReflectionTestTarget))]
            public static Func<int> TestStaticPropertyGetter;
            [ReflectedSetter(Name = "TestPropertyStatic", Type = typeof(ReflectionTestTarget))]
            public static Action<int> TestStaticPropertySetter;

            [ReflectedStaticMethod(Type = typeof(ReflectionTestTarget))]
            public static Func<int, bool> TestCallStatic;
        }
        #endregion

        private readonly Random _rand = new Random();
        private int AcquireRandomNum()
        {
            return _rand.Next();
        }

        #region Instance
        [Fact]
        public void TestInstanceFieldGet()
        {
            ReflectionManager.Process(typeof(ReflectionTestBinding));
            int testNumber = AcquireRandomNum();
            var target = new ReflectionTestTarget
            {
                TestField = testNumber
            };
            Assert.Equal(testNumber, ReflectionTestBinding.TestFieldGetter.Invoke(target));
        }
        [Fact]
        public void TestInstanceFieldSet()
        {
            ReflectionManager.Process(typeof(ReflectionTestBinding));
            int testNumber = AcquireRandomNum();
            var target = new ReflectionTestTarget();
            ReflectionTestBinding.TestFieldSetter.Invoke(target, testNumber);
            Assert.Equal(testNumber, target.TestField);
        }

        [Fact]
        public void TestInstancePropertyGet()
        {
            ReflectionManager.Process(typeof(ReflectionTestBinding));
            int testNumber = AcquireRandomNum();
            var target = new ReflectionTestTarget
            {
                TestProperty = testNumber
            };
            Assert.Equal(testNumber, ReflectionTestBinding.TestPropertyGetter.Invoke(target));
        }

        [Fact]
        public void TestInstancePropertySet()
        {
            ReflectionManager.Process(typeof(ReflectionTestBinding));
            int testNumber = AcquireRandomNum();
            var target = new ReflectionTestTarget();
            ReflectionTestBinding.TestPropertySetter.Invoke(target, testNumber);
            Assert.Equal(testNumber, target.TestProperty);
        }

        [Fact]
        public void TestInstanceInvoke()
        {
            ReflectionManager.Process(typeof(ReflectionTestBinding));
            var target = new ReflectionTestTarget();
            Assert.True(ReflectionTestBinding.TestCall.Invoke(target, 1));
            Assert.False(ReflectionTestBinding.TestCall.Invoke(target, -1));
        }
        #endregion

        #region Static
        [Fact]
        public void TestStaticFieldGet()
        {
            ReflectionManager.Process(typeof(ReflectionTestBinding));
            int testNumber = AcquireRandomNum();
            ReflectionTestTarget.TestFieldStatic = testNumber;
            Assert.Equal(testNumber, ReflectionTestBinding.TestStaticFieldGetter.Invoke());
        }
        [Fact]
        public void TestStaticFieldSet()
        {
            ReflectionManager.Process(typeof(ReflectionTestBinding));
            int testNumber = AcquireRandomNum();
            ReflectionTestBinding.TestStaticFieldSetter.Invoke(testNumber);
            Assert.Equal(testNumber, ReflectionTestTarget.TestFieldStatic);
        }

        [Fact]
        public void TestStaticPropertyGet()
        {
            ReflectionManager.Process(typeof(ReflectionTestBinding));
            int testNumber = AcquireRandomNum();
            ReflectionTestTarget.TestPropertyStatic = testNumber;
            Assert.Equal(testNumber, ReflectionTestBinding.TestStaticPropertyGetter.Invoke());
        }

        [Fact]
        public void TestStaticPropertySet()
        {
            ReflectionManager.Process(typeof(ReflectionTestBinding));
            int testNumber = AcquireRandomNum();
            ReflectionTestBinding.TestStaticPropertySetter.Invoke(testNumber);
            Assert.Equal(testNumber, ReflectionTestTarget.TestPropertyStatic);
        }

        [Fact]
        public void TestStaticInvoke()
        {
            ReflectionManager.Process(typeof(ReflectionTestBinding));
            Assert.True(ReflectionTestBinding.TestCallStatic.Invoke(1));
            Assert.False(ReflectionTestBinding.TestCallStatic.Invoke(-1));
        }
        #endregion
        #endregion

        #region FieldProvider
        public struct FieldRef
        {
            public FieldInfo Field;

            public FieldRef(FieldInfo f)
            {
                Field = f;
            }

            public override string ToString()
            {
                return Field.DeclaringType?.FullName + "." + Field.Name;
            }
        }

        private static bool _init = false;
        private static HashSet<object[]> _getters, _setters, _invokers;

        private static void Init()
        {
            if (_init)
                return;
            _getters = new HashSet<object[]>();
            _setters = new HashSet<object[]>();
            _invokers = new HashSet<object[]>();

            foreach (Type type in typeof(TorchBase).Assembly.GetTypes())
                InternalInit(type);
            InternalInit(typeof(ReflectionTestBinding));

            _init = true;
        }

        private static void InternalInit(Type type)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Static |
                                                           BindingFlags.Instance |
                                                           BindingFlags.Public |
                                                           BindingFlags.NonPublic))
            {
                if (field.GetCustomAttribute<ReflectedMethodAttribute>() != null)
                    _invokers.Add(new object[] { new FieldRef(field) });
                if (field.GetCustomAttribute<ReflectedGetterAttribute>() != null)
                    _getters.Add(new object[] { new FieldRef(field) });
                if (field.GetCustomAttribute<ReflectedSetterAttribute>() != null)
                    _setters.Add(new object[] { new FieldRef(field) });
            }
        }

        public static IEnumerable<object[]> Getters
        {
            get
            {
                Init();
                return _getters;
            }
        }

        public static IEnumerable<object[]> Setters
        {
            get
            {
                Init();
                return _setters;
            }
        }

        public static IEnumerable<object[]> Invokers
        {
            get
            {
                Init();
                return _invokers;
            }
        }
        #endregion
    }
}
