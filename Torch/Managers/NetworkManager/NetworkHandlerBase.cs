using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Serialization;

namespace Torch.Managers
{
    public abstract class NetworkHandlerBase
    {
        /// <summary>
        /// Check the method name and do unit tests on parameters in here.
        /// </summary>
        /// <param name="site"></param>
        /// <returns></returns>
        public abstract bool CanHandle(CallSite site);

        /// <summary>
        /// Performs action on network packet. Return value of true means the packet has been handled, and will not be passed on to the game server.
        /// </summary>
        /// <param name="remoteUserId"></param>
        /// <param name="site"></param>
        /// <param name="stream"></param>
        /// <param name="obj"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public abstract bool Handle(ulong remoteUserId, CallSite site, BitStream stream, object obj, MyPacket packet);
        
        /// <summary>
        /// Extracts method arguments from the bitstream or packs them back in, depending on stream read mode.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="info"></param>
        /// <param name="stream"></param>
        /// <param name="arg1"></param>
        public void Serialize<T1>(MethodInfo info, BitStream stream, ref T1 arg1)
        {
            var s1 = MyFactory.GetSerializer<T1>();

            var args = info.GetParameters();
            var info1 = MySerializeInfo.CreateForParameter(args, 0);

            if (stream.Reading)
            {
                MySerializationHelpers.CreateAndRead(stream, out arg1, s1, info1);
            }
            else
            {
                MySerializationHelpers.Write(stream, ref arg1, s1, info1);
            }
        }

        public void Serialize<T1, T2>(MethodInfo info, BitStream stream, ref T1 arg1, ref T2 arg2)
        {
            var s1 = MyFactory.GetSerializer<T1>();
            var s2 = MyFactory.GetSerializer<T2>();

            var args = info.GetParameters();
            var info1 = MySerializeInfo.CreateForParameter(args, 0);
            var info2 = MySerializeInfo.CreateForParameter(args, 1);
           
            if (stream.Reading)
            {
                MySerializationHelpers.CreateAndRead(stream, out arg1, s1, info1);
                MySerializationHelpers.CreateAndRead(stream, out arg2, s2, info2);
            }
            else
            {
                MySerializationHelpers.Write(stream, ref arg1, s1, info1);
                MySerializationHelpers.Write(stream, ref arg2, s2, info2);
            }
        }

        public void Serialize<T1, T2, T3>(MethodInfo info, BitStream stream, ref T1 arg1, ref T2 arg2, ref T3 arg3)
        {
            var s1 = MyFactory.GetSerializer<T1>();
            var s2 = MyFactory.GetSerializer<T2>();
            var s3 = MyFactory.GetSerializer<T3>();

            var args = info.GetParameters();
            var info1 = MySerializeInfo.CreateForParameter(args, 0);
            var info2 = MySerializeInfo.CreateForParameter(args, 1);
            var info3 = MySerializeInfo.CreateForParameter(args, 2);

            if (stream.Reading)
            {
                MySerializationHelpers.CreateAndRead(stream, out arg1, s1, info1);
                MySerializationHelpers.CreateAndRead(stream, out arg2, s2, info2);
                MySerializationHelpers.CreateAndRead(stream, out arg3, s3, info3);
            }
            else
            {
                MySerializationHelpers.Write(stream, ref arg1, s1, info1);
                MySerializationHelpers.Write(stream, ref arg2, s2, info2);
                MySerializationHelpers.Write(stream, ref arg3, s3, info3);
            }
        }

        public void Serialize<T1, T2, T3, T4>(MethodInfo info, BitStream stream, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4)
        {
            var s1 = MyFactory.GetSerializer<T1>();
            var s2 = MyFactory.GetSerializer<T2>();
            var s3 = MyFactory.GetSerializer<T3>();
            var s4 = MyFactory.GetSerializer<T4>();

            var args = info.GetParameters();
            var info1 = MySerializeInfo.CreateForParameter(args, 0);
            var info2 = MySerializeInfo.CreateForParameter(args, 1);
            var info3 = MySerializeInfo.CreateForParameter(args, 2);
            var info4 = MySerializeInfo.CreateForParameter(args, 3);

            if (stream.Reading)
            {
                MySerializationHelpers.CreateAndRead(stream, out arg1, s1, info1);
                MySerializationHelpers.CreateAndRead(stream, out arg2, s2, info2);
                MySerializationHelpers.CreateAndRead(stream, out arg3, s3, info3);
                MySerializationHelpers.CreateAndRead(stream, out arg4, s4, info4);
            }
            else
            {
                MySerializationHelpers.Write(stream, ref arg1, s1, info1);
                MySerializationHelpers.Write(stream, ref arg2, s2, info2);
                MySerializationHelpers.Write(stream, ref arg3, s3, info3);
                MySerializationHelpers.Write(stream, ref arg4, s4, info4);
            }
        }

