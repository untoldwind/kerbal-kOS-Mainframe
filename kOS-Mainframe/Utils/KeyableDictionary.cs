using System;
using System.Collections.Generic;

namespace kOSMainframe.Utils {
    //A simple wrapper around a Dictionary, with the only change being that
    //The keys are also stored in a list so they can be iterated without allocating an IEnumerator
    class KeyableDictionary<TKey, TValue> : IDictionary<TKey, TValue> {
        protected Dictionary<TKey, TValue> d = new Dictionary<TKey, TValue>();
        // Also store the keys in a list so we can iterate them without allocating an IEnumerator
        protected List<TKey> k = new List<TKey>();

        public virtual TValue this[TKey key] {
            get {
                return d[key];
            }
            set {
                if (d.ContainsKey(key)) d[key] = value;
                else {
                    k.Add(key);
                    d.Add(key, value);
                }
            }
        }

        public void Add(TKey key, TValue value) {
            k.Add(key);
            d.Add(key, value);
        }
        public bool ContainsKey(TKey key) {
            return d.ContainsKey(key);
        }
        public ICollection<TKey> Keys {
            get {
                return d.Keys;
            }
        }
        public List<TKey> KeysList {
            get {
                return k;
            }
        }

        public bool Remove(TKey key) {
            return d.Remove(key) && k.Remove(key);
        }
        public bool TryGetValue(TKey key, out TValue value) {
            return d.TryGetValue(key, out value);
        }
        public ICollection<TValue> Values {
            get {
                return d.Values;
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item) {
            ((IDictionary<TKey, TValue>)d).Add(item);
            k.Add(item.Key);
        }

        public void Clear() {
            d.Clear();
            k.Clear();
        }
        public bool Contains(KeyValuePair<TKey, TValue> item) {
            return ((IDictionary<TKey, TValue>)d).Contains(item);
        }
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            ((IDictionary<TKey, TValue>)d).CopyTo(array, arrayIndex);
        }
        public int Count {
            get {
                return d.Count;
            }
        }
        public bool IsReadOnly {
            get {
                return ((IDictionary<TKey, TValue>)d).IsReadOnly;
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) {
            return ((IDictionary<TKey, TValue>)d).Remove(item) && k.Remove(item.Key);
        }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            return d.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return ((System.Collections.IEnumerable)d).GetEnumerator();
        }
    }
}
