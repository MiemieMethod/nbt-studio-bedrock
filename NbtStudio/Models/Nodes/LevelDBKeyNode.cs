using fNbt;
using LevelDBWrapper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TryashtarUtils.Utility;

namespace NbtStudio.Models.Nodes
{
    internal class LevelDBKeyNode : ModelNode<ILevelDBKeyGroupOrKey>
    {
        public readonly LevelDBKey Key;
        public readonly LevelDBFolder Folder;
        private bool HasSetupEvents = false;
        public LevelDBKeyNode(NbtTreeModel tree, INode parent, LevelDBKey key) : base(tree, parent)
        {
            Key = key;
            Folder = key.Folder;
            Key.OnChanged += LevelDBKey_OnChanged;
            Key.OnLoaded += LevelDBKey_OnLoaded;
        }

        protected override IEnumerable<ILevelDBKeyGroupOrKey> GetChildren()
        {
            return Enumerable.Empty<ILevelDBKeyGroupOrKey>();
        }

        protected override void SelfDispose()
        {
            Key.OnChanged -= LevelDBKey_OnChanged;
            Key.OnLoaded -= LevelDBKey_OnLoaded;
        }

        private void LevelDBKey_OnChanged(object sender, EventArgs e)
        {
            RefreshChildren();
        }

        private void LevelDBKey_OnLoaded(object sender, EventArgs e)
        {
            SetupEvents();
        }

        private void SetupEvents()
        {
            if (!HasSetupEvents)
            {
                HasSetupEvents = true;
            }
        }

        public string TryGetLevelName()
        {
            string levelNamePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Folder.Path), "levelname.txt");
            if (File.Exists(levelNamePath))
            {
                return File.ReadAllText(levelNamePath).Trim();
            }
            return null;
        }

        public string GetLabel()
        {
            switch (Key.Type)
            {
                case LevelDBKeyType.Others:
                    return Encoding.ASCII.GetString(Key.Key);
                case LevelDBKeyType.Chunk:
                    ChunkKeySubType type = Key.SubType;
                    if (type == ChunkKeySubType.SubChunkPrefix)
                        return $"({Key.X}, {Key.Z}; {Key.Dimension}) SubChunk {Key.SubchunkIndex}";
                    return $"({Key.X}, {Key.Z}; {Key.Dimension}) " + type.ToString();
                case LevelDBKeyType.Actor:
                    return $"Actor {Key.ActorUID}";
                case LevelDBKeyType.ActorDigest:
                    return $"Actor Digest ({Key.X}, {Key.Z}; {Key.Dimension})";
                case LevelDBKeyType.Village:
                    return $"Village {Key.VillageUUID} ({Key.VillageSubType})";
                default:
                    return BitConverter.ToString(Key.Key).Replace("-", "");
            }
        }

        public string PreviewContents()
        {
            switch (Key.Type)
            {
                case LevelDBKeyType.Others:
                    return $"[{BitConverter.ToString(Key.Key).Replace("-", "")}]";
                case LevelDBKeyType.Chunk:
                    return $"[{BitConverter.ToString(Key.Key).Replace("-", "")}]";
                case LevelDBKeyType.Actor:
                    return $"[{BitConverter.ToString(Key.Key).Replace("-", "")}, actorprefix{Key.ActorUID}]";
                case LevelDBKeyType.ActorDigest:
                    return $"[{BitConverter.ToString(Key.Key).Replace("-", "")}, digp{BitConverter.ToString(Key.Key, 4).Replace("-", "")}]";
                case LevelDBKeyType.Village:
                    return $"[{BitConverter.ToString(Key.Key).Replace("-", "")}, VILLAGE_{Key.VillageDimension}{Key.VillageUUID}_{Key.VillageSubType}]";
                default:
                    return $"[{BitConverter.ToString(Key.Key).Replace("-", "")}, {Encoding.ASCII.GetString(Key.Key)}]";
            }
            
        }

        public override string Description
        {
            get
            {
                if (Folder.Path is null)
                {
                    return "unsaved leveldb";
                }

                string groupTypeName = GetLabel();
                string levelName = TryGetLevelName();

                if (!string.IsNullOrEmpty(levelName))
                {
                    return $"{groupTypeName} ({levelName})";
                }

                return groupTypeName;
            }
        }
    }
}