        public void Serialize<T1, T2, T3, T4, T5>(MethodInfo info, BitStream stream, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5)
        {
            var s1 = MyFactory.GetSerializer<T1>();
            var s2 = MyFactory.GetSerializer<T2>();
            var s3 = MyFactory.GetSerializer<T3>();
            var s4 = MyFactory.GetSerializer<T4>();
            var s5 = MyFactory.GetSerializer<T5>();

            var args = info.GetParameters();
            var info1 = MySerializeInfo.CreateForParameter(args, 0);
            var info2 = MySerializeInfo.CreateForParameter(args, 1);
            var info3 = MySerializeInfo.CreateForParameter(args, 2);
            var info4 = MySerializeInfo.CreateForParameter(args, 3);
            var info5 = MySerializeInfo.CreateForParameter(args, 4);

            if (stream.Reading)
            {
                MySerializationHelpers.CreateAndRead(stream, out arg1, s1, info1);
                MySerializationHelpers.CreateAndRead(stream, out arg2, s2, info2);
                MySerializationHelpers.CreateAndRead(stream, out arg3, s3, info3);
                MySerializationHelpers.CreateAndRead(stream, out arg4, s4, info4);
                MySerializationHelpers.CreateAndRead(stream, out arg5, s5, info5);
            }
            else
            {
                MySerializationHelpers.Write(stream, ref arg1, s1, info1);
                MySerializationHelpers.Write(stream, ref arg2, s2, info2);
                MySerializationHelpers.Write(stream, ref arg3, s3, info3);
                MySerializationHelpers.Write(stream, ref arg4, s4, info4);
                MySerializationHelpers.Write(stream, ref arg5, s5, info5);
            }
        }

        public void Serialize<T1, T2, T3, T4, T5, T6>(MethodInfo info, BitStream stream, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6)
        {
            var s1 = MyFactory.GetSerializer<T1>();
            var s2 = MyFactory.GetSerializer<T2>();
            var s3 = MyFactory.GetSerializer<T3>();
            var s4 = MyFactory.GetSerializer<T4>();
            var s5 = MyFactory.GetSerializer<T5>();
            var s6 = MyFactory.GetSerializer<T6>();

            var args = info.GetParameters();
            var info1 = MySerializeInfo.CreateForParameter(args, 0);
            var info2 = MySerializeInfo.CreateForParameter(args, 1);
            var info3 = MySerializeInfo.CreateForParameter(args, 2);
            var info4 = MySerializeInfo.CreateForParameter(args, 3);
            var info5 = MySerializeInfo.CreateForParameter(args, 4);
            var info6 = MySerializeInfo.CreateForParameter(args, 5);

            if (stream.Reading)
            {
                MySerializationHelpers.CreateAndRead(stream, out arg1, s1, info1);
                MySerializationHelpers.CreateAndRead(stream, out arg2, s2, info2);
                MySerializationHelpers.CreateAndRead(stream, out arg3, s3, info3);
                MySerializationHelpers.CreateAndRead(stream, out arg4, s4, info4);
                MySerializationHelpers.CreateAndRead(stream, out arg5, s5, info5);
                MySerializationHelpers.CreateAndRead(stream, out arg6, s6, info6);
            }
            else
            {
                MySerializationHelpers.Write(stream, ref arg1, s1, info1);
                MySerializationHelpers.Write(stream, ref arg2, s2, info2);
                MySerializationHelpers.Write(stream, ref arg3, s3, info3);
                MySerializationHelpers.Write(stream, ref arg4, s4, info4);
                MySerializationHelpers.Write(stream, ref arg5, s5, info5);
                MySerializationHelpers.Write(stream, ref arg6, s6, info6);
            }
        }

        public void Serialize<T1, T2, T3, T4, T5, T6, T7>(MethodInfo info, BitStream stream, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7)
        {
            var s1 = MyFactory.GetSerializer<T1>();
            var s2 = MyFactory.GetSerializer<T2>();
            var s3 = MyFactory.GetSerializer<T3>();
            var s4 = MyFactory.GetSerializer<T4>();
            var s5 = MyFactory.GetSerializer<T5>();
            var s6 = MyFactory.GetSerializer<T6>();
            var s7 = MyFactory.GetSerializer<T7>();

            var args = info.GetParameters();
            var info1 = MySerializeInfo.CreateForParameter(args, 0);
            var info2 = MySerializeInfo.CreateForParameter(args, 1);
            var info3 = MySerializeInfo.CreateForParameter(args, 2);
            var info4 = MySerializeInfo.CreateForParameter(args, 3);
            var info5 = MySerializeInfo.CreateForParameter(args, 4);
            var info6 = MySerializeInfo.CreateForParameter(args, 5);
            var info7 = MySerializeInfo.CreateForParameter(args, 6);

            if ( stream.Reading )
            {
                MySerializationHelpers.CreateAndRead(stream, out arg1, s1, info1);
                MySerializationHelpers.CreateAndRead(stream, out arg2, s2, info2);
                MySerializationHelpers.CreateAndRead(stream, out arg3, s3, info3);
                MySerializationHelpers.CreateAndRead(stream, out arg4, s4, info4);
                MySerializationHelpers.CreateAndRead(stream, out arg5, s5, info5);
                MySerializationHelpers.CreateAndRead(stream, out arg6, s6, info6);
                MySerializationHelpers.CreateAndRead(stream, out arg7, s7, info7);
            }
            else
            {
                MySerializationHelpers.Write(stream, ref arg1, s1, info1);
                MySerializationHelpers.Write(stream, ref arg2, s2, info2);
                MySerializationHelpers.Write(stream, ref arg3, s3, info3);
                MySerializationHelpers.Write(stream, ref arg4, s4, info4);
                MySerializationHelpers.Write(stream, ref arg5, s5, info5);
                MySerializationHelpers.Write(stream, ref arg6, s6, info6);
                MySerializationHelpers.Write(stream, ref arg7, s7, info7);
            }
        }
    }
}
