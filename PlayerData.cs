using System;
using System.Collections.Generic;

namespace MadelineParty
{
    public class PlayerData : IComparable
    {
        public uint CelesteNetID = uint.MaxValue;
        public int TurnOrder;
        public int TokenSelected;
        // Used to maintain player position across minigames
        public PlayerToken token;
        // What the player rolls at the beginning of the game
        // Determines Turn Order
        public int StartingRoll;
        public List<GameData.Item> Items = new() { };
        public List<int> pastBoardSpaceIDs = new();

        public int Strawberries {
            get;
            private set;
        }
        public int Hearts {
            get;
            private set;
        }

        public PlayerData(int token)
        {
            TokenSelected = token;
        }

        public PlayerData(int token, uint gnetID) : this(token)
        {
            CelesteNetID = gnetID;
        }

        // Adds or subtracts the specified number of strawberries from the total
        public void ChangeStrawberries(int strawberries)
        {
            var change = Math.Max(-Strawberries, strawberries);
            if(change > 0 && TokenSelected == GameData.Instance.realPlayerID) {
                MadelinePartyModule.SaveData.BerriesCollected += change;
                
                if(MadelinePartyModule.SaveData.BerriesCollected >= 500) {
                    AchievementHelperImports.TriggerAchievement?.Invoke("MadelineParty", "Collect_Strawberries_500");
                    if (MadelinePartyModule.SaveData.BerriesCollected >= 1000) {
                        AchievementHelperImports.TriggerAchievement?.Invoke("MadelineParty", "Collect_Strawberries_1000");
                        if (MadelinePartyModule.SaveData.BerriesCollected >= 5000) {
                            AchievementHelperImports.TriggerAchievement?.Invoke("MadelineParty", "Collect_Strawberries_5000");
                        }
                    }
                }
            }
            Strawberries += change;
            if(Strawberries == 202 && TokenSelected == GameData.Instance.realPlayerID) {
                AchievementHelperImports.TriggerAchievement?.Invoke("MadelineParty", "Game_Strawberries_202");
            }
        }

        public void AddHeart() {
            
            MadelinePartyModule.SaveData.HeartsCollected++;
            Hearts++;
            
            AchievementHelperImports.TriggerAchievement?.Invoke("MadelineParty", "Collect_Hearts_1");
            if (MadelinePartyModule.SaveData.HeartsCollected >= 24) {
                AchievementHelperImports.TriggerAchievement?.Invoke("MadelineParty", "Collect_Hearts_24");
                if (MadelinePartyModule.SaveData.HeartsCollected >= 88) {
                    AchievementHelperImports.TriggerAchievement?.Invoke("MadelineParty", "Collect_Hearts_88");
                }
            }
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            if (obj is PlayerData other)
            {
                int compare = other.StartingRoll.CompareTo(StartingRoll);  
                if (compare == 0)
                {
                    return TurnOrder.CompareTo(other.TurnOrder);
                }
                return compare;
            }
            return 1;
        }
    }
}
