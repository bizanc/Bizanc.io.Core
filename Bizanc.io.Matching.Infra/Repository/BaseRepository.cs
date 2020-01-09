using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Repository;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Embedded;

namespace Bizanc.io.Matching.Infra.Repository
{
    public abstract class BaseRepository
    {
        protected static IDocumentStore Store { get; private set; }
        private static bool started = false;
        private static object startLock = new object();
        public BaseRepository()
        {
            if (!started)
            {
                started = true;
                
                EmbeddedServer.Instance.StartServer(new ServerOptions(){ FrameworkVersion= "2.2.7" });
#if DEBUG
                EmbeddedServer.Instance.OpenStudioInBrowser();
#endif
                Store = EmbeddedServer.Instance.GetDocumentStore("Bizancio");
            }
        }
    }

    public abstract class BaseRepository<T> : BaseRepository, IBaseRepository<T>
    {
        public BaseRepository() : base() { }

        public async Task Save(T obj)
        {
            using (var s = Store.OpenAsyncSession())
            {
                await s.StoreAsync(obj);
                await s.SaveChangesAsync();

                s.Advanced.WaitForIndexesAfterSaveChanges(new TimeSpan(1,0,0));
            }
        }

        public async Task Save(IEnumerable<T> list)
        {
            using (var s = Store.OpenAsyncSession())
            {
                foreach (var obj in list)
                    await s.StoreAsync(obj);

                await s.SaveChangesAsync();

                s.Advanced.WaitForIndexesAfterSaveChanges(new TimeSpan(1, 0, 0));
            }
        }

        public async Task StreamResult(IAsyncDocumentSession session, IQueryable<T> query, Channel<T> result)
        {
            using (var stream = await session.Advanced.StreamAsync(query))
            {
                while (await stream.MoveNextAsync())
                    await result.Writer.WriteAsync(stream.Current.Document);
            }

            result.Writer.Complete();
        }
    }
}