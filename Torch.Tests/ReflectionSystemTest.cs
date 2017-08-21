using System;
using System.Collections.Generic;
using Torch.Utils;
using Xunit;

namespace Torch.Tests
{
    public class ReflectionSystemTest
    {
        static ReflectionSystemTest()
        {
            TestUtils.Init();
        }
        
        private static ReflectionTestManager _manager = new ReflectionTestManager().Init(typeof(ReflectionTestBinding));
        public static IEnumerable<object[]> Getters => _manager.Getters;

        public static IEnumerable<object[]> Setters => _manager.Setters;

        public static IEnumerable<object[]> Invokers => _manager.Invokers;

        #region Binding
        [Theory]
        [MemberData(nameof(Getters))]
        public void TestBindingGetter(ReflectionTestManager.FieldRef field)
        {
            if (field.Field == null)
                return;
            Assert.True(ReflectedManager.Process(field.Field));
            if (field.Field.IsStatic)
                Assert.NotNull(field.Field.GetValue(null));
        }

        [Theory]
        [MemberData(nameof(Setters))]
        public void TestBindingSetter(ReflectionTestManager.FieldRef field)
        {
            if (field.Field == null)
                return;
            Assert.True(ReflectedManager.Process(field.Field));
            if (field.Field.IsStatic)
                Assert.NotNull(field.Field.GetValue(null));
        }

        [Theory]
        [MemberData(nameof(Invokers))]
        public void TestBindingInvoker(ReflectionTestManager.FieldRef field)
        {
            if (field.Field == null)
                return;
            Assert.True(ReflectedManager.Process(field.Field));
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
            ReflectedManager.Process(typeof(ReflectionTestBinding));
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
            ReflectedManager.Process(typeof(ReflectionTestBinding));
            int testNumber = AcquireRandomNum();
            var target = new ReflectionTestTarget();
            ReflectionTestBinding.TestFieldSetter.Invoke(target, testNumber);
            Assert.Equal(testNumber, target.TestField);
        }

        [Fact]
        public void TestInstancePropertyGet()
        {
            ReflectedManager.Process(typeof(ReflectionTestBinding));
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
            ReflectedManager.Process(typeof(ReflectionTestBinding));
            int testNumber = AcquireRandomNum();
            var target = new ReflectionTestTarget();
            ReflectionTestBinding.TestPropertySetter.Invoke(target, testNumber);
            Assert.Equal(testNumber, target.TestProperty);
        }

        [Fact]
        public void TestInstanceInvoke()
        {
            ReflectedManager.Process(typeof(ReflectionTestBinding));
            var target = new ReflectionTestTarget();
            Assert.True(ReflectionTestBinding.TestCall.Invoke(target, 1));
            Assert.False(ReflectionTestBinding.TestCall.Invoke(target, -1));
        }
        #endregion

        #region Static
        [Fact]
        public void TestStaticFieldGet()
        {
            ReflectedManager.Process(typeof(ReflectionTestBinding));
            int testNumber = AcquireRandomNum();
            ReflectionTestTarget.TestFieldStatic = testNumber;
            Assert.Equal(testNumber, ReflectionTestBinding.TestStaticFieldGetter.Invoke());
        }
        [Fact]
        public void TestStaticFieldSet()
        {
            ReflectedManager.Process(typeof(ReflectionTestBinding));
            int testNumber = AcquireRandomNum();
            ReflectionTestBinding.TestStaticFieldSetter.Invoke(testNumber);
            Assert.Equal(testNumber, ReflectionTestTarget.TestFieldStatic);
        }

        [Fact]
        public void TestStaticPropertyGet()
        {
            ReflectedManager.Process(typeof(ReflectionTestBinding));
            int testNumber = AcquireRandomNum();
            ReflectionTestTarget.TestPropertyStatic = testNumber;
            Assert.Equal(testNumber, ReflectionTestBinding.TestStaticPropertyGetter.Invoke());
        }

        [Fact]
        public void TestStaticPropertySet()
        {
            ReflectedManager.Process(typeof(ReflectionTestBinding));
            int testNumber = AcquireRandomNum();
            ReflectionTestBinding.TestStaticPropertySetter.Invoke(testNumber);
            Assert.Equal(testNumber, ReflectionTestTarget.TestPropertyStatic);
        }

        [Fact]
        public void TestStaticInvoke()
        {
            ReflectedManager.Process(typeof(ReflectionTestBinding));
            Assert.True(ReflectionTestBinding.TestCallStatic.Invoke(1));
            Assert.False(ReflectionTestBinding.TestCallStatic.Invoke(-1));
        }
        #endregion
        #endregion
 }
}
