using System;
using System.Collections.Generic;
using System.Reflection;
using Torch.Utils;

namespace Torch.Tests
{
    public class ReflectionTestManager
    {
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
                if (Field == null)
                    return "Ignored";
                return Field.DeclaringType?.FullName + "." + Field.Name;
            }
        }

        private readonly HashSet<object[]> _getters = new HashSet<object[]>();
        private readonly HashSet<object[]> _setters = new HashSet<object[]>();
        private readonly HashSet<object[]> _invokers = new HashSet<object[]>();

        public ReflectionTestManager()
        {
            _getters.Add(new object[] { new FieldRef(null) });
            _setters.Add(new object[] { new FieldRef(null) });
            _invokers.Add(new object[] { new FieldRef(null) });
        }

        public ReflectionTestManager Init(Assembly asm)
        {
            foreach (Type type in asm.GetTypes())
                Init(type);
            return this;
        }

        public ReflectionTestManager Init(Type type)
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
            return this;
        }

        public IEnumerable<object[]> Getters => _getters;

        public IEnumerable<object[]> Setters => _setters;

        public IEnumerable<object[]> Invokers => _invokers;

        #endregion

    }
}
