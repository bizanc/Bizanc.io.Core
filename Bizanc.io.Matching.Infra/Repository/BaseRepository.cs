using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Repository;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;

namespace Bizanc.io.Matching.Infra.Repository
{
    public abstract class BaseRepository
    {
        protected static IDocumentStore Store { get; private set; }
        private static bool started = false;
        private static object startLock = new object();
        public BaseRepository(string db = null)
        {
            if (!started)
            {
                if (string.IsNullOrEmpty(db))
                {
                    started = true;

                    EmbeddedServer.Instance.StartServer(new ServerOptions() { FrameworkVersion = "2.2.8" });
#if DEBUG
                    EmbeddedServer.Instance.OpenStudioInBrowser();
#endif
                    Store = EmbeddedServer.Instance.GetDocumentStore("Bizancio");
                }
                else
                {
                    var database = "Bizancio";
                    var retries = 0;

                    while (Store == null && retries < 10)
                    {

                        try
                        {
                            Store = new DocumentStore()
                            {
                                Urls = new[] { db }
                            }.Initialize();
                            try
                            {
                                Store.Maintenance.ForDatabase(database).Send(new GetStatisticsOperation());
                            }
                            catch (DatabaseDoesNotExistException)
                            {
                                try
                                {
                                    Store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(database)));
                                }
                                catch (ConcurrencyException)
                                {
                                    // The database was already created before calling CreateDatabaseOperation
                                }
                            }

                            Store.Dispose();
                            Store = new DocumentStore()
                            {
                                Urls = new[] { db },
                                Database = database
                            }.Initialize();
                        }
                        catch
                        {
                            retries++;
                            Store = null;

                            if(retries >= 10)
                                throw;
                            else
                                Thread.Sleep(3000);

                        }
                    }
                }
            }
        }
    }

    public abstract class BaseRepository<T> : BaseRepository, IBaseRepository<T>
    {
        public BaseRepository(string db = null) : base(db) { }

        public async Task Save(T obj)
        {
            using (var s = Store.OpenAsyncSession())
            {
                await s.StoreAsync(obj);
                await s.SaveChangesAsync();

                s.Advanced.WaitForIndexesAfterSaveChanges(new TimeSpan(1, 0, 0));
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