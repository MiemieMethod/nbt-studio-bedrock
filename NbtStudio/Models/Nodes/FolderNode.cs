﻿using Aga.Controls.Tree;
using fNbt;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TryashtarUtils.Utility;

namespace NbtStudio
{
    public class FolderNode : ModelNode<IHavePath>
    {
        public readonly NbtFolder Folder;
        public FolderNode(NbtTreeModel tree, INode parent, NbtFolder folder) : base(tree, parent)
        {
            Folder = folder;
            Folder.ContentsChanged += Folder_ContentsChanged;
        }

        protected override void SelfDispose()
        {
            Folder.ContentsChanged -= Folder_ContentsChanged;
        }

        private void Folder_ContentsChanged(object sender, EventArgs e)
        {
            RefreshChildren();
        }

        public override bool HasChildren
        {
            get
            {
                if (!Folder.HasScanned)
                    return true;
                return base.HasChildren;
            }
        }

        protected override IEnumerable<IHavePath> GetChildren()
        {
            if (!Folder.HasScanned)
                Folder.Scan();
            return Folder.DBFolders.Concat(Folder.Subfolders.Concat<IHavePath>(Folder.Files));
        }

        public override string Description => System.IO.Path.GetFileName(Folder.Path);

        public override bool CanCopy => true;
        public override DataObject Copy() => FileNodeOperations.Copy(Folder.Path);
        public override bool CanCut => true;
        public override DataObject Cut() => FileNodeOperations.Cut(Folder.Path);
        public override bool CanDelete => true;
        public override void Delete()
        {
            FileNodeOperations.DeleteFolder(Folder.Path);
            base.Delete();
        }
        public override bool CanEdit => true;
        public override bool CanPaste => true;
        public override IEnumerable<INode> Paste(IDataObject data)
        {
            var children = GetChildren().ToList();
            var files = (string[])data.GetData("FileDrop");
            var drop_effect = (MemoryStream)data.GetData("Preferred DropEffect");
            if (files is null || drop_effect is null)
                return Enumerable.Empty<INode>();
            var bytes = new byte[4];
            drop_effect.Read(bytes, 0, bytes.Length);
            var drop = (DragDropEffects)BitConverter.ToInt32(bytes, 0);
            bool move = drop.HasFlag(DragDropEffects.Move);
            foreach (var item in files)
            {
                var destination = IOUtils.GetUniqueFilename(System.IO.Path.Combine(Folder.Path, System.IO.Path.GetFileName(item)));
                if (move)
                {
                    if (Directory.Exists(item))
                        FileSystem.MoveDirectory(item, destination, UIOption.AllDialogs);
                    else if (File.Exists(item))
                        FileSystem.MoveFile(item, destination, UIOption.AllDialogs);
                }
                else
                {
                    if (Directory.Exists(item))
                        FileSystem.CopyDirectory(item, destination, UIOption.AllDialogs);
                    else if (File.Exists(item))
                        FileSystem.CopyFile(item, destination, UIOption.AllDialogs);
                }
            }
            Folder.Scan();
            var new_children = children.Except(GetChildren().ToList());
            return NodeChildren(new_children);
        }
        public override bool CanRename => true;
        public override bool CanSort => false;
        public override bool CanReceiveDrop(IEnumerable<INode> nodes) => nodes.All(x => x.Get<IFile>() is not null || x is FolderNode);
        public override void ReceiveDrop(IEnumerable<INode> nodes, int index)
        {
            var files = nodes.Filter(x => x.Get<IFile>());
            var folders = nodes.Filter(x => x.Get<NbtFolder>());
            foreach (var file in files)
            {
                if (file.Path is not null)
                {
                    var destination = System.IO.Path.Combine(Folder.Path, System.IO.Path.GetFileName(file.Path));
                    FileSystem.MoveFile(file.Path, destination, UIOption.AllDialogs);
                }
            }
            foreach (var folder in folders)
            {
                var destination = System.IO.Path.Combine(Folder.Path, System.IO.Path.GetFileName(folder.Path));
                FileSystem.MoveDirectory(folder.Path, destination, UIOption.AllDialogs);
            }
            Folder.Scan();
        }
    }
}
