using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Bizanc.io.Matching.Core.Util
{
    public class StreamEnumerable<T> : IEnumerable<T>
    {
        private CancellationToken cancelToken;

        private ImmutableList<StreamEnumerator<T>> observers = new List<StreamEnumerator<T>>().ToImmutableList();
        private ImmutableList<T> buffer = new List<T>().ToImmutableList();
        private bool cleanup = false;

        public StreamEnumerable(CancellationToken cancelToken, bool clean = false)
        {
            this.cancelToken = cancelToken;
            this.cleanup = clean;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var enumerator = new StreamEnumerator<T>(cancelToken);

            if (buffer.Count > 0 && !cleanup)
            {
                foreach (var d in buffer)
                    enumerator.Notify(d);


            }

            observers = observers.Add(enumerator);
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Push(T data)
        {
            if (observers.Count == 0)
            {
                if (!cleanup)
                    buffer = buffer.Add(data);
            }
            else
                observers.ForEach(o => o.Notify(data));
        }
    }
}