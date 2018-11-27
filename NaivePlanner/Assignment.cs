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

}
