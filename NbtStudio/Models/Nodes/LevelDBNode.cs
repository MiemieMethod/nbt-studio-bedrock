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

namespace NbtStudio.Models.Nodes
{
    internal class LevelDBNode : ModelNode<ILevelDBKeyGroup>
    {
        public readonly LevelDBFolder Folder;
        public LevelDBNode(NbtTreeModel tree, INode parent, LevelDBFolder folder) : base(tree, parent)
        {
            Folder = folder;
            Folder.KeysChanged += LevelDB_KeysChanged;
            Folder.ActionPerformed += LevelDB_ActionPerformed;
            Folder.OnSaved += LevelDB_OnSaved;
        }

        protected override IEnumerable<ILevelDBKeyGroup> GetChildren()
        {
            if (!Folder.HasResolved)
                Folder.Resolve();
            return Folder.Groups;
        }

        protected override void SelfDispose()
        {
            Folder.KeysChanged -= LevelDB_KeysChanged;
            Folder.ActionPerformed -= LevelDB_ActionPerformed;
            Folder.OnSaved -= LevelDB_OnSaved;
        }

        private void LevelDB_OnSaved()
        {
            RefreshChildren();
        }

        private void LevelDB_ActionPerformed(UndoableAction action)
        {
            NoticeAction(action);
        }

        private void LevelDB_KeysChanged()
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

        public override string Description
        {
            get
            {
                if (Folder.Path is null)
                {
                    return "unsaved leveldb";
                }

                string folderName = System.IO.Path.GetFileName(Folder.Path);
                string levelName = TryGetLevelName();

                if (!string.IsNullOrEmpty(levelName))
                {
                    return $"{folderName} ({levelName})";
                }

                return folderName;
            }
        }
    }
}
