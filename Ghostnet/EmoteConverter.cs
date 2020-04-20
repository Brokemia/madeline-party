using System;
using Celeste.Mod.Ghost.Net;

namespace MadelineParty.Ghostnet
{
    public static class EmoteConverter
    {
        private static string emotePrefix = MadelinePartyModule.emotePrefix;

        public static ChunkMEmote convertMinigameStartToEmoteChunk(MinigameStartData data)
        {
            // b for begin
            string value = emotePrefix + 'b';
            // +1 to account for choice values of 0
            // I think it caused problems with null-terminated strings earlier
            value += (char)(data.choice + 1);
            value += (char)data.gameStart.ToString().Length;
            value += data.gameStart;
            value += (char)data.playerID;
            value += data.playerName;

            return new ChunkMEmote
            {
                Value = value
            };
        }

        public static MinigameStartData convertEmoteValueToMinigameStart(string value)
        {
            if (value.StartsWith(emotePrefix + 'b', StringComparison.InvariantCulture))
            {
                MinigameStartData result = new MinigameStartData();
                result.choice = value[emotePrefix.Length + 1] - 1;
                int gameStartLength = value[emotePrefix.Length + 2];
                result.gameStart = long.Parse(value.Substring(emotePrefix.Length + 3, gameStartLength));
                result.playerID = value[emotePrefix.Length + 3 + gameStartLength];
                result.playerName = value.Substring(emotePrefix.Length + 4 + gameStartLength);
                return result;
            }
            return null;
        }

        public static ChunkMEmote convertRandomSeedToEmoteChunk(RandomSeedData data)
        {
            // s for seed
            string value = emotePrefix + 's';
            value += (char)data.turnOrderSeed.ToString().Length;
            value += data.turnOrderSeed;
            value += (char)data.tieBreakerSeed.ToString().Length;
            value += data.tieBreakerSeed;
            value += (char)data.playerID;
            value += data.playerName;

            return new ChunkMEmote
            {
                Value = value
            };
        }

        public static RandomSeedData convertEmoteValueToRandomSeed(string value)
        {
            if (value.StartsWith(emotePrefix + 's', StringComparison.InvariantCulture))
            {
                RandomSeedData result = new RandomSeedData();
                int seedLen = value[emotePrefix.Length + 1];
                result.turnOrderSeed = uint.Parse(value.Substring(emotePrefix.Length + 2, seedLen));
                int seedLen2 = value[emotePrefix.Length + 2 + seedLen];
                result.tieBreakerSeed = uint.Parse(value.Substring(emotePrefix.Length + 3 + seedLen, seedLen2));
                result.playerID = value[emotePrefix.Length + 3 + seedLen + seedLen2];
                result.playerName = value.Substring(emotePrefix.Length + 4 + seedLen + seedLen2);
                return result;
            }
            return null;
        }

        public static ChunkMEmote convertDieRollToEmoteChunk(DieRollData data)
        {
            // d for dice
            string value = emotePrefix + 'd';
            value += (char)data.rolls.Length;
            foreach (int roll in data.rolls)
            {
                value += (char)roll;
            }
            value += (char)data.playerID;
            value += data.playerName;

            return new ChunkMEmote
            {
                Value = value
            };
        }

        public static DieRollData convertEmoteValueToDieRoll(string value)
        {
            if (value.StartsWith(emotePrefix + 'd', StringComparison.InvariantCulture))
            {
                DieRollData result = new DieRollData();
                result.rolls = new int[value[emotePrefix.Length + 1]];
                Console.WriteLine("Number of rolls: " + result.rolls.Length);
                for (int i = 0; i < result.rolls.Length; i++)
                {
                    result.rolls[i] = value[emotePrefix.Length + 2 + i];
                    Console.WriteLine("Roll get: "+ result.rolls[i]);
                }
                result.playerID = value[emotePrefix.Length + 2 + result.rolls.Length];
                result.playerName = value.Substring(emotePrefix.Length + 3 + result.rolls.Length);
                return result;
            }
            return null;
        }

        public static ChunkMEmote convertMinigameEndToEmoteChunk(MinigameEndData data)
        {
            // m for minigame
            string value = emotePrefix + 'm';
            value += (char)data.results.ToString().Length;
            value += data.results;
            value += (char)data.playerID;
            value += data.playerName;

            return new ChunkMEmote
            {
                Value = value
            };
        }

        public static MinigameEndData convertEmoteValueToMinigameEnd(string value)
        {
            if (value.StartsWith(emotePrefix + 'm', StringComparison.InvariantCulture))
            {
                MinigameEndData result = new MinigameEndData();
                int resultLength = value[emotePrefix.Length + 1];
                result.results = uint.Parse(value.Substring(emotePrefix.Length + 2, resultLength));
                result.playerID = value[emotePrefix.Length + 2 + resultLength];
                result.playerName = value.Substring(emotePrefix.Length + 3 + resultLength);
                return result;
            }
            return null;
        }

        public static ChunkMEmote convertPlayerChoiceToEmoteChunk(PlayerChoiceData choice)
        {
            // p for player
            string value = emotePrefix + 'p';
            switch (choice.choiceType)
            {
                case PlayerChoiceData.ChoiceType.HEART:
                    value += 'h';
                    break;
                case PlayerChoiceData.ChoiceType.DIRECTION:
                    value += 'd';
                    break;
                case PlayerChoiceData.ChoiceType.HEARTX:
                    value += 'x';
                    break;
                case PlayerChoiceData.ChoiceType.HEARTY:
                    value += 'y';
                    break;
                case PlayerChoiceData.ChoiceType.ENTERSHOP:
                    value += 'e';
                    break;
                case PlayerChoiceData.ChoiceType.SHOPITEM:
                    value += 'i';
                    break;
            }
            // +1 to account for choice values of 0
            // I think it caused problems with null-terminated strings earlier
            value += (char)(choice.choice + 1);
            value += (char)choice.playerID;
            value += choice.playerName;

            return new ChunkMEmote
            {
                Value = value
            };
        }

        public static PlayerChoiceData convertEmoteValueToPlayerChoice(string value)
        {
            if (value.StartsWith(emotePrefix + 'p', StringComparison.InvariantCulture))
            {
                PlayerChoiceData result = new PlayerChoiceData();
                switch (value[emotePrefix.Length + 1])
                {
                    case 'h':
                        result.choiceType = PlayerChoiceData.ChoiceType.HEART;
                        break;
                    case 'd':
                        result.choiceType = PlayerChoiceData.ChoiceType.DIRECTION;
                        break;
                    case 'x':
                        result.choiceType = PlayerChoiceData.ChoiceType.HEARTX;
                        break;
                    case 'y':
                        result.choiceType = PlayerChoiceData.ChoiceType.HEARTY;
                        break;
                    case 'e':
                        result.choiceType = PlayerChoiceData.ChoiceType.ENTERSHOP;
                        break;
                    case 'i':
                        result.choiceType = PlayerChoiceData.ChoiceType.SHOPITEM;
                        break;
                }
                result.choice = value[emotePrefix.Length + 2] - 1;
                result.playerID = value[emotePrefix.Length + 3];
                result.playerName = value.Substring(emotePrefix.Length + 4);
                return result;
            }
            return null;
        }

        public static ChunkMEmote convertPartyChunkToEmoteChunk(MadelinePartyChunk chunk)
        {
            // c for chunk
            string value = emotePrefix + 'c';
            if (chunk.playerSelectTrigger == -2)
            {
                // Joining a party
                value += 'p';
                value += chunk.lookingForParty;
                value += (char)chunk.version.Length;
                value += chunk.version;
                value += (char)chunk.playerID;
                value += (char)chunk.respondingTo;
                value += chunk.partyHost ? 't' : 'f';
                value += chunk.playerName;
            }
            else
            {
                // Player select
                value += 's';
                value += chunk.playerSelectTrigger + 2;
                value += (char)chunk.playerID;
                value += (char)chunk.respondingTo;
                value += chunk.playerName;
            }

            return new ChunkMEmote
            {
                Value = value
            };
        }

        public static MadelinePartyChunk convertEmoteValueToChunk(string value)
        {
            if (value.StartsWith(emotePrefix + 'c', StringComparison.InvariantCulture))
            {
                switch (value[emotePrefix.Length + 1])
                {
                    case 'p':
                        // Joining a party
                        byte partySize = (byte)char.GetNumericValue(value[emotePrefix.Length + 2]);
                        int verLen = value[emotePrefix.Length + 3];
                        string version = value.Substring(emotePrefix.Length + 4, verLen);
                        uint pID = value[emotePrefix.Length + 4 + verLen];
                        uint respond = value[emotePrefix.Length + 5 + verLen];
                        bool pHost = value[emotePrefix.Length + 6 + verLen] == 't';
                        string pName = value.Substring(emotePrefix.Length + 7 + verLen);
                        return new MadelinePartyChunk
                        {
                            lookingForParty = partySize,
                            playerID = pID,
                            respondingTo = respond,
                            partyHost = pHost,
                            playerName = pName
                        };
                    case 's':
                        // Player select
                        // Added 2 so it would fit in a single char and still allow negative numbers
                        int selectTrigger = (int)char.GetNumericValue(value[emotePrefix.Length + 2]) - 2;
                        pID = value[emotePrefix.Length + 3];
                        respond = value[emotePrefix.Length + 4];
                        pName = value.Substring(emotePrefix.Length + 5);
                        return new MadelinePartyChunk
                        {
                            playerSelectTrigger = selectTrigger,
                            playerID = pID,
                            respondingTo = respond,
                            playerName = pName
                        };
                }
            }
            return null;

        }
    }
}
