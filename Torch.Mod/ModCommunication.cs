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
using VRage.Game.ModAPI;
using VRage.Utils;
using Task = ParallelTasks.Task;

namespace Torch.Mod
{
    public static class ModCommunication
    {
        public const ushort NET_ID = 4352;
        private static bool _closing;
        private static ConcurrentQueue<MessageBase> _outgoing;
        private static ConcurrentQueue<byte[]> _incoming;
        private static List<IMyPlayer> _playerCache;
        private static FastResourceLock _lock;
        private static Task _task;

        public static void Register()
        {
            MyLog.Default.WriteLineAndConsole("TORCH MOD: Registering mod communication.");
            _outgoing = new ConcurrentQueue<MessageBase>();
            _incoming = new ConcurrentQueue<byte[]>();
            _playerCache = new List<IMyPlayer>();
            _lock = new FastResourceLock();
           

            MyAPIGateway.Multiplayer.RegisterMessageHandler(NET_ID, MessageHandler);
            //background thread to handle de/compression and processing
            _task = MyAPIGateway.Parallel.StartBackground(DoProcessing);
            MyLog.Default.WriteLineAndConsole("TORCH MOD: Mod communication registered successfully.");
        }

        public static void Unregister()
        {
            MyLog.Default.WriteLineAndConsole("TORCH MOD: Unregistering mod communication.");
            MyAPIGateway.Multiplayer?.UnregisterMessageHandler(NET_ID, MessageHandler);
            ReleaseLock();
            _closing = true;
            //_task.Wait();
        }

        private static void MessageHandler(byte[] bytes)
        {
            _incoming.Enqueue(bytes);
            ReleaseLock();
        }

        public static void DoProcessing()
        {
            while (!_closing)
            {
                try
                {
                    byte[] incoming;
                    while (_incoming.TryDequeue(out incoming))
                    {
                        MessageBase m;
                        try
                        {
                            var o = MyCompression.Decompress(incoming);
                            m = MyAPIGateway.Utilities.SerializeFromBinary<MessageBase>(o);
                        }
                        catch (Exception ex)
                        {
                            MyLog.Default.WriteLineAndConsole($"TORCH MOD: Failed to deserialize message! {ex}");
                            continue;
                        }
                        if (MyAPIGateway.Multiplayer.IsServer)
                            m.ProcessServer();
                        else
                            m.ProcessClient();
                    }

                    if (!_outgoing.IsEmpty)
                    {
                        List<MessageBase> tosend = new List<MessageBase>(_outgoing.Count);
                        MessageBase outMessage;
                        while (_outgoing.TryDequeue(out outMessage))
                        {
                            var b = MyAPIGateway.Utilities.SerializeToBinary(outMessage);
                            outMessage.CompressedData = MyCompression.Compress(b);
                            tosend.Add(outMessage);
                        }

                        MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                                                                  {
                                                                      MyAPIGateway.Players.GetPlayers(_playerCache);
                                                                      foreach (var outgoing in tosend)
                                                                      {
                                                                          switch (outgoing.TargetType)
                                                                          {
                                                                              case MessageTarget.Single:
                                                                                  MyAPIGateway.Multiplayer.SendMessageTo(NET_ID, outgoing.CompressedData, outgoing.Target);
                                                                                  break;
                                                                              case MessageTarget.Server:
                                                                                  MyAPIGateway.Multiplayer.SendMessageToServer(NET_ID, outgoing.CompressedData);
                                                                                  break;
                                                                              case MessageTarget.AllClients:
                                                                                  foreach (var p in _playerCache)
                                                                                  {
                                                                                      if (p.SteamUserId == MyAPIGateway.Multiplayer.MyId)
                                                                                          continue;
                                                                                      MyAPIGateway.Multiplayer.SendMessageTo(NET_ID, outgoing.CompressedData, p.SteamUserId);
                                                                                  }
                                                                                  break;
                                                                              case MessageTarget.AllExcept:
                                                                                  foreach (var p in _playerCache)
                                                                                  {
                                                                                      if (p.SteamUserId == MyAPIGateway.Multiplayer.MyId || outgoing.Ignore.Contains(p.SteamUserId))
                                                                                          continue;
                                                                                      MyAPIGateway.Multiplayer.SendMessageTo(NET_ID, outgoing.CompressedData, p.SteamUserId);
                                                                                  }
                                                                                  break;
                                                                              default:
                                                                                  throw new Exception();
                                                                          }
                                                                      }
                                                                      _playerCache.Clear();
                                                                  });
                    }

                    AcquireLock();
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLineAndConsole($"TORCH MOD: Exception occurred in communication thread! {ex}");
                }
            }

            MyLog.Default.WriteLineAndConsole("TORCH MOD: COMMUNICATION THREAD: EXIT SIGNAL RECIEVED!");
            //exit signal received. Clean everything and GTFO
            _outgoing = null;
            _incoming = null;
            _playerCache = null;
            _lock = null;
        }

        public static void SendMessageTo(MessageBase message, ulong target)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                throw new Exception("Only server can send targeted messages");
            message.Target = target;
            message.TargetType = MessageTarget.Single;
            MyLog.Default.WriteLineAndConsole($"Sending message of type {message.GetType().FullName}");
            _outgoing.Enqueue(message);
            ReleaseLock();
        }

        public static void SendMessageToClients(MessageBase message)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                throw new Exception("Only server can send targeted messages");
            message.TargetType = MessageTarget.AllClients;
            _outgoing.Enqueue(message);
            ReleaseLock();
        }

        public static void SendMessageExcept(MessageBase message, params ulong[] ignoredUsers)
        {
            if (MyAPIGateway.Multiplayer.IsServer)
                throw new Exception("Only server can send targeted messages");
            message.TargetType = MessageTarget.AllExcept;
            message.Ignore = ignoredUsers;
            _outgoing.Enqueue(message);
            ReleaseLock();
        }

        public static void SendMessageToServer(MessageBase message)
        {
            message.TargetType = MessageTarget.Server;
            _outgoing.Enqueue(message);
            ReleaseLock();
        }

        private static void ReleaseLock()
        {
            if(_lock==null)
                return;
            while(!_lock.TryAcquireExclusive())
                _lock.ReleaseExclusive();
            _lock.ReleaseExclusive();
        }

        private static void AcquireLock()
        {
            if (_lock == null)
                return;
            ReleaseLock();
            _lock.AcquireExclusive();
            _lock.AcquireExclusive();
        }
    }
}
