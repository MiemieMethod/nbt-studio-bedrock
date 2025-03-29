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
using System.Collections;

namespace NbtStudio
{
    public interface ILevelDBKeyGroupOrKey
    {
    }

    public interface ILevelDBKeyGroup : IRefreshable
    {
        LevelDBFolder Folder { get; }
        LevelDB DB { get; }
        LevelDBKeyGroupType Type { get; }
        public event Action KeysChanged;
        public event Action<UndoableAction> ActionPerformed;
        public bool HasResolved { get; }
        public int Count { get; }
    }

    public enum LevelDBKeyGroupType : byte
    {
        Unknown = 0xff,
        All = 0,
        Dimension,
        Chunk,
        Others,
    }

    public abstract class LevelDBKeyGroup<T> : IList<T>, ILevelDBKeyGroup, ILevelDBKeyGroupOrKey
    {
        public LevelDBFolder Folder { get; private set; }
        public LevelDB DB { get; private set; }
        public abstract LevelDBKeyGroupType Type { get; protected set; }
        public abstract int Count { get; }
        public abstract bool IsReadOnly { get; protected set; }
        public bool HasResolved { get; protected set; } = false;
        public bool CanRefresh => Folder.CanRefresh;

        public abstract T this[int index] { get; set; }

        public event Action KeysChanged;
        public event Action<UndoableAction> ActionPerformed;

        public LevelDBKeyGroup(LevelDBFolder db_folder)
        {
            Folder = db_folder;
            DB = Folder.DB;
        }
        protected virtual void OnKeysChanged()
        {
            KeysChanged?.Invoke();
        }

        public abstract int IndexOf(T item);
        public abstract void Insert(int index, T item);
        public abstract void RemoveAt(int index);
        public abstract void Add(T item);
        public abstract void Clear();
        public abstract bool Contains(T item);
        public abstract void CopyTo(T[] array, int arrayIndex);
        public abstract bool Remove(T item);
        public abstract IEnumerator<T> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public abstract void Refresh();
    }

    public class MiscLevelDBKeyGroup : LevelDBKeyGroup<LevelDBKey>
    {
        public override LevelDBKeyGroupType Type { get; protected set; }
        public List<LevelDBKey> Keys { get; private set; } = new();
        public override int Count => Keys.Count;
        public override bool IsReadOnly { get; protected set; } = false;
        public override void Refresh() => Resolve();

        public MiscLevelDBKeyGroup(LevelDBFolder db_folder, LevelDBKeyGroupType type) : base(db_folder)
        {
            Type = type;
        }

        public void Resolve()
        {
            IEnumerable<byte[]> keys = DB.ByteKeys();
            switch (Type)
            {
                case LevelDBKeyGroupType.Dimension:
                    break;
                case LevelDBKeyGroupType.Chunk:
                    break;
                case LevelDBKeyGroupType.Others:
                    break;
                case LevelDBKeyGroupType.All:
                    foreach (byte[] k in keys)
                        Keys.Add(new LevelDBKey(Folder, k));
                    break;
            }
            HasResolved = true;
            OnKeysChanged();
        }


        public override LevelDBKey this[int index] {
            get => Keys[index];
            set => throw new NotImplementedException();
        }

        public override void Add(LevelDBKey item)
        {
            throw new NotImplementedException();
        }

        public override void Clear()
        {
            throw new NotImplementedException();
        }

        public override bool Contains(LevelDBKey item)
        {
            return Keys.Contains(item);
        }

        public override void CopyTo(LevelDBKey[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator<LevelDBKey> GetEnumerator()
        {
            return Keys.GetEnumerator();
        }

        public override int IndexOf(LevelDBKey item)
        {
            throw new NotImplementedException();
        }

        public override void Insert(int index, LevelDBKey item)
        {
            throw new NotImplementedException();
        }

        public override bool Remove(LevelDBKey item)
        {
            throw new NotImplementedException();
        }

        public override void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }
    }
}
