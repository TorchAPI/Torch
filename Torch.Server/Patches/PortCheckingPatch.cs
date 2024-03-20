using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using NLog;
using Torch.Managers.PatchManager;

namespace Torch.Patches
{
    [PatchShim]
    internal static class PortCheckingPatch
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();
        
        public static void Patch(PatchContext ctx)
        {
            _log.Info("performing port check");
            ctx.GetPattern(typeof(Sandbox.Engine.Multiplayer.MyDedicatedServerBase).GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance))
                .Prefixes.Add(typeof(PortCheckingPatch).GetMethod(nameof(Initialize)));
        }
        
        public static void Initialize(IPEndPoint serverEndpoint)
        {
            _log.Info("Checking if port is in use");

            // Create a TcpListener on localhost at the specified port
            TcpListener tcpListener = null;
            try
            {
                tcpListener = new TcpListener(IPAddress.Loopback, serverEndpoint.Port);
                tcpListener.Start();
            }
            catch (SocketException)
            {
                // If an exception is caught, then the port is in use
                throw new Exception("Port is in use, check if another server is running on the same port");
            }
            finally
            {
                tcpListener?.Stop();
            }
        }
    }
}