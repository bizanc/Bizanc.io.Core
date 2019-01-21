using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Repository;
using Raven.Client.Documents;
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
                
                EmbeddedServer.Instance.StartServer(new ServerOptions(){ FrameworkVersion = "2.2.1"});
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
    }
}