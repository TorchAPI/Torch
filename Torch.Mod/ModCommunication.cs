using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using Torch.Mod.Messages;
using VRage;
using VRage.Collections;
using VRage.Game.ModAPI;
using VRage.Network;
using VRage.Utils;
using Task = ParallelTasks.Task;

namespace Torch.Mod
{
    public static class ModCommunication
    {
        public const ushort NET_ID = 4352;
        private static bool _closing = false;
        private static List<IMyPlayer> _playerCache;

        public static void Register()
        {
            MyLog.Default.WriteLineAndConsole("TORCH MOD: Registering mod communication.");
            //_processing = new BlockingCollection<MessageBase>(new ConcurrentQueue<MessageBase>());
            _playerCache = new List<IMyPlayer>();
           // _messagePool = new MyConcurrentPool<IncomingMessage>(8);

            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NET_ID, MessageHandler);
            //MyAPIGateway.Multiplayer.RegisterMessageHandler(NET_ID, MessageHandler);
            //background thread to handle de/compression and processing
            _closing = false;


            //MyAPIGateway.Parallel.StartBackground(DoProcessing);


            MyLog.Default.WriteLineAndConsole("TORCH MOD: Mod communication registered successfully.");
        }

        private static void MessageHandler(ushort arg1, byte[] arg2, ulong arg3, bool arg4)
        {
            try
            {
                MyLog.Default.WriteLineAndConsole($"Recieved Torch Message!");
                MessageBase msgBase = MyAPIGateway.Utilities.SerializeFromBinary<MessageBase>(arg2);
                

                    if (true)
                        MyAPIGateway.Utilities.ShowMessage("Torch", $"Received message of type {msgBase.GetType().Name}");

                    if (MyAPIGateway.Multiplayer.IsServer)
                        msgBase.ProcessServer();
                    else
                        msgBase.ProcessClient();
                

            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"TORCH MOD: Failed to deserialize message! {ex}");
                return;
            }
        }

        public static void Unregister()
        {
            MyLog.Default.WriteLineAndConsole("TORCH MOD: Unregistering mod communication.");
            MyAPIGateway.Multiplayer?.UnregisterSecureMessageHandler(NET_ID, MessageHandler);
            //_processing?.CompleteAdding();
            _closing = true;
            //_task.Wait();
        }

        public static void DoProcessing(MessageBase m)
        {
            if (true)
                MyAPIGateway.Utilities.ShowMessage("Torch", $"Sending message of type {m.GetType().Name}");

            var b = MyAPIGateway.Utilities.SerializeToBinary(m);


            switch (m.TargetType)
            {
                case MessageTarget.Single:
                    MyAPIGateway.Multiplayer.SendMessageTo(NET_ID, b, m.Target);
                    break;
                case MessageTarget.Server:
                    MyAPIGateway.Multiplayer.SendMessageToServer(NET_ID, b);
                    break;
                case MessageTarget.AllClients:
                    MyAPIGateway.Players.GetPlayers(_playerCache);
                    foreach (var p in _playerCache)
                    {
                        if (p.SteamUserId == MyAPIGateway.Multiplayer.MyId)
                            continue;
                        MyAPIGateway.Multiplayer.SendMessageTo(NET_ID, b, p.SteamUserId);
                    }

                    break;
                case MessageTarget.AllExcept:
                    MyAPIGateway.Players.GetPlayers(_playerCache);
                    foreach (var p in _playerCache)
                    {
                        if (p.SteamUserId == MyAPIGateway.Multiplayer.MyId || m.Ignore.Contains(p.SteamUserId))
                            continue;
                        MyAPIGateway.Multiplayer.SendMessageTo(NET_ID, b, p.SteamUserId);
                    }

                    break;
                default:
                    throw new Exception();
            }

            //_playerCache.Clear();

        }




        /*
        public static void DoProcessing()
        {
            while (!_closing)
            {
                try
                {
                    MessageBase m;
                    try
                    {
                        m = _processing.Take();
                    }
                    catch
                    {
                        continue;
                    }

                    MyLog.Default.WriteLineAndConsole($"Processing message: {m.GetType().Name}");

                    if (m is IncomingMessage) //process incoming messages
                    {
                        MessageBase i;
                        try
                        {
                            var o = MyCompression.Decompress(m.CompressedData);
                            m.CompressedData = null;
                            _messagePool.Return((IncomingMessage)m);
                            i = MyAPIGateway.Utilities.SerializeFromBinary<MessageBase>(o);
                        }
                        catch (Exception ex)
                        {
                            MyLog.Default.WriteLineAndConsole($"TORCH MOD: Failed to deserialize message! {ex}");
                            continue;
                        }

                        if (TorchModCore.Debug)
                            MyAPIGateway.Utilities.ShowMessage("Torch", $"Received message of type {i.GetType().Name}");

                        if (MyAPIGateway.Multiplayer.IsServer)
                            i.ProcessServer();
                        else
                            i.ProcessClient();
                    }
                    else //process outgoing messages
                    {
                        if (TorchModCore.Debug)
                            MyAPIGateway.Utilities.ShowMessage("Torch", $"Sending message of type {m.GetType().Name}");

                        var b = MyAPIGateway.Utilities.SerializeToBinary(m);
                        m.CompressedData = MyCompression.Compress(b);

                        switch (m.TargetType)
                        {
                            case MessageTarget.Single:
                                MyAPIGateway.Multiplayer.SendMessageTo(NET_ID, m.CompressedData, m.Target);
                                break;
                            case MessageTarget.Server:
                                MyAPIGateway.Multiplayer.SendMessageToServer(NET_ID, m.CompressedData);
                                break;
                            case MessageTarget.AllClients:
                                MyAPIGateway.Players.GetPlayers(_playerCache);
                                foreach (var p in _playerCache)
                                {
                                    if (p.SteamUserId == MyAPIGateway.Multiplayer.MyId)
                                        continue;
                                    MyAPIGateway.Multiplayer.SendMessageTo(NET_ID, m.CompressedData, p.SteamUserId);
                                }

                                break;
                            case MessageTarget.AllExcept:
                                MyAPIGateway.Players.GetPlayers(_playerCache);
                                foreach (var p in _playerCache)
                                {
                                    if (p.SteamUserId == MyAPIGateway.Multiplayer.MyId || m.Ignore.Contains(p.SteamUserId))
                                        continue;
                                    MyAPIGateway.Multiplayer.SendMessageTo(NET_ID, m.CompressedData, p.SteamUserId);
                                }

                                break;
                            default:
                                throw new Exception();
                        }

                        _playerCache.Clear();
                    }
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLineAndConsole($"TORCH MOD: Exception occurred in communication thread! {ex}");
                }
            }

            MyLog.Default.WriteLineAndConsole("TORCH MOD: INFO: Communication thread shut down successfully! THIS IS NOT AN ERROR");
            //exit signal received. Clean everything and GTFO
            _processing?.Dispose();
            _processing = null;
            _messagePool?.Clean();
            _messagePool = null;
            _playerCache = null;
        }
        */
        public static void SendMessageTo(MessageBase message, ulong target)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                throw new Exception("Only server can send targeted messages");

            if (_closing)
                return;

            message.Target = target;
            message.TargetType = MessageTarget.Single;
            DoProcessing(message);
        }

        public static void SendMessageToClients(MessageBase message)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                throw new Exception("Only server can send targeted messages");

            if (_closing)
                return;

            message.TargetType = MessageTarget.AllClients;
            DoProcessing(message);
        }

        public static void SendMessageExcept(MessageBase message, params ulong[] ignoredUsers)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                throw new Exception("Only server can send targeted messages");

            if (_closing)
                return;

            message.TargetType = MessageTarget.AllExcept;
            message.Ignore = ignoredUsers;
            DoProcessing(message);
        }

        public static void SendMessageToServer(MessageBase message)
        {
            if (_closing)
                return;

            message.TargetType = MessageTarget.Server;
            DoProcessing(message);
        }
    }
}
