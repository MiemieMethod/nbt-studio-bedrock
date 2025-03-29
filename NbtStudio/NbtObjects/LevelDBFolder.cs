using LevelDBWrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TryashtarUtils.Nbt;
using TryashtarUtils.Utility;
using System.Windows.Forms;
using fNbt;
using System.Windows;
using System.Reflection.Metadata;

namespace NbtStudio
{
    public class LevelDBFolder : IHavePath, ISaveable, IRefreshable, IDisposable
    {
        public string Path { get; private set; }
        public LevelDB DB { get; private set; }
        public IEnumerable<byte[]> Keys { get; private set; }
        public List<ILevelDBKeyGroup> Groups;
        public event Action KeysChanged;
        public event Action OnSaved;
        public event Action<UndoableAction> ActionPerformed;
        public bool HasResolved { get; private set; } = false;
        public bool CanSave => DB is not null;
        public bool CanRefresh => CanSave;
        public bool HasUnsavedChanges { get; private set; } = false;
        public void Refresh() => Resolve();

        public LevelDBFolder(string path, CompressionLevel compression)
        {
            Path = path;
            var options = new Options { CompressionLevel = compression };
            DB = new LevelDB(path, options);
        }

        public static IFailable<LevelDBFolder> TryCreate(string path, CompressionLevel compression)
        {
            if (!Directory.Exists(path))
                return new Failable<LevelDBFolder>(() => throw new DirectoryNotFoundException($"Directory not found: {path}"), "Directory not found");
            var dbFolder = new LevelDBFolder(path, compression);
            if (dbFolder.DB == null)
                return new Failable<LevelDBFolder>(() => throw new InvalidOperationException("Failed to open LevelDB"), "Failed to open LevelDB");
            return new Failable<LevelDBFolder>(() => dbFolder, "Loaded DBFolder");
        }

        public void Resolve()
        {
            Keys = DB.ByteKeys();
            PrepareGroups();
            HasResolved = true;
            KeysChanged?.Invoke();
        }

        void PrepareGroups()
        {
            Groups = new List<ILevelDBKeyGroup>
            {
                new MiscLevelDBKeyGroup(this, LevelDBKeyGroupType.All),
            };
        }

        public void Save()
        {
            HasUnsavedChanges = false;
            OnSaved?.Invoke();
        }

        public void SaveAs(string path)
        {
            Save();
        }

        public void Move(string path)
        {
            Directory.Move(Path, path);
            Path = path;
        }

        public void Dispose()
        {
            DB.Dispose();
        }
    }
}
