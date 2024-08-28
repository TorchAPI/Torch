using System;
using System.Collections.Generic;
using System.Reflection;
using Havok;
using NLog;
using Sandbox.Definitions;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using VRage.Game;
using VRage.Game.Definitions.Reputation;

namespace Torch.Patches
{
    [PatchShim]
    public static class FactionPatch
    {
        public const int MAX_FACTION_INFO_LENGTH = 512;
        public const int MAX_FACTION_NAME_LENGTH = 64;
        public const int PLAYER_FACTION_TAG_LENGTH = 3;
        public const int NPC_FACTION_TAG_LENGTH = 10;
        
        private static Logger _log = LogManager.GetCurrentClassLogger();
        private static MethodInfo _registerFactionTagMethod = typeof(MyFactionCollection).GetMethod("RegisterFactionTag", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo _addMethod = typeof(MyFactionCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo _compatDefaultFactions = typeof(MyFactionCollection).GetMethod("CompatDefaultFactions", BindingFlags.Public | BindingFlags.Instance);
        private static MethodInfo _cleanupFactionMsgMethod = typeof(MyFactionCollection).GetMethod("CleanUpFactionMessage", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(typeof(MyFactionCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)).Prefixes.Add(typeof(FactionPatch).GetMethod(nameof(AddPrefix)));
            ctx.GetPattern(typeof(MyFactionCollection).GetMethod("Init", BindingFlags.Public | BindingFlags.Instance)).Prefixes.Add(typeof(FactionPatch).GetMethod(nameof(InitPrefix)));

            _log.Info($"Patched {nameof(MyFactionCollection)}.{nameof(MyFactionCollection.Add)}");
        }

        public static bool AddPrefix(MyFaction faction, MyFactionCollection __instance)
        {
            //get instance of         private Dictionary<long, MyFaction> m_factions = new Dictionary<long, MyFaction>();
            var m_factions = typeof(MyFactionCollection).GetField("m_factions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);

            if (m_factions.GetType().GetMethod("ContainsKey")?.Invoke(m_factions, new object[] { faction.FactionId }) is
                    bool containsKey && containsKey)
            {
                _log.Warn($"Faction {faction.Tag} already exists, not adding.");
                return false;
            }
            
            //add faction to dictionary
            m_factions.GetType().GetMethod("Add")?.Invoke(m_factions, new object[] { faction.FactionId, faction });
            _registerFactionTagMethod.Invoke(__instance, new object[] { faction });

            return false;
        }
        
        public static object CreateMyReputationNotification(MyHudNotification myHudNotification)
        {
            // Get the internal type using Assembly.GetType
            // Make sure to replace "AssemblyContainingInternalClass" with the actual assembly containing the internal class
            Type internalType = Assembly.GetAssembly(typeof(MyPerPlayerData)).GetType("Sandbox.Game.Multiplayer.MyReputationNotification");

            // Get the constructor information
            ConstructorInfo ctorInfo = internalType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(MyHudNotification) }, null);
            
            // Create an instance of MyReputationNotification
            object myReputationNotificationInstance = ctorInfo.Invoke(new object[] { myHudNotification });

            // You now have an object of type MyReputationNotification.
            // You can use it as a parameter or use reflection to call its methods.

            return myReputationNotificationInstance;
        }

        public static bool InitPrefix(MyObjectBuilder_FactionCollection builder, MyFactionCollection __instance)
        {
            _log.Info("Faction INIT");
            var m_playerFaction = typeof(MyFactionCollection).GetField("m_playerFaction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            var m_relationsBetweenFactions = typeof(MyFactionCollection).GetField("m_relationsBetweenFactions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            var m_relationsBetweenPlayersAndFactions = typeof(MyFactionCollection).GetField("m_relationsBetweenPlayersAndFactions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            var m_factionRequests = typeof(MyFactionCollection).GetField("m_factionRequests", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            var m_playerToFactionsVis = typeof(MyFactionCollection).GetField("m_playerToFactionsVis", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            
            int _factionCounter = 0;
            foreach (var factionBuilder in builder.Factions)
            {
                _factionCounter++;
                
                // For compatibility from before there was currency in the game.
                // If existing faction have account already, do not create another one.
                if (!MyBankingSystem.Static.TryGetAccountInfo(factionBuilder.FactionId, out var accountInfo))
                    MyBankingSystem.Static.CreateAccount(factionBuilder.FactionId, 0);
                
                factionBuilder.Name = CleanUpStringInput(factionBuilder.Name, _factionCounter);
                factionBuilder.Description = CleanUpStringInput(factionBuilder.Description, _factionCounter);
                factionBuilder.Tag = CleanUpStringInput(factionBuilder.Tag, _factionCounter);
                factionBuilder.PrivateInfo = CleanUpStringInput(factionBuilder.PrivateInfo, _factionCounter);
                
                factionBuilder.Name = factionBuilder.Name.Substring(0, Math.Min(factionBuilder.Name.Length, MAX_FACTION_NAME_LENGTH));
                factionBuilder.Tag = factionBuilder.Tag.Substring(0, Math.Min(factionBuilder.Tag.Length, NPC_FACTION_TAG_LENGTH));
                
                //sorry

                if (factionBuilder.Description != null)
                {
                    if (factionBuilder.Description.Contains("Potentially Exploited Faction"))
                        factionBuilder.Description = string.Empty;
                }

                if(factionBuilder.PrivateInfo != null)
                {
                    if (factionBuilder.PrivateInfo.Contains("Potentially Exploited Faction"))
                        factionBuilder.PrivateInfo = string.Empty;
                }

                if (string.IsNullOrWhiteSpace(factionBuilder.Name))
                    factionBuilder.Name = $"Potentially Exploited Faction {_factionCounter}";
                
                if (string.IsNullOrWhiteSpace(factionBuilder.Tag))
                    factionBuilder.Tag = $"PEF{_factionCounter}";

                //do the same substring but with null checks for the description and private info
                if (factionBuilder.Description != null)
                    factionBuilder.Description = factionBuilder.Description.Substring(0, Math.Min(factionBuilder.Description.Length, MAX_FACTION_INFO_LENGTH));
                
                if (factionBuilder.PrivateInfo != null)
                    factionBuilder.PrivateInfo = factionBuilder.PrivateInfo.Substring(0, Math.Min(factionBuilder.PrivateInfo.Length, MAX_FACTION_INFO_LENGTH));


                _addMethod.Invoke(__instance, new object[] { new MyFaction(factionBuilder) });
            }

            foreach (var player in builder.Players.Dictionary)
            {
                if (m_playerFaction.GetType().GetMethod("ContainsKey")
                            ?.Invoke(m_playerFaction, new object[] { player.Key }) is
                        bool containsKey && containsKey)
                {
                    _log.Error($"{player.Key} | {player.Value} Is already added, crash avoided");
                    continue;
                }
                
                m_playerFaction.GetType().GetMethod("Add")?.Invoke(m_playerFaction, new object[] { player.Key, player.Value });

            }

            MySessionComponentEconomy eco = null;
            if (MySession.Static != null)
            {
                eco = MySession.Static.GetComponent<MySessionComponentEconomy>();
            }
            
            var m_ValidateRepuationConsistency = typeof(MySessionComponentEconomy).GetMethod("ValidateReputationConsistency", BindingFlags.NonPublic | BindingFlags.Instance);

            MyRelationsBetweenFactions rel;
            int rep;

            foreach (var relation in builder.Relations)
            {
                rel = relation.Relation;
                rep = relation.Reputation;
                if (eco != null)
                {
                    var result = m_ValidateRepuationConsistency.Invoke(eco, new object[] { rel, rep }) as Tuple<MyRelationsBetweenFactions, int>;
                    if (result != null)
                    {
                        rel = result.Item1;
                        rep = result.Item2;
                    }

                }
                
                m_relationsBetweenFactions.GetType().GetMethod("Add")?.Invoke(m_relationsBetweenFactions, new object[] { new MyFactionCollection.MyRelatablePair(relation.FactionId1, relation.FactionId2), new Tuple<MyRelationsBetweenFactions, int>(rel, rep)});
            }

            foreach (var relation in builder.RelationsWithPlayers)
            {
                rel = relation.Relation;
                rep = relation.Reputation;
                if (eco != null)
                {
                    var result = m_ValidateRepuationConsistency.Invoke(eco, new object[] { rel, rep }) as Tuple<MyRelationsBetweenFactions, int>;
                    if (result != null)
                    {
                        rel = result.Item1;
                        rep = result.Item2;
                    }

                }

                if (m_relationsBetweenPlayersAndFactions.GetType().GetMethod("ContainsKey")?.Invoke(m_relationsBetweenPlayersAndFactions,
                        new object[]
                                { new MyFactionCollection.MyRelatablePair(relation.PlayerId, relation.FactionId) }) is
                            bool containsKey && containsKey)
                {
                    _log.Error($"Relation between {relation.PlayerId} and {relation.FactionId} is a detected duplicate, crash avoided!");
                        continue;
                }
                
                m_relationsBetweenPlayersAndFactions.GetType().GetMethod("Add")?.Invoke(m_relationsBetweenPlayersAndFactions, new object[] { new MyFactionCollection.MyRelatablePair(relation.PlayerId, relation.FactionId), new Tuple<MyRelationsBetweenFactions, int>(rel, rep) });
            }

            foreach (var request in builder.Requests)
            {
                var set = new HashSet<long>();

                foreach (var entry in request.FactionRequests)
                    set.Add(entry);

                m_factionRequests.GetType().GetMethod("Add")?.Invoke(m_factionRequests, new object[] { request.FactionId, set });
            }

            if (builder.PlayerToFactionsVis != null)
            {
                m_playerToFactionsVis.GetType().GetMethod("Clear")?.Invoke(m_playerToFactionsVis, new object[] { });

                foreach (var playerFactionsOb in builder.PlayerToFactionsVis)
                {
                    var discoveredFactions = new List<long>();
                    foreach (var discoveredFaction in playerFactionsOb.DiscoveredFactions)
                    {
                        discoveredFactions.Add(discoveredFaction);
                    }

                    if (playerFactionsOb.IdentityId != 0)
                    {
                        if (Sync.Players.TryGetPlayerId(playerFactionsOb.IdentityId, out var playerId))
                            m_playerToFactionsVis.GetType().GetMethod("Add")?.Invoke(m_playerToFactionsVis, new object[] {playerId, discoveredFactions });
                    }
                    else
                    {
                        var playerId = new MyPlayer.PlayerId(playerFactionsOb.PlayerId, playerFactionsOb.SerialId);
                        if (Sync.Players.TryGetIdentityId(playerId.SteamId, playerId.SerialId) != 0)
                            m_playerToFactionsVis.GetType().GetMethod("Add")?.Invoke(m_playerToFactionsVis, new object[] { playerId, discoveredFactions });
                    }
                }
            }
            
            var defaultSettings = MyDefinitionManager.Static.GetDefinition<MyReputationSettingsDefinition>("DefaultReputationSettings");
            //set default reputation settings
            typeof(MyFactionCollection).GetField("m_reputationSettings", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(__instance,defaultSettings );

            typeof(MyFactionCollection).GetField("m_notificationRepInc", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(__instance, CreateMyReputationNotification(new MyHudNotification(text: MySpaceTexts.Economy_Notification_ReputationIncreased, font: MyFontEnum.Green)));
            
            typeof(MyFactionCollection).GetField("m_notificationRepDec", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(__instance,
                    CreateMyReputationNotification(new MyHudNotification(
                        text: MySpaceTexts.Economy_Notification_ReputationDecreased, font: MyFontEnum.Red)));

            _compatDefaultFactions.Invoke(__instance, new object[] { null });
            _log.Info("Factions INIT END");
            return false;
        }
        
        private static string CleanUpStringInput(string input, int factionCountCurrent)
        {
            if (input == null)
            {
                return input;
            }
            
            return input.Replace('&', ' ').Replace('#', ' ');
            //do the same but after, if the input is an empty string or just whitespace, return "Potentially Exploited Faction (INVESTIGATE)"
            return string.IsNullOrWhiteSpace(input) ? $"Potentially Exploited Faction data (INVESTIGATE)-{factionCountCurrent}" : input;
        }
    }
}
