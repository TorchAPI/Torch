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
        private readonly HashSet<object[]> _memberInfo = new HashSet<object[]>();
        private readonly HashSet<object[]> _events = new HashSet<object[]>();

        public ReflectionTestManager()
        {
            _getters.Add(new object[] { new FieldRef(null) });
            _setters.Add(new object[] { new FieldRef(null) });
            _invokers.Add(new object[] { new FieldRef(null) });
            _memberInfo.Add(new object[] {new FieldRef(null)});
            _events.Add(new object[] {new FieldRef(null)});
        }

        public ReflectionTestManager Init(Assembly asm)
        {
            try
            {
                foreach (Type type in asm.GetTypes())
                    Init(type);
            }
            catch (ReflectionTypeLoadException e)
            {
                throw e.LoaderExceptions[0];
            }
            return this;
        }

        public ReflectionTestManager Init(Type type)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Static |
                                                           BindingFlags.Instance |
                                                           BindingFlags.Public |
                                                           BindingFlags.NonPublic))
            {
                var args = new object[] { new FieldRef(field) };
                foreach (ReflectedMemberAttribute attr in field.GetCustomAttributes<ReflectedMemberAttribute>())
                {
                    if (!field.IsStatic)
                        throw new ArgumentException("Field must be static to be reflected");
                    switch (attr)
                    {
                        case ReflectedMethodAttribute rma:
                            _invokers.Add(args);
                            break;
                        case ReflectedGetterAttribute rga:
                            _getters.Add(args);
                            break;
                        case ReflectedSetterAttribute rsa:
                            _setters.Add(args);
                            break;
                        case ReflectedFieldInfoAttribute rfia:
                        case ReflectedPropertyInfoAttribute rpia:
                        case ReflectedMethodInfoAttribute rmia:
                            _memberInfo.Add(args);
                            break;
                    }
                }
                var reflectedEventReplacer = field.GetCustomAttribute<ReflectedEventReplaceAttribute>();
                if (reflectedEventReplacer != null)
                {
                    if (!field.IsStatic)
                        throw new ArgumentException("Field must be static to be reflected");
                    _events.Add(args);
                }
            }
            return this;
        }

        public IEnumerable<object[]> Getters => _getters;

        public IEnumerable<object[]> Setters => _setters;

        public IEnumerable<object[]> Invokers => _invokers;

        public IEnumerable<object[]> MemberInfo => _memberInfo;

        public IEnumerable<object[]> Events => _events;

        #endregion

    }
}
