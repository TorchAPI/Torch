using System;
using System.Collections.Generic;
using Torch.Client;
using Torch.Tests;
using Torch.Utils;
using Xunit;

namespace Torch.Client.Tests
{
    public class TorchClientReflectionTest
    {
        static TorchClientReflectionTest()
        {
            TestUtils.Init();
        }

        private static ReflectionTestManager _manager;

        private static ReflectionTestManager Manager()
        {
            if (_manager != null)
                return _manager;

            return _manager = new ReflectionTestManager().Init(typeof(TorchClient).Assembly);
        }

        public static IEnumerable<object[]> Getters => Manager().Getters;

        public static IEnumerable<object[]> Setters => Manager().Setters;

        public static IEnumerable<object[]> Invokers => Manager().Invokers;

        public static IEnumerable<object[]> MemberInfo => Manager().MemberInfo;

        public static IEnumerable<object[]> Events => Manager().Events;

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

        [Theory]
        [MemberData(nameof(MemberInfo))]
        public void TestBindingMemberInfo(ReflectionTestManager.FieldRef field)
        {
            if (field.Field == null)
                return;
            Assert.True(ReflectedManager.Process(field.Field));
            if (field.Field.IsStatic)
                Assert.NotNull(field.Field.GetValue(null));
        }

        [Theory]
        [MemberData(nameof(Events))]
        public void TestBindingEvents(ReflectionTestManager.FieldRef field)
        {
            if (field.Field == null)
                return;
            Assert.True(ReflectedManager.Process(field.Field));
            if (field.Field.IsStatic)
                ((Func<ReflectedEventReplacer>)field.Field.GetValue(null)).Invoke();
        }
        #endregion
    }
}
