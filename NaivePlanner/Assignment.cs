using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaivePlanner
{
    public class Assignment : IEnumerable<bool?>
    {
        BitArray _store;
        public int Count => _store.Length;
        static Random rnd = new Random();


        public Assignment(Assignment a)
        {
            _store = (BitArray)a._store.Clone();
        }

        public Assignment(int varCount, bool randomize = false)
        {
            _store = new BitArray(varCount);
            if (randomize)
            {
                for (int i = 0; i < varCount; i++)
                {
                    _store[i] = rnd.Next(2) == 0 ? false : true;
                }
            }
        }


        public bool this[int index]
        {
            get => _store[index];
            set => _store[index] = value;
        }

        public IEnumerator<bool?> GetEnumerator()
        {
            for (int i = 0; i < _store.Length; i++)
            {
                yield return _store[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _store.GetEnumerator();
        }

        public Assignment Copy()
        {
            return new Assignment(this);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < _store.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(_store[i] ? $"{i + 1}" : $"-{i + 1}");
            }
            return sb.ToString();
        }
    }

    public class Assignment_old : IEnumerable<bool>
    {
        byte[] _store = null;
        public int Count { get; private set; } = 0;

        static Random rnd = new Random();

        public Assignment_old(IEnumerable<byte> bytes, int variables)
        {
            Count = variables;
            int numBytes = variables / 8;
            if (variables % 8 > 0)
            {
                numBytes++;
            }
            var b = bytes.ToArray();
            if (b.Length != numBytes)
            {
                throw new ArgumentException(nameof(bytes));
            }

            _store = b;
        }

        private Assignment_old(Assignment_old a)
        {
            _store = new byte[a._store.Length];
            Array.Copy(a._store, _store, a._store.Length);
            Count = a.Count;
        }

        public Assignment_old Copy()
        {
            return new Assignment_old(this);
        }

        public Assignment_old(int variables, bool randomize = false)
        {
            if (variables == 0)
            {
                return;
            }

            int numBytes = variables / 8;
            if (variables % 8 > 0)
            {
                numBytes++;
            }

            _store = new byte[numBytes];
            if (randomize)
            {
                rnd.NextBytes(_store);
                var s = variables % 8;
                if (s > 0)
                {
                    int b = _store[numBytes - 1];
                    int mask = 0xFF;
                    while (s > 0)
                    {
                        mask >>= 1;
                        s--;
                    }
                    _store[numBytes - 1] = (byte)(b & mask);
                }
            }


            Count = variables;


        }

        public bool this[int index]
        {
            get
            {
                if (index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }
                var byteIndex = index / 8;
                byte mask = 0x00;
                switch (index % 8)
                {
                    case 0:
                        mask = 1;
                        break;
                    case 1:
                        mask = 2;
                        break;
                    case 2:
                        mask = 4;
                        break;
                    case 3:
                        mask = 8;
                        break;
                    case 4:
                        mask = 16;
                        break;
                    case 5:
                        mask = 32;
                        break;
                    case 6:
                        mask = 64;
                        break;
                    case 7:
                        mask = 128;
                        break;
                }
                var ret = _store[byteIndex] & mask;
                return ret > 0;
            }
            set
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }

                var byteIndex = index / 8;
                byte mask = 0x00;
                switch (index % 8)
                {
                    case 0:
                        mask = 1;
                        break;
                    case 1:
                        mask = 2;
                        break;
                    case 2:
                        mask = 4;
                        break;
                    case 3:
                        mask = 8;
                        break;
                    case 4:
                        mask = 16;
                        break;
                    case 5:
                        mask = 32;
                        break;
                    case 6:
                        mask = 64;
                        break;
                    case 7:
                        mask = 128;
                        break;
                }
                if (value == true)
                {
                    _store[byteIndex] |= mask;
                }
                else
                {
                    _store[byteIndex] &= (byte)~mask;
                }
            }
        }

        public IEnumerator<bool> GetEnumerator()
        {

            byte index = 0;
            byte mask = 1;
            for (int c = 0; c < Count; c++)
            {
                yield return (_store[index] & mask) > 0;
                mask = (mask == 128) ? (byte)0x01 : (byte)(mask << 1);
            }

        }

        public IEnumerable<byte> GetBytes()
        {
            for (int c = 0; c < _store.Length; c++)
            {
                yield return _store[c];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}
