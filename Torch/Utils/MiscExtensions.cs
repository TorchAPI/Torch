using System;
using System.IO;
using System.Net;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Steamworks;

namespace Torch.Utils
{
    public static class MiscExtensions
    {
        public static byte[] ReadToEnd(this Stream stream, int optionalDataLength = -1)
        {
            long streamLength = optionalDataLength;

            try
            {
                if (stream.CanSeek)
                    streamLength = stream.Length;
            }
            catch
            {
            }

            int bufferSize = streamLength > 0 ? (int)streamLength : 32768;
            var buffer = new byte[bufferSize];
            int totalRead = 0;
            int numRead;

            do
            {
                int toRead = buffer.Length - totalRead;

                numRead = stream.Read(buffer, totalRead, toRead);

                totalRead += numRead;

                if (numRead > 0 && totalRead == buffer.Length)
                {
                    if (streamLength > 0)
                        break;

                    Array.Resize(ref buffer, buffer.Length * 2);
                }
            }
            while (numRead > 0);

            var result = new byte[totalRead];

            Array.Copy(buffer, 0, result, 0, result.Length);

            return result;
        }

        public static IPAddress GetRemoteIP(this P2PSessionState_t state)
        {
            // Reverse endianness
            var bytes = BitConverter.GetBytes(state.m_nRemoteIP);
            Array.Reverse(bytes);

            return new IPAddress(bytes);
        }

        public static string GetGridOwnerName(this MyCubeGrid grid)
        {
            if (grid.BigOwners.Count == 0 || grid.BigOwners[0] == 0)
                return "nobody";

            var identityId = grid.BigOwners[0];

            if (MySession.Static.Players.IdentityIsNpc(identityId))
            {
                var identity = MySession.Static.Players.TryGetIdentity(identityId);
                return identity.DisplayName;
            }
            else
            {
                return MyMultiplayer.Static.GetMemberName(MySession.Static.Players.TryGetSteamId(identityId));
            }
        }
    }
}