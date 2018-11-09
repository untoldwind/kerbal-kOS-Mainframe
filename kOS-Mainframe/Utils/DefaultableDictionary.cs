using System;
namespace kOSMainframe.Utils
{
    //A simple wrapper around a Dictionary, with the only change being that
    //accessing the value of a nonexistent key returns a default value instead of an error.
    class DefaultableDictionary<TKey, TValue> : KeyableDictionary<TKey, TValue>
    {
        private readonly TValue defaultValue;

        public DefaultableDictionary(TValue defaultValue)
        {
            this.defaultValue = defaultValue;
        }

        public override TValue this[TKey key]
        {
            get
            {
                TValue val;
                if (d.TryGetValue(key, out val))
                    return val;

                return defaultValue;
            }
            set
            {
                if (d.ContainsKey(key)) d[key] = value;
                else
                {
                    k.Add(key);
                    d.Add(key, value);
                }
            }
        }
    }
}
