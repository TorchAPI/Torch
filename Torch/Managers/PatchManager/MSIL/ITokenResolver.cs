using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Torch.Managers.PatchManager.MSIL
{
    //https://stackoverflow.com/questions/4148297/resolving-the-tokens-found-in-the-il-from-a-dynamic-method/35711376#35711376
    internal interface ITokenResolver
    {
        MemberInfo ResolveMember(int token);
        Type ResolveType(int token);
        FieldInfo ResolveField(int token);
        MethodBase ResolveMethod(int token);
        byte[] ResolveSignature(int token);
        string ResolveString(int token);
    }

    internal sealed class NormalTokenResolver : ITokenResolver
    {
        private readonly Type[] _genericTypeArgs, _genericMethArgs;
        private readonly Module _module;

        internal NormalTokenResolver(MethodBase method)
        {
            _module = method.Module;
            _genericTypeArgs = method.DeclaringType?.GenericTypeArguments ?? new Type[0];
            _genericMethArgs = (method is MethodInfo ? method.GetGenericArguments() : new Type[0]);
        }

        public MemberInfo ResolveMember(int token)
        {
            return _module.ResolveMember(token, _genericTypeArgs, _genericMethArgs);
        }

        public Type ResolveType(int token)
        {
            return _module.ResolveType(token, _genericTypeArgs, _genericMethArgs);
        }

        public FieldInfo ResolveField(int token)
        {
            return _module.ResolveField(token, _genericTypeArgs, _genericMethArgs);
        }

        public MethodBase ResolveMethod(int token)
        {
            return _module.ResolveMethod(token, _genericTypeArgs, _genericMethArgs);
        }

        public byte[] ResolveSignature(int token)
        {
            return _module.ResolveSignature(token);
        }

        public string ResolveString(int token)
        {
            return _module.ResolveString(token);
        }
    }

    internal sealed class NullTokenResolver : ITokenResolver
    {
        internal static readonly NullTokenResolver Instance = new NullTokenResolver();

        internal NullTokenResolver()
        {
        }

        public MemberInfo ResolveMember(int token)
        {
            return null;
        }

        public Type ResolveType(int token)
        {
            return null;
        }

        public FieldInfo ResolveField(int token)
        {
            return null;
        }

        public MethodBase ResolveMethod(int token)
        {
            return null;
        }

        public byte[] ResolveSignature(int token)
        {
            return null;
        }

        public string ResolveString(int token)
        {
            return null;
        }
    }

    internal sealed class DynamicMethodTokenResolver : ITokenResolver
    {
        private readonly MethodInfo _getFieldInfo;
        private readonly MethodInfo _getMethodBase;
        private readonly GetTypeFromHandleUnsafe _getTypeFromHandleUnsafe;
        private readonly ConstructorInfo _runtimeFieldHandleStubCtor;
        private readonly ConstructorInfo _runtimeMethodHandleInternalCtor;
        private readonly SignatureResolver _signatureResolver;
        private readonly StringResolver _stringResolver;

        private readonly TokenResolver _tokenResolver;

        public DynamicMethodTokenResolver(DynamicMethod dynamicMethod)
        {
            object resolver = typeof(DynamicMethod)
                .GetField("m_resolver", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(dynamicMethod);
            if (resolver == null) throw new ArgumentException("The dynamic method's IL has not been finalized.");

            _tokenResolver = (TokenResolver) resolver.GetType()
                .GetMethod("ResolveToken", BindingFlags.Instance | BindingFlags.NonPublic)
                .CreateDelegate(typeof(TokenResolver), resolver);
            _stringResolver = (StringResolver) resolver.GetType()
                .GetMethod("GetStringLiteral", BindingFlags.Instance | BindingFlags.NonPublic)
                .CreateDelegate(typeof(StringResolver), resolver);
            _signatureResolver = (SignatureResolver) resolver.GetType()
                .GetMethod("ResolveSignature", BindingFlags.Instance | BindingFlags.NonPublic)
                .CreateDelegate(typeof(SignatureResolver), resolver);

            _getTypeFromHandleUnsafe = (GetTypeFromHandleUnsafe) typeof(Type)
                .GetMethod("GetTypeFromHandleUnsafe", BindingFlags.Static | BindingFlags.NonPublic, null,
                    new[] {typeof(IntPtr)}, null).CreateDelegate(typeof(GetTypeFromHandleUnsafe), null);
            Type runtimeType = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeType");

            Type runtimeMethodHandleInternal =
                typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeMethodHandleInternal");
            _getMethodBase = runtimeType.GetMethod("GetMethodBase", BindingFlags.Static | BindingFlags.NonPublic, null,
                new[] {runtimeType, runtimeMethodHandleInternal}, null);
            _runtimeMethodHandleInternalCtor =
                runtimeMethodHandleInternal.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] {typeof(IntPtr)}, null);

            Type runtimeFieldInfoStub = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeFieldInfoStub");
            _runtimeFieldHandleStubCtor =
                runtimeFieldInfoStub.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null,
                    new[] {typeof(IntPtr), typeof(object)}, null);
            _getFieldInfo = runtimeType.GetMethod("GetFieldInfo", BindingFlags.Static | BindingFlags.NonPublic, null,
                new[] {runtimeType, typeof(RuntimeTypeHandle).Assembly.GetType("System.IRuntimeFieldInfo")}, null);
        }

        public Type ResolveType(int token)
        {
            IntPtr typeHandle, methodHandle, fieldHandle;
            _tokenResolver.Invoke(token, out typeHandle, out methodHandle, out fieldHandle);

            return _getTypeFromHandleUnsafe.Invoke(typeHandle);
        }

        public MethodBase ResolveMethod(int token)
        {
            IntPtr typeHandle, methodHandle, fieldHandle;
            _tokenResolver.Invoke(token, out typeHandle, out methodHandle, out fieldHandle);

            return (MethodBase) _getMethodBase.Invoke(null, new[]
            {
                typeHandle == IntPtr.Zero ? null : _getTypeFromHandleUnsafe.Invoke(typeHandle),
                _runtimeMethodHandleInternalCtor.Invoke(new object[] {methodHandle})
            });
        }

        public FieldInfo ResolveField(int token)
        {
            IntPtr typeHandle, methodHandle, fieldHandle;
            _tokenResolver.Invoke(token, out typeHandle, out methodHandle, out fieldHandle);

            return (FieldInfo) _getFieldInfo.Invoke(null, new[]
            {
                typeHandle == IntPtr.Zero ? null : _getTypeFromHandleUnsafe.Invoke(typeHandle),
                _runtimeFieldHandleStubCtor.Invoke(new object[] {fieldHandle, null})
            });
        }

        public MemberInfo ResolveMember(int token)
        {
            IntPtr typeHandle, methodHandle, fieldHandle;
            _tokenResolver.Invoke(token, out typeHandle, out methodHandle, out fieldHandle);

            if (methodHandle != IntPtr.Zero)
                return (MethodBase) _getMethodBase.Invoke(null, new[]
                {
                    typeHandle == IntPtr.Zero ? null : _getTypeFromHandleUnsafe.Invoke(typeHandle),
                    _runtimeMethodHandleInternalCtor.Invoke(new object[] {methodHandle})
                });

            if (fieldHandle != IntPtr.Zero)
                return (FieldInfo) _getFieldInfo.Invoke(null, new[]
                {
                    typeHandle == IntPtr.Zero ? null : _getTypeFromHandleUnsafe.Invoke(typeHandle),
                    _runtimeFieldHandleStubCtor.Invoke(new object[] {fieldHandle, null})
                });

            if (typeHandle != IntPtr.Zero)
                return _getTypeFromHandleUnsafe.Invoke(typeHandle);

            throw new NotImplementedException(
                "DynamicMethods are not able to reference members by token other than types, methods and fields.");
        }

        public byte[] ResolveSignature(int token)
        {
            return _signatureResolver.Invoke(token, 0);
        }

        public string ResolveString(int token)
        {
            return _stringResolver.Invoke(token);
        }

        private delegate void TokenResolver(int token, out IntPtr typeHandle, out IntPtr methodHandle,
            out IntPtr fieldHandle);

        private delegate string StringResolver(int token);

        private delegate byte[] SignatureResolver(int token, int fromMethod);

        private delegate Type GetTypeFromHandleUnsafe(IntPtr handle);
    }
}