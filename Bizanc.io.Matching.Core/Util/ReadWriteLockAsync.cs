using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bizanc.io.Matching.Core.Util
{
    public class ReadWriteLockAsync
    {
        private SemaphoreSlim locker;
        private SemaphoreSlim writeLocker = new SemaphoreSlim(1, 1);

        private int size;
        public ReadWriteLockAsync(int size = 10)
        {
            this.size = size;
            locker = new SemaphoreSlim(size, size);
        }

        public async Task EnterReadLock() =>
            await locker.WaitAsync();

        public void ExitReadLock() =>
            locker.Release();

        public async Task EnterWriteLock()
        {
            if (size == 1)
                await locker.WaitAsync();
            else
            {
                await writeLocker.WaitAsync();
                for (int i = 0; i < size; i++)
                    await locker.WaitAsync();
                writeLocker.Release();
            }
        }

        public void ExitWriteLock() =>
            locker.Release(size);
    }
}