using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bizanc.io.Matching.Core.Util
{
    public class StreamEnumerator<T> : IEnumerator<T>
    {
        private T current = default(T);

        public T Current { get { return current; } }

        object IEnumerator.Current { get { return Current; } }

        private TaskCompletionSource<T> waitable = new TaskCompletionSource<T>();

        IImmutableQueue<T> buffer = ImmutableQueue.Create<T>();

        private object dequeueLocker = new object();

        private CancellationToken cancelToken;

        public StreamEnumerator(CancellationToken cancelToken)
        {
            this.cancelToken = cancelToken;
        }

        public void Dispose()
        {
            Console.WriteLine("Stream disposed, Buffer count: " + buffer.Count());
        }

        public bool MoveNext()
        {
            while (!cancelToken.IsCancellationRequested)
            {
                if (!buffer.IsEmpty)
                {
                    lock (dequeueLocker)
                    {
                        if (!buffer.IsEmpty)
                        {
                            buffer = buffer.Dequeue(out current);
                            Console.WriteLine("Deuque");
                            return true;
                        }
                    }
                }

                waitable.Task.Wait();
                if (!cancelToken.IsCancellationRequested)
                {
                    lock (dequeueLocker)
                    {
                        if (!buffer.IsEmpty)
                        {
                            buffer = buffer.Dequeue(out current);
                            Console.WriteLine("Deuque");
                            return true;
                        }
                        else
                            Console.WriteLine("Awaited empty queue");
                    }
                }
            }

            Console.WriteLine("Stream finished");
            return false;
        }

        public void Notify(T data)
        {
            lock (dequeueLocker)
            {
                buffer = buffer.Enqueue(data);
                var oldWaitable = waitable;
                waitable = new TaskCompletionSource<T>();
                cancelToken.Register(() => waitable.TrySetCanceled());

                oldWaitable.TrySetResult(data);
            }
        }

        public void Reset()
        {
        }
    }
}