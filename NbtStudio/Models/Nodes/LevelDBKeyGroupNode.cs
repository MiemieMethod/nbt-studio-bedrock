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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace NbtStudio.Models.Nodes
{
    internal class LevelDBKeyGroupNode : ModelNode<ILevelDBKeyGroupOrKey>
    {
        public readonly ILevelDBKeyGroup KeyGroup;
        public readonly LevelDBFolder Folder;
        public LevelDBKeyGroupNode(NbtTreeModel tree, INode parent, ILevelDBKeyGroup group) : base(tree, parent)
        {
            KeyGroup = group;
            Folder = group.Folder;
            KeyGroup.KeysChanged += LevelDBKeyGroup_KeysChanged;
            KeyGroup.ActionPerformed += LevelDBKeyGroup_ActionPerformed;
        }

        protected override IEnumerable<ILevelDBKeyGroupOrKey> GetChildren()
        {
            if (!KeyGroup.HasResolved)
                KeyGroup.Refresh();
            return (IEnumerable<ILevelDBKeyGroupOrKey>)KeyGroup;
        }

        protected override void SelfDispose()
        {
            KeyGroup.KeysChanged -= LevelDBKeyGroup_KeysChanged;
            KeyGroup.ActionPerformed -= LevelDBKeyGroup_ActionPerformed;
        }

        private void LevelDBKeyGroup_ActionPerformed(UndoableAction action)
        {
            NoticeAction(action);
        }

        private void LevelDBKeyGroup_KeysChanged()
        {
            RefreshChildren();
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
            switch(KeyGroup.Type)
            {
                case LevelDBKeyGroupType.All:
                    return "All Keys";
                case LevelDBKeyGroupType.Dimension:
                    return "Dimension Keys";
                case LevelDBKeyGroupType.Chunk:
                    return "Chunk Keys";
                case LevelDBKeyGroupType.Others:
                    return "Other Keys";
                default:
                    return "Unknown Keys";
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
