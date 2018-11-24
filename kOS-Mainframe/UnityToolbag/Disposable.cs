using System;

namespace kOSMainframe.UnityToolbag {
    public class Disposable<T> : IDisposable {
        public readonly T value;
        private Pool<T> pool;

        public Disposable(Pool<T> pool, T value) {
            this.pool = pool;
            this.value = value;
        }

        public void Dispose() {
            pool.Release(value);
        }
    }
}
