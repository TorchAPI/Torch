using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Steamworks;
using VRage.Game.ModAPI;

namespace Torch.Utils
{
    public static class MiscExtensions
    {
        private static readonly ThreadLocal<WeakReference<byte[]>> _streamBuffer = new ThreadLocal<WeakReference<byte[]>>(() => new WeakReference<byte[]>(null));

        private static long LengthSafe(this Stream stream)
        {
            try
            {
                return stream.Length;
            }
            catch
            {
                return 512;
            }
        }

        public static byte[] ReadToEnd(this Stream stream, int optionalDataLength = -1)
        {
            byte[] buffer;
            if (!_streamBuffer.Value.TryGetTarget(out buffer))
                buffer = new byte[stream.LengthSafe()];
            var initialBufferSize = optionalDataLength > 0 ? optionalDataLength : stream.LengthSafe();
            if (buffer.Length < initialBufferSize)
                buffer = new byte[initialBufferSize];
            if (buffer.Length < 1024)
                buffer = new byte[1024];
            var streamPosition = 0;
            while (true)
            {
                if (buffer.Length == streamPosition)
                    Array.Resize(ref buffer, Math.Max((int)stream.LengthSafe(), buffer.Length * 2));
                int count = stream.Read(buffer, streamPosition, buffer.Length - streamPosition);
                if (count == 0)
                    break;
                streamPosition += count;
            }
            var result = new byte[streamPosition];
            Array.Copy(buffer, 0, result, 0, result.Length);
            _streamBuffer.Value.SetTarget(buffer);
            return result;
        }

        public static IPAddress GetRemoteIP(this P2PSessionState_t state)
        {
            // What is endianness anyway?
            return new IPAddress(BitConverter.GetBytes(state.m_nRemoteIP).Reverse().ToArray());
        }

        public static string GetGridOwnerName(this MyCubeGrid grid)
        {
            if (grid.BigOwners.Count == 0 || grid.BigOwners[0] == 0)
                return "nobody";
            return MyMultiplayer.Static.GetMemberName(MySession.Static.Players.TryGetSteamId(grid.BigOwners[0]));
        }
    }
}
