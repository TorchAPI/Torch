using System;
using System.Collections.Generic;
using System.Reflection;
using NLog;
using Sandbox.Engine.Multiplayer;
using VRage.Network;
using VRageMath;

namespace Torch.Managers
{
    public static class NetworkManager
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();
        
        #region Network Injection

        private static Dictionary<MethodInfo, Delegate> _delegateCache = new Dictionary<MethodInfo, Delegate>();

        private static Func<T, TA> GetDelegate<T, TA>(MethodInfo method) where TA : class
        {
            if (!_delegateCache.TryGetValue(method, out var del))
            {
                del = (Func<T, TA>)(x => Delegate.CreateDelegate(typeof(TA), x, method) as TA);
                _delegateCache[method] = del;
            }

            return (Func<T, TA>)del;
        }

        public static void RaiseEvent<T1>(T1 instance, MethodInfo method, EndpointId target = default(EndpointId)) where T1 : IMyEventOwner
        {
            var del = GetDelegate<T1, Action>(method);

            MyMultiplayer.RaiseEvent(instance, del, target);
        }

        public static void RaiseEvent<T1, T2>(T1 instance, MethodInfo method, T2 arg1, EndpointId target = default(EndpointId)) where T1 : IMyEventOwner
        {
            var del = GetDelegate<T1, Action<T2>> (method);

            MyMultiplayer.RaiseEvent(instance, del, arg1, target);
        }

        public static void RaiseEvent<T1, T2, T3>(T1 instance, MethodInfo method, T2 arg1, T3 arg2, EndpointId target = default(EndpointId)) where T1 : IMyEventOwner
        {
            var del = GetDelegate<T1, Action<T2, T3>>(method);

            MyMultiplayer.RaiseEvent(instance, del, arg1, arg2, target);
        }

        public static void RaiseEvent<T1, T2, T3, T4>(T1 instance, MethodInfo method, T2 arg1, T3 arg2, T4 arg3, EndpointId target = default(EndpointId)) where T1 : IMyEventOwner
        {
            var del = GetDelegate<T1, Action<T2, T3, T4>>(method);

            MyMultiplayer.RaiseEvent(instance, del, arg1, arg2, arg3, target);
        }

        public static void RaiseEvent<T1, T2, T3, T4, T5>(T1 instance, MethodInfo method, T2 arg1, T3 arg2, T4 arg3, T5 arg4, EndpointId target = default(EndpointId)) where T1 : IMyEventOwner
        {
            var del = GetDelegate<T1, Action<T2, T3, T4, T5>>(method);

            MyMultiplayer.RaiseEvent(instance, del, arg1, arg2, arg3, arg4, target);
        }

        public static void RaiseEvent<T1, T2, T3, T4, T5, T6>(T1 instance, MethodInfo method, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, EndpointId target = default(EndpointId)) where T1 : IMyEventOwner
        {
            var del = GetDelegate<T1, Action<T2, T3, T4, T5, T6>>(method);

            MyMultiplayer.RaiseEvent(instance, del, arg1, arg2, arg3, arg4, arg5, target);
        }

        public static void RaiseEvent<T1, T2, T3, T4, T5, T6, T7>(T1 instance, MethodInfo method, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6, EndpointId target = default(EndpointId)) where T1 : IMyEventOwner
        {
            var del = GetDelegate<T1, Action<T2, T3, T4, T5, T6, T7>>(method);

            MyMultiplayer.RaiseEvent(instance, del, arg1, arg2, arg3, arg4, arg5, arg6, target);
        }

        public static void RaiseStaticEvent(MethodInfo method, EndpointId target = default(EndpointId), Vector3D? position = null)
        {
            var del = GetDelegate<IMyEventOwner, Action>(method);

            MyMultiplayer.RaiseStaticEvent(del, target, position);
        }

        public static void RaiseStaticEvent<T1>(MethodInfo method, T1 arg1, EndpointId target = default(EndpointId), Vector3D? position = null)
        {
            var del = GetDelegate<IMyEventOwner, Action<T1>>(method);

            MyMultiplayer.RaiseStaticEvent(del, arg1, target, position);
        }

        public static void RaiseStaticEvent<T1, T2>(MethodInfo method, T1 arg1, T2 arg2, EndpointId target = default(EndpointId), Vector3D? position = null)
        {
            var del = GetDelegate<IMyEventOwner, Action<T1, T2>>(method);

            MyMultiplayer.RaiseStaticEvent(del, arg1, arg2, target, position);
        }
        
        public static void RaiseStaticEvent<T1, T2, T3>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3, EndpointId target = default(EndpointId), Vector3D? position = null)
        {
            var del = GetDelegate<IMyEventOwner, Action<T1, T2, T3>>(method);

            MyMultiplayer.RaiseStaticEvent(del, arg1, arg2, arg3, target, position);
        }

        public static void RaiseStaticEvent<T1, T2, T3, T4>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, EndpointId target = default(EndpointId), Vector3D? position = null)
        {
            var del = GetDelegate<IMyEventOwner, Action<T1, T2, T3, T4>>(method);

            MyMultiplayer.RaiseStaticEvent(del, arg1, arg2, arg3, arg4, target, position);
        }

        public static void RaiseStaticEvent<T1, T2, T3, T4, T5>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, EndpointId target = default(EndpointId), Vector3D? position = null)
        {
            var del = GetDelegate<IMyEventOwner, Action<T1, T2, T3, T4, T5>>(method);

            MyMultiplayer.RaiseStaticEvent(del, arg1, arg2, arg3, arg4, arg5, target, position);
        }

        public static void RaiseStaticEvent<T1, T2, T3, T4, T5, T6>(MethodInfo method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, EndpointId target = default(EndpointId), Vector3D? position = null)
        {
            var del = GetDelegate<IMyEventOwner, Action<T1, T2, T3, T4, T5, T6>>(method);

            MyMultiplayer.RaiseStaticEvent(del, arg1, arg2, arg3, arg4, arg5, arg6, target, position);
        }
        #endregion


    }
}
