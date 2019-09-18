using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Bizanc.io.Matching.Core.Domain.Messages;
using System.Threading;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Util;
using Serilog;

namespace Bizanc.io.Matching.Core.Domain.Immutable
{
    public class EntityPool<T> where T : BaseMessage
    {
        private Dictionary<string, T> pool = new Dictionary<string, T>();
        private ReadWriteLockAsync poolLocker = new ReadWriteLockAsync(1);
        public Guid id = Guid.NewGuid();

        public async Task<List<T>> GetPool()
        {
            try
            {
                await poolLocker.EnterReadLock();
                return pool.Values.ToList();
            }
            finally
            {
                poolLocker.ExitReadLock();
            }
        }

        public EntityPool() { }

        public EntityPool<T> Fork()
        {
            var fork = new EntityPool<T>(this);
            return fork;
        }

        public EntityPool(EntityPool<T> prevPool)
        {
            try
            {
                poolLocker.EnterReadLock().Wait();
                prevPool.poolLocker.EnterReadLock().Wait();
                foreach (var data in prevPool.pool.Values)
                    if (!pool.ContainsKey(data.HashStr))
                        pool.Add(data.HashStr, data.Clone<T>());
            }
            finally
            {
                prevPool.poolLocker.ExitReadLock();
                poolLocker.ExitReadLock();
            }
        }

        public async Task<bool> Add(T obj)
        {
            if (!pool.ContainsKey(obj.HashStr))
            {
                try
                {
                    await poolLocker.EnterWriteLock();
                    if (!pool.ContainsKey(obj.HashStr))
                    {
                        pool.Add(obj.HashStr, obj.Clone<T>());
                        return true;
                    }
                }
                finally
                {
                    poolLocker.ExitWriteLock();
                }
            }

            return false;
        }

        public async Task Add(IEnumerable<T> list)
        {
            try
            {
                await poolLocker.EnterWriteLock();
                foreach (var obj in list)
                {
                    var clone = obj.Clone<T>();
                    clone.Reset();

                    if (!pool.ContainsKey(obj.HashStr))
                        pool.Add(obj.HashStr, clone);
                }
            }
            finally
            {
                poolLocker.ExitWriteLock();
            }
        }

        public async Task<bool> Contains(T obj)
        {
            try
            {
                //await poolLocker.EnterReadLock();
                return await Task.FromResult(pool.ContainsKey(obj.HashStr));
            }
            finally
            {
                //poolLocker.ExitReadLock();
            }
        }

        public async Task<bool> ContainsAll(IEnumerable<T> batch)
        {
            try
            {
                await poolLocker.EnterReadLock();

                foreach(var obj in batch)
                    if(!pool.ContainsKey(obj.HashStr))
                        return false;
            }
            finally
            {
                poolLocker.ExitReadLock();
            }

            return true;
        }

        public async Task<int> Count()
        {
            try
            {
                await poolLocker.EnterReadLock();
                return pool.Count;
            }
            finally
            {
                poolLocker.ExitReadLock();
            }
        }

        public async Task<T> Get(string id)
        {
            try
            {
                await poolLocker.EnterReadLock();
                if (pool.ContainsKey(id))
                    return pool[id];
                
                return null;
            }
            finally
            {
                poolLocker.ExitReadLock();
            }
        }

        public async Task<List<T>> Take(int size)
        {
            if (pool.Count < size || size == 0)
                return await GetPool();
            else
            {
                try
                {
                    await poolLocker.EnterReadLock();
                    return pool.Values.Take(size).ToList();
                }
                finally
                {
                    poolLocker.ExitReadLock();
                }
            }
        }

        public void VerifyTX(Domain.Transaction tx)
        {
            Log.Debug("Contain Pool " + pool.ContainsKey(tx.HashStr));
            Log.Debug("Pool Count" + pool.Count);
        }

        public async Task Remove(IEnumerable<T> batch)
        {
            if (batch == null || batch.Count() == 0)
                return;

            if (pool.Count > 0)
            {
                try
                {
                    await poolLocker.EnterWriteLock();
                    foreach (var d in batch)
                    {
                        if (pool.ContainsKey(d.HashStr))
                        {
                            d.Finish();
                            pool.Remove(d.HashStr);
                        }
                    }
                }
                finally
                {
                    poolLocker.ExitWriteLock();
                }
            }
        }
    }
}