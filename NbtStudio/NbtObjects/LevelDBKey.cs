using fNbt;
using LevelDBWrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NbtStudio
{
    public enum LevelDBKeyType : byte
    {
        Unknown = 0xff,
        Chunk = 0,
        Actor,
        ActorDigest,
        Village,
        Others,
    }

    public enum ChunkKeySubType : byte
    {
        Unknown = 0xff,
        Data3D = 43,
        Version, // This was moved to the front as needed for the extended heights feature. Old chunks will not have this data.
        Data2D,
        Data2DLegacy,
        SubChunkPrefix,
        LegacyTerrain,
        BlockEntity,
        Entity,
        PendingTicks,
        LegacyBlockExtraData,
        BiomeState,
        FinalizedState,
        ConversionData, // data that the converter provides, that are used at runtime for things like blending
        BorderBlocks,
        HardcodedSpawners,
        RandomTicks,
        CheckSums,
        GenerationSeed,
        GeneratedPreCavesAndCliffsBlending = 61, // not used, DON'T REMOVE
        BlendingBiomeHeight = 62, // not used, DON'T REMOVE
        MetaDataHash,
        BlendingData,
        ActorDigestVersion,
        VersionEnchant = 110, // china only
        VersionMarkInsert, // china only
        LegacyVersion = 118,
    }

    public enum VillageKeySubType : byte
    {
        Unknown = 0x00,
        DWELLERS = 0x01,
        INFO = 0x02,
        PLAYERS = 0x03,
        POI = 0x04,
    }

    public class LevelDBKey : IExportable, ILevelDBKeyGroupOrKey
    {
        public const int BlocksXDimension = 16;
        public const int BlocksZDimension = 16;
        public LevelDBFolder Folder { get; private set; }
        public LevelDB DB { get; private set; }
        public byte[] Key { get; private set; }
        public LevelDBKeyType Type { get; private set; }
        public ChunkKeySubType SubType { get; private set; }
        public int Dimension { get; private set; }
        public int X { get; private set; }
        public int Z { get; private set; }
        public int SubchunkIndex { get; private set; }
        public long ActorUID { get; private set; }
        public VillageKeySubType VillageSubType { get; private set; }
        public string VillageDimension { get; private set; }
        public string VillageUUID { get; private set; }
        public byte[] Value { get; private set; }
        public bool HasUnsavedChanges { get; private set; } = false;
        public event EventHandler OnChanged;
        public event EventHandler OnLoaded;

        public LevelDBKey(LevelDBFolder db_folder, byte[] key)
        {
            Folder = db_folder;
            DB = Folder.DB;
            Key = key;
            Value = DB.Get(key);
            ParseKey();
        }

        private void ParseKey()
        {
            int sz = Key.Length;
            string fullkey = Encoding.ASCII.GetString(Key);

            switch (fullkey)
            {
                case "AutonomousEntities":
                case "BiomeData":
                case "LevelChunkMetaDataDictionary":
                case "Nether":
                case "Overworld":
                case "TheEnd":
                case "dimension0":
                case "dimension1":
                case "dimension2":
                case "game_flatworldlayers":
                case "mVillages":
                case "mobevents":
                case "portals":
                case "schedulerWT":
                case "scoreboard":
                case "~local_player":
                    //MessageBox.Show($"Key Length: {sz}\nFull Key: {fullkey}", "Debug Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    Type = LevelDBKeyType.Others;
                    break;
                default:
                    if (sz == 9 || sz == 10 || sz == 13 || sz == 14)
                    {
                        // 处理 Chunk Key
                        X = BitConverter.ToInt32(Key, 0);
                        Z = BitConverter.ToInt32(Key, 4);
                        Dimension = 0;
                        int keyTypeIndex = 8;
                        if (sz == 13 || sz == 14)
                        {
                            Dimension = BitConverter.ToInt32(Key, 8);
                            keyTypeIndex = 12;
                        }

                        if (Dimension < 0)
                        {
                            Type = LevelDBKeyType.Unknown;
                            return;
                        }

                        SubType = (ChunkKeySubType)Key[keyTypeIndex];

                        if ((int)SubType < 43 || ((int)SubType > 65 && (int)SubType != 110 && (int)SubType != 111 && (int)SubType != 118))
                        {
                            Type = LevelDBKeyType.Unknown;
                            return;
                        }

                        Type = LevelDBKeyType.Chunk;

                        // 处理子区块
                        SubchunkIndex = 0;
                        if (sz == 10 || sz == 14)
                        {
                            if (SubType != ChunkKeySubType.SubChunkPrefix)
                            {
                                Type = LevelDBKeyType.Unknown;
                                return;
                            }
                            SubchunkIndex = unchecked((sbyte)Key[^1]);
                        }
                    }
                    else if (sz == 19 && Encoding.ASCII.GetString(Key, 0, 11) == "actorprefix")
                    {
                        // 处理 Actor Key
                        Type = LevelDBKeyType.Actor;
                        ActorUID = BitConverter.ToInt64(Key, 11);
                    }
                    else if ((sz == 12 || sz == 16) && Encoding.ASCII.GetString(Key, 0, 4) == "digp")
                    {
                        // 处理 Actor Digest Key
                        Type = LevelDBKeyType.ActorDigest;
                        X = BitConverter.ToInt32(Key, 4);
                        Z = BitConverter.ToInt32(Key, 8);
                        Dimension = 0;
                        if (sz == 16)
                        {
                            Dimension = BitConverter.ToInt32(Key, 12);
                        }
                    }
                    else if (Encoding.ASCII.GetString(Key, 0, 8) == "VILLAGE_")
                    {
                        // 处理 Village Key
                        Type = LevelDBKeyType.Village;
                        int next_begin = 8;
                        if (Encoding.ASCII.GetString(Key, 8, 18) == "Overworld_")
                        {
                            VillageDimension = "Overworld_";
                            next_begin = 18;
                        }
                        else if (Encoding.ASCII.GetString(Key, 8, 15) == "Nether_")
                        {
                            VillageDimension = "Nether_";
                            next_begin = 15;
                        }
                        else if (Encoding.ASCII.GetString(Key, 8, 12) == "End_")
                        {
                            VillageDimension = "End_";
                            next_begin = 12;
                        }
                        else
                            VillageDimension = "";
                        VillageUUID = Encoding.ASCII.GetString(Key, next_begin, next_begin + 36);
                        string typeStr = Encoding.ASCII.GetString(Key, next_begin + 37, sz - next_begin - 37);

                        switch (typeStr)
                        {
                            case "DWELLERS":
                                VillageSubType = VillageKeySubType.DWELLERS;
                                break;
                            case "INFO":
                                VillageSubType = VillageKeySubType.INFO;
                                break;
                            case "PLAYERS":
                                VillageSubType = VillageKeySubType.PLAYERS;
                                break;
                            case "POI":
                                VillageSubType = VillageKeySubType.POI;
                                break;
                            default:
                                VillageSubType = VillageKeySubType.Unknown;
                                break;
                        }
                    }
                    else
                        Type = LevelDBKeyType.Unknown;
                    break;
            }
        }

        public void SetValue(byte[] value)
        {
            Value = value;
            HasUnsavedChanges = true;
            OnChanged?.Invoke(this, EventArgs.Empty);
        }

        public byte[] SaveBytes()
        {
            return Value;
        }

        public void SaveAs(string path)
        {
            File.WriteAllBytes(path, SaveBytes());
        }

    }
}
