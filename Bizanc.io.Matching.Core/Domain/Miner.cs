using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain.Messages;
using Bizanc.io.Matching.Core.Crypto;
using Bizanc.io.Matching.Core.Repository;
using Newtonsoft.Json;
using System.Collections.Immutable;
using Bizanc.io.Matching.Core.Util;
using System.Threading;
using Bizanc.io.Matching.Core.Connector;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Bizanc.io.Matching.Core.Domain.Immutable;
using Serilog;

namespace Bizanc.io.Matching.Core.Domain
{
    public class Miner : IPeerManager, IChainRepository
    {
        private string address;

        private string Address { get { return address + ":" + listenPort; } }

        private int listenPort = 5556;

        private Chain chain { get; set; }

        private ConcurrentDictionary<Guid, Chain> forks = new ConcurrentDictionary<Guid, Chain>();

        private ConcurrentDictionary<Guid, IPeer> peerDictionary = new ConcurrentDictionary<Guid, IPeer>();

        private IPeerListener peerListener;

        private Wallet wallet;

        private IBlockRepository blockRepository;

        private IWalletRepository walletRepository;

        private IBalanceRepository balanceRepository;

        private IBookRepository bookRepository;

        private IDepositRepository depositRepository;

        private IOfferRepository offerRepository;

        private ITransactionRepository transactionRepository;

        private IWithdrawalRepository withdrawalRepository;

        private ITradeRepository tradeRepository;

        private IWithdrawInfoRepository withdrawInfoRepository;

        private Channel<Block> blockStream;

        private Channel<Transaction> transactionStream;

        private Channel<Offer> offerStream;

        private Channel<Withdrawal> withdrawalStream;

        private Channel<Chain> PersistStream;

        private Channel<Chain> chainUpdateStream;

        private bool hasChainListner = false;

        private IConnector connector;

        private bool isOracle = false;

        private ReadWriteLockAsync commitLocker = new ReadWriteLockAsync(1);

        private ReadWriteLockAsync persistLock = new ReadWriteLockAsync(1);

        private int threads;

        private bool synching = false;

        private TaskCompletionSource<object> synchSource = new TaskCompletionSource<object>();

        public Miner(IPeerListener peerListener, IWalletRepository walletRepository,
                        IBlockRepository blockRepository,
                        IBalanceRepository balanceRepository,
                        IBookRepository bookRepository,
                        IDepositRepository depositRepository,
                        IOfferRepository offerRepository,
                        ITransactionRepository transactionRepository,
                        IWithdrawalRepository withdrawalRepository,
                        ITradeRepository tradeRepository,
                        IWithdrawInfoRepository withdrawInfoRepository,
                        IConnector connector,
                        int threads = 1)
        {
            this.peerListener = peerListener;
            this.walletRepository = walletRepository;
            this.blockRepository = blockRepository;
            this.balanceRepository = balanceRepository;
            this.bookRepository = bookRepository;
            this.depositRepository = depositRepository;
            this.offerRepository = offerRepository;
            this.transactionRepository = transactionRepository;
            this.withdrawalRepository = withdrawalRepository;
            this.tradeRepository = tradeRepository;
            this.withdrawInfoRepository = withdrawInfoRepository;
            this.connector = connector;
            this.threads = threads;
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("log/bizancnode_log.txt",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();

            chainUpdateStream = Channel.CreateUnbounded<Chain>();
        }

        public async Task Start(bool isOracle = false, string minerAddress = "")
        {
            this.isOracle = isOracle;

            blockStream = Channel.CreateUnbounded<Block>();
            transactionStream = Channel.CreateUnbounded<Transaction>();
            offerStream = Channel.CreateUnbounded<Offer>();
            withdrawalStream = Channel.CreateUnbounded<Withdrawal>();
            PersistStream = Channel.CreateUnbounded<Chain>();

            var persistPoints = (await blockRepository.GetPersistInfo()).OrderBy(p => p.TimeStamp).ToList();
            var balances = await balanceRepository.Get();
            var books = await bookRepository.Get();

            if (persistPoints == null || persistPoints.Count == 0)
                chain = new Chain(threads);
            else
            {
                foreach (var persistInfo in persistPoints)
                {
                    var balance = balances.Where(b => b.BlockHash == persistInfo.BlockHash).FirstOrDefault();
                    var book = books.Where(b => b.BlockHash == persistInfo.BlockHash).FirstOrDefault();
                    if (balance == null)
                        continue;

                    var block = await blockRepository.Get(persistInfo.BlockHash);
                    var lastBLock = await blockRepository.Get(block.PreviousHashStr);
                    var transact = new Immutable.TransactionManager(balance);
                    book = new Immutable.Book(book, transact);
                    var deposit = new Immutable.Deposit(null, transact);
                    var withdrawal = new Immutable.Withdrawal(null, transact);
                    chain = new Chain(chain, transact, deposit, withdrawal, book, block, lastBLock, new Immutable.Pool(), threads);
                    chain.Persisted = true;
                }
            }

            if (!isOracle)
            {
                if (string.IsNullOrEmpty(minerAddress))
                {
                    wallet = await walletRepository.Get();
                    if (wallet == null)
                    {
                        var pair = CryptoHelper.CreateKeyPair();

                        wallet = new Wallet() { PrivateKey = pair.Item1, PublicKey = pair.Item2 };
                        await walletRepository.Save(wallet);

                        Log.Information("Wallet Generated");
                        Log.Information("Public " + wallet.PublicKey);
                        Log.Information("Private " + wallet.PrivateKey);
                    }
                    else
                    {
                        Log.Information("Wallet Recoved");
                        Log.Information("Public " + wallet.PublicKey);
                        Log.Information("Private " + wallet.PrivateKey);
                    }
                }
                else
                {
                    wallet = new Wallet() { PublicKey = minerAddress };
                    Log.Information("Wallet Received");
                    Log.Information("Public " + wallet.PublicKey);
                }
                await chain.Initialize(wallet.PublicKey);
            }
            else
                await chain.Initialize("");

            ProcessDeposits();
            ProcessWithdrawInfo();
            ProcessOffers();
            ProcessTransactions();
            ProcessWithdrawal();
            ProcessPersist();

            var (deposits, withdraws) = await connector.Start(await depositRepository.GetLastEthBlockNumber(), await withdrawInfoRepository.GetLastEthBlockNumber(), await depositRepository.GetLastBtcBlockNumber(), await withdrawInfoRepository.GetLastBtcBlockNumber());
            foreach (var deposit in deposits)
                await AppendDeposit(deposit);

            foreach (var withdraw in withdraws)
                await AppendWithdraw(withdraw);

            ProcessBlocks();

            if (!isOracle)
                ProcessMining();
        }

        public async Task StartListener()
        {
            if (synching)
                await synchSource.Task;

            await peerListener.Start();
            ProcessAccept("");
        }

        public void StartSynch()
        {
            synching = true;
        }

        private async void ProcessDeposits()
        {
            var reader = connector.GetDepositsReader();
            while (await reader.WaitToReadAsync())
                await AppendDeposit(await reader.ReadAsync());
        }

        private async void ProcessWithdrawInfo()
        {
            var reader = connector.GetWithdrawsReader();
            while (await reader.WaitToReadAsync())
                await AppendWithdraw(await reader.ReadAsync());
        }

        private async void ProcessTransactions()
        {
            while (await transactionStream.Reader.WaitToReadAsync())
                AppendTransactionAsync(await transactionStream.Reader.ReadAsync());
        }

        private async void ProcessBlocks()
        {
            while (await blockStream.Reader.WaitToReadAsync())
            {
                if (synching && !synchSource.Task.IsCompleted && synchWatch.IsRunning)
                {
                    synchWatch.Stop();
                    if (synchWatch.Elapsed.Seconds >= 5)
                    {
                        synching = false;
                        synchSource.SetResult(null);
                    }
                }

                var bk = await blockStream.Reader.ReadAsync();
                if (await ProcessBlock(bk))
                {
                    Log.Information("Received newer block");
                    if(!synching)
                        Notify(bk);
                }

                if (synching && !synchSource.Task.IsCompleted)
                {
                    synchWatch = new Stopwatch();
                    synchWatch.Start();
                }
            }
        }

        private async void ProcessOffers()
        {
            while (await offerStream.Reader.WaitToReadAsync())
                await AppendOffer(await offerStream.Reader.ReadAsync());
        }

        private async void ProcessWithdrawal()
        {
            while (await withdrawalStream.Reader.WaitToReadAsync())
                await AppendWithdrawal(await withdrawalStream.Reader.ReadAsync());
        }

        public async void ProcessAccept(string pw)
        {
            try
            {
                if (synching)
                    await synchSource.Task;

                var peer = await peerListener.Accept();

                while (peer != null)
                {
                    try
                    {
                        if (synching)
                            await synchSource.Task;

                        Connect(peer);
                        peer = await peerListener.Accept();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        public async void ProcessReceive(IPeer peer)
        {
            try
            {
                var buffer = new List<string>();
                var msg = await peer.Receive();

                while (msg != null)
                {
                    try
                    {
                        await ProcessMessage(peer, msg, buffer);

                        if (peer.InitSource.Task.IsCompleted)
                            buffer = new List<string>();

                        msg = await peer.Receive();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                        msg = null;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }

            await Disconnect(peer);
        }

        public ChannelReader<Chain> GetChainStream()
        {
            hasChainListner = true;
            return chainUpdateStream.Reader;
        }


        private async Task ProcessMessage(IPeer peer, string msg, List<string> buffer)
        {
            BaseMessage message = null;

            try
            {
                message = await Task.FromResult(JsonConvert.DeserializeObject<BaseMessage>(msg));
            }
            catch (Exception e)
            {
                Log.Error(e.ToString() + "\n" + msg);
                throw;
            }

            if (message.MessageType != MessageType.BlockResponse && message.MessageType != MessageType.HandShake && !peer.InitSource.Task.IsCompleted)
            {
                buffer.Add(msg);
                return;
            }

            try
            {
                switch (message.MessageType)
                {
                    case MessageType.HandShake:
                        var hsk = await Task.FromResult(JsonConvert.DeserializeObject<HandShake>(msg));
                        await Message(peer, hsk);
                        break;
                    case MessageType.Block:
                        await Message(peer, JsonConvert.DeserializeObject<Block>(msg, new JsonSerializerSettings
                        {
                            ObjectCreationHandling = ObjectCreationHandling.Replace
                        }));
                        break;
                    case MessageType.PeerListRequest:
                        Message(peer, JsonConvert.DeserializeObject<PeerListRequest>(msg));
                        break;
                    case MessageType.PeerListResponse:
                        Message(peer, JsonConvert.DeserializeObject<PeerListResponse>(msg));
                        break;
                    case MessageType.Transaction:
                        await Message(peer, JsonConvert.DeserializeObject<Transaction>(msg));
                        break;
                    case MessageType.Offer:
                        await Message(peer, JsonConvert.DeserializeObject<Offer>(msg));
                        break;
                    case MessageType.OfferCancel:
                        await Message(peer, JsonConvert.DeserializeObject<OfferCancel>(msg));
                        break;
                    case MessageType.TransactionPoolRequest:
                        Message(peer, JsonConvert.DeserializeObject<TransactionPoolRequest>(msg));
                        break;
                    case MessageType.TransactionPoolResponse:
                        await Message(peer, JsonConvert.DeserializeObject<TransactionPoolResponse>(msg));
                        break;
                    case MessageType.BlockResponse:
                        var resp = JsonConvert.DeserializeObject<BlockResponse>(msg, new JsonSerializerSettings
                        {
                            ObjectCreationHandling = ObjectCreationHandling.Replace
                        });
                        await Message(peer, resp);
                        if (resp.End && buffer.Count > 0)
                        {
                            foreach (var m in buffer)
                                await ProcessMessage(peer, m, buffer);
                        }
                        break;
                    case MessageType.HeartBeat:
                        await Message(peer, JsonConvert.DeserializeObject<HeartBeat>(msg));
                        break;
                    case MessageType.Withdrawal:
                        await Message(peer, JsonConvert.DeserializeObject<Withdrawal>(msg));
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString() + "\n" + msg);
                throw;
            }
        }

        private void RemoveOldForks(Chain newChain)
        {
            var toRemove = forks.Values.ToList().Where(f => f.Count < (newChain.Count - 3) && f.Timestamp < DateTime.Now.AddMinutes(5)).Select(f => f.Id);
            Chain remove = null;
            foreach (var item in toRemove)
                forks.TryRemove(item, out remove);

            if (toRemove.Count() > 0)
                Log.Debug(toRemove.Count() + " old forks removed.");
        }

        private async void Cleanup(Chain c)
        {
            var persistPoint = c.Cleanup();
            Log.Debug("Current Depth " + c.CurrentBlock.Header.Depth);
            if (persistPoint != null)
            {
                Log.Debug("Persisting and cleanup from depth " + persistPoint.CurrentBlock.Header.Depth);

                if (persistPoint.CurrentBlock != null && !string.IsNullOrEmpty(persistPoint.CurrentBlock.PreviousHashStr))
                {
                    Log.Information("Cleaning persist point " + persistPoint.CurrentBlock.PreviousHashStr);
                    try
                    {
                        await blockRepository.CleanPersistInfo();
                        await balanceRepository.Clean();
                        await bookRepository.Clean();
                    }
                    catch (Exception e)
                    {
                        Log.Error("Failed to clean persist point: " + e.ToString());
                    }

                    Log.Information("Persist point Cleaned.");
                }
            }
            else
                Log.Debug("Nothing to Clean");
        }

        private async void ProcessMining()
        {
            Log.Debug("ProcessMining");
            if (isOracle || synching)
                return;

            Chain preChain = null;

            preChain = chain;

            var newChain = await preChain.StartMining();

            if (newChain != null)
            {
                Log.Debug("Getting miner commit lock.");
                try
                {
                    await commitLocker.EnterWriteLock();
                    Log.Debug("Got miner commit lock.");
                    if (chain.Id == preChain.Id)
                    {
                        chain = newChain;
                        await newChain.UpdatePool();

                        Log.Debug("Chain commited, starting cleanup");
                        Persist(newChain);

                        Log.Debug("removing old forks");
                        RemoveOldForks(newChain);
                        Log.Debug("cleaned old forks");

                        Log.Information("Mined Block, Notifying");

                        Log.Debug("Commit ProcessMining");
                        Notify(newChain.CurrentBlock);
                        if (hasChainListner)
                            await chainUpdateStream.Writer.WriteAsync(newChain);

                        ProcessMining();
                    }
                    else
                    {
                        Log.Warning("Mined Block, but pre chain changed, ignoring...");
                        newChain = null;
                    }
                }
                finally
                {
                    commitLocker.ExitWriteLock();
                }
            }

            Log.Debug("Finish ProcessMining");
        }

        private async Task<bool> ProcessBlock(Block block)
        {
            if (chain.Contains(block) || forks.Values.Any(f => f.Contains(block)))
                return false;

            if (block.Header.Depth < chain.GetLastBlockDepth())
                return false;

            try
            {
                Log.Debug("Adding Block " + block.HashStr);
                Log.Debug("Validating Block...");

                if (!CryptoHelper.IsValidHash(block.Header.Difficult, CryptoHelper.Hash(block.Header.ToString())))
                {
                    Log.Error("Invalid Block Hash");
                    return false;
                }

                Log.Debug("Verifying deposits...");
                foreach (var dp in block.Deposits)
                {
                    dp.BuildHash();
                    if (!await chain.Contains(dp) && !await depositRepository.Contains(dp.HashStr))
                    {
                        Log.Error("Block with invalid deposit");
                        return false;
                    }
                }

                Log.Debug("Deposits Verified, Appending Offers...");
                foreach (var of in block.Offers)
                    await AppendOffer(of);

                foreach (var of in block.OfferCancels)
                    await AppendOfferCancel(of);

                Log.Debug("Offers Appended, Appending Transactions...");

                if (block.TransactionsDictionary.Count > 100)
                {
                    block.Transactions.AsParallel().ForAll(tx =>
                    {
                        if (!string.IsNullOrEmpty(tx.Wallet))
                            AppendTransaction(tx).Wait();
                    });
                }
                else
                {
                    foreach (var tx in block.Transactions)
                    {
                        if (!string.IsNullOrEmpty(tx.Wallet))
                            AppendTransaction(tx).Wait();
                    }
                }

                Log.Debug("Transactions Appended, Appending WIthdrawals...");

                foreach (var wd in block.Withdrawals)
                    await AppendWithdrawal(wd);

                Log.Debug("Withdrawals Appended, Appending Block...");

                var preChain = chain;
                var newChain = await preChain.Append(block);

                if (newChain != null)
                {
                    try
                    {
                        await commitLocker.EnterWriteLock();
                        if (preChain.Id == chain.Id)
                        {
                            Log.Debug("Process block updating pool");
                            chain = newChain;
                            await newChain.UpdatePool();
                            Persist(newChain);
                            Log.Debug("Process block removing old forks");
                            RemoveOldForks(newChain);
                            Log.Debug("Process Block cleaned old forks");

                            Log.Information("Block Appended");
                            if (hasChainListner)
                                await chainUpdateStream.Writer.WriteAsync(newChain);
                            ProcessMining();
                            return true;
                        }
                        else
                        {
                            Log.Warning("Cant Commit Append block, main chain changed....");
                        }
                    }
                    finally
                    {
                        commitLocker.ExitWriteLock();
                    }
                }


                Log.Debug("Can't append on main chain");

                if (forks.Count > 0)
                {
                    Log.Debug("Verifiyng existent forks...");

                    foreach (var f in forks.Values)
                    {
                        Log.Debug("Trying to append on existent forks...");
                        var fork = await f.Append(block);

                        if (fork != null)
                        {
                            Log.Information("Appended on fork");

                            if (fork.Count > chain.Count)
                            {
                                try
                                {
                                    await commitLocker.EnterWriteLock();

                                    chain.StopMining();
                                    Log.Warning("Fork greater then main chain, replacing;");

                                    forks.TryAdd(chain.Id, chain);

                                    preChain = chain;
                                    chain = fork;

                                    Chain result = null;
                                    forks.TryRemove(f.Id, out result);

                                    await fork.UpdatePool();
                                }
                                finally
                                {
                                    commitLocker.ExitWriteLock();
                                }

                                preChain.CancelToken = new CancellationTokenSource();
                                Log.Debug("Chain commited, cleanup started");
                                Persist(fork);
                                RemoveOldForks(fork);
                                if (hasChainListner)
                                    await chainUpdateStream.Writer.WriteAsync(fork);
                                ProcessMining();
                            }
                            else
                            {
                                Log.Debug("Adding new chain to forks.");
                                forks.TryAdd(fork.Id, fork);
                                Chain result = null;
                                forks.TryRemove(f.Id, out result);
                                await fork.UpdatePool();
                            }

                            return true;
                        }

                        Log.Debug("Cant append on fork.");
                    }
                }

                Log.Debug("Trying to fork main chain...");

                if (chain.CanFork(block))
                {
                    Chain fork = null;

                    try
                    {
                        await commitLocker.EnterWriteLock();

                        fork = await chain.Fork(block, new Pool());
                        forks.TryAdd(fork.Id, fork);
                        await fork.Pool.Merge(chain.Pool);
                    }
                    finally
                    {
                        commitLocker.ExitWriteLock();
                    }

                    Log.Warning("Chain forked, Appending forked block");
                    var newFork = await fork.Append(block);

                    forks.TryAdd(newFork.Id, newFork);
                    Chain result = null;
                    forks.TryRemove(fork.Id, out result);

                    await newFork.UpdatePool();
                    Log.Warning("Forked block appended");
                    fork.CancelToken = new CancellationTokenSource();
                    return true;
                }

                Log.Error("Can't process block depth: " + block.Header.Depth.ToString() + "Timestamp: " + block.Timestamp
                + " Nonce: " + block.Header.Nonce + " Current: " + chain.CurrentBlock.Header.Depth
                + " Miner: " + block.Transactions.First().Outputs.First().Wallet);

                return false;
            }
            catch (Exception e)
            {
                Log.Error("Falha ao processar bloco");
                Log.Error(e.ToString());
            }

            return false;
        }

        private async void ProcessPersist()
        {
            Chain retry = null;
            while (retry != null || await PersistStream.Reader.WaitToReadAsync())
            {
                Chain pChain = null;
                if (retry != null)
                    pChain = retry;
                else
                    pChain = await PersistStream.Reader.ReadAsync();

                var gotLock = false;
                try
                {
                    var chainData = pChain.Get(40);
                    if (chainData != null && !chainData.Persisted)
                    {
                        if (chainData.CurrentBlock.PreviousHashStr != "")
                        {
                            await balanceRepository.Save(chainData.TransactManager.Balance);
                            await bookRepository.Save(chainData.BookManager);
                        }

                        await persistLock.EnterWriteLock();
                        gotLock = true;

                        if (chainData != null && !chainData.Persisted)
                        {
                            Log.Debug("Persisting block " + pChain.CurrentBlock.Header.Depth);

                            await blockRepository.Save(chainData.CurrentBlock);

                            await depositRepository.Save(chainData.CurrentBlock.Deposits);
                            await offerRepository.Save(chainData.BookManager.ProcessedOffers);
                            await offerRepository.SaveCancel(chainData.CurrentBlock.OfferCancels);
                            await transactionRepository.Save(chainData.CurrentBlock.Transactions);
                            await withdrawalRepository.Save(chainData.CurrentBlock.Withdrawals);
                            await tradeRepository.Save(chainData.BookManager.Trades);

                            if (chainData.CurrentBlock.PreviousHashStr != "")
                                await blockRepository.SavePersistInfo(new BlockPersistInfo() { BlockHash = chainData.CurrentBlock.HashStr, TimeStamp = DateTime.Now });

                            Cleanup(pChain);
                            retry = null;
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Failed to persist and cleanup block: " + pChain.CurrentBlock.Header.Depth);
                    Log.Error(e.ToString());
                    retry = pChain;
                }
                finally
                {
                    if (gotLock)
                        persistLock.ExitWriteLock();
                }
            }
        }

        public async void Persist(Chain pChain)
        {
            await PersistStream.Writer.WriteAsync(pChain);
        }


        private void Notify(Block block, string except = "")
        {
            foreach (var peer in peerDictionary.Values)
                peer.SendMessage(block);
        }

        private void Notify(Offer offer)
        {
            foreach (var peer in peerDictionary.Values)
                peer.SendMessage(offer);
        }

        private void Notify(OfferCancel offer)
        {
            foreach (var peer in peerDictionary.Values)
                peer.SendMessage(offer);
        }

        private void Notify(Transaction tx)
        {
            foreach (var peer in peerDictionary.Values)
            {
                peer.SendMessage(tx);
            }
        }

        private void Notify(Withdrawal wd)
        {
            foreach (var peer in peerDictionary.Values)
                peer.SendMessage(wd);
        }

        public void Connect(IPeer peer)
        {
            var handShake = new HandShake();
            handShake.AppVersion = "0.0.0.1";

            if (chain.CurrentBlock == null || chain.CurrentBlock.Header.Depth < 20)
                handShake.BlockLength = -1;
            else
                handShake.BlockLength = chain.CurrentBlock.Header.Depth - 20;

            handShake.Version = "1";
            handShake.Address = peer.Address;
            handShake.ListenPort = listenPort;

            peer.SendMessage(handShake);

            ProcessReceive(peer);
        }

        private async void ReconnectTask(Task task, IPeer peer, int count = 0)
        {
            if (peerDictionary.Values.Any(p => p.Equal(peer.Address)))
                return;

            var newPeer = await peerListener.Connect(peer.Address);

            while (newPeer == null && count < 100)
            {
                if (peerDictionary.Values.Any(p => p.Equal(peer.Address)))
                    return;

                count++;
                await Task.Delay(5000 * count).ContinueWith(async t => { newPeer = await peerListener.Connect(peer.Address); });
            }

            if (newPeer != null)
                Connect(newPeer);
        }

        public async Task Disconnect(IPeer sender)
        {
            await Task.Run(() =>
            {
                if (peerDictionary.ContainsKey(sender.Id))
                {
                    if (peerDictionary[sender.Id].Id == sender.Id)
                    {
                        IPeer result;
                        peerDictionary.TryRemove(sender.Id, out result);
                    }

                    Log.Information("Peer " + sender.Address + " disconnected.");

                    sender.Disconnect();
                    Task.Delay(1000).ContinueWith(t => ReconnectTask(t, sender));
                }
                else
                    return;

            });
        }

        public async Task Message(IPeer sender, HandShake handShake)
        {
            this.address = handShake.Address;
            sender.ListenPort = handShake.ListenPort;

            if (handShake.ListenPort == 0)
            {
                Log.Error("Received port 0 from " + sender.Address);
                return;
            }

            if (!peerDictionary.ContainsKey(sender.Id))
            {

                peerDictionary.TryAdd(sender.Id, sender);
                sender.StartHeartBeat();

                Log.Information("Total Peers:" + peerDictionary.Count + " New " + sender.Address);

                await GetBlocks((int)handShake.BlockLength, sender);

                var request = new PeerListRequest();
                sender.SendMessage(request);

                var txPoolRequest = new TransactionPoolRequest();
                sender.SendMessage(txPoolRequest);

            }
            else
            {
                await sender.Disconnect();
            }
        }

        public void Message(IPeer sender, PeerListRequest listRequest)
        {
            var list = peerDictionary.Values.Where(p => p.Address != sender.Address).Select(p => p.Address).ToList();
            sender.SendMessage(new PeerListResponse() { Peers = list });
        }

        public async void Message(IPeer sender, PeerListResponse listResponse)
        {
            if (synching)
                await synchSource.Task;

            foreach (var ad in listResponse.Peers)
            {
                if (!peerDictionary.Values.Any(p => p.Equal(ad)))
                {
                    Log.Information("connecting to peer from peerlist response: " + ad);
                    var peer = await peerListener.Connect(ad);
                    if (peer != null)
                        Connect(peer);
                }
            }

            Log.Information("Finished trying new peers from: " + sender.Address);
        }

        public async void Message(IPeer sender, TransactionPoolRequest txPool)
        {
            if (!sender.InitSource.Task.IsCompleted)
                sender.InitSource.Task.Wait();

            sender.SendMessage(new TransactionPoolResponse() { TransactionPool = await ListTransactionsPool(0) });
        }

        public async Task Message(IPeer sender, TransactionPoolResponse txPool)
        {
            if (!sender.InitSource.Task.IsCompleted)
                await sender.InitSource.Task;

            foreach (var tx in txPool.TransactionPool)
                await AppendTransaction(tx);
        }

        public async Task Message(IPeer sender, Transaction tx)
        {
            if (!sender.InitSource.Task.IsCompleted)
                await sender.InitSource.Task;

            PushTransaction(tx);
        }

        public async Task Message(IPeer sender, Offer of)
        {
            if (!sender.InitSource.Task.IsCompleted)
                await sender.InitSource.Task;
            PushOffer(of);
        }

        public async Task Message(IPeer sender, OfferCancel of)
        {
            if (!sender.InitSource.Task.IsCompleted)
                await sender.InitSource.Task;
            await AppendOfferCancel(of);
        }

        public async Task Message(IPeer sender, Withdrawal wd)
        {
            if (!sender.InitSource.Task.IsCompleted)
                await sender.InitSource.Task;

            PushWithdrawal(wd);
        }

        public async Task Message(IPeer sender, BlockResponse blockResponse)
        {
            Log.Debug("Received block message");
            foreach (var block in blockResponse.Blocks)
            {
                if (await ProcessBlock(block))
                {
                    Log.Information("Received newer block");
                    Notify(block);
                }
            }

            if (blockResponse.End)
            {
                sender.InitSource.SetResult(null);
                synchWatch.Start();
            }
        }

        private Stopwatch synchWatch = new Stopwatch();

        public async Task Message(IPeer sender, Block block)
        {
            await sender.InitSource.Task;

            await blockStream.Writer.WriteAsync(block);
        }

        public async Task Message(IPeer sender, HeartBeat heartBeat)
        {
            await Task.CompletedTask;
        }

        public async Task<Deposit> GetDepositById(string id)
        {
            Deposit result = await chain.Pool.GetDeposit(id);

            if (result == null)
                result = chain.GetDeposit(id);

            if (result == null)
                result = await depositRepository.Get(id);

            return result;
        }

        public async Task<Deposit> GetDepositByTxHash(string txHash)
        {
            Deposit result = null;
            var deposits = await chain.GetAllDeposits();
            result = deposits.FirstOrDefault(t => t.TxHash == txHash);

            if (result == null)
                result = await depositRepository.GetByTxHash(txHash);

            return result;
        }

        public async Task<IList<Deposit>> ListDeposits(int size)
        {
            var result = await chain.GetAllDeposits();

            if (result.Count < size)
            {
                var dbResult = await depositRepository.GetLast(size - result.Count);
                dbResult.AddRange(result);
                result = dbResult;
            }

            return result.Take(size).ToList();
        }

        public async Task<List<Deposit>> ListDepositsPool(int size)
        {
            return await chain.Pool.DepositPool.Take(size);
        }

        public async Task<IList<Deposit>> ListDepositsByTargetWallet(string wallet, int size)
        {
            var result = new List<Deposit>();

            var pool = (await chain.GetAllDeposits()).Where(w => w.TargetWallet == wallet);

            if (pool.Count() >= size)
                return pool.Take(size).ToList();

            result.AddRange(pool);
            size = size - result.Count;

            result.AddRange(await depositRepository.GetByTarget(wallet, size));

            return result;
        }

        public async Task<Offer> GetOfferById(string id)
        {
            Offer result = await chain.Pool.GetOffer(id);

            if (result == null)
                result = chain.BookManager.GetOpenOffer(id);

            if (result == null)
                result = chain.GetProcessedOffer(id);

            if (result == null)
                result = await offerRepository.Get(id);

            return result;
        }

        public async Task<IList<Offer>> ListOffers(int size)
        {
            var result = await chain.GetAllOffers();

            if (result.Count < size)
            {
                var dbResult = await offerRepository.GetLast(size - result.Count);
                dbResult.AddRange(result);
                result = dbResult;
            }
            else
                return result.Take(size).ToList();

            return result;
        }

        public async Task<IList<Offer>> ListOffersPool(int size)
        {
            return await chain.Pool.OfferPool.Take(size);
        }

        public async Task<IList<Offer>> ListOffersByWallet(string wallet, int size)
        {
            var result = new List<Offer>();

            var offers = (await chain.Pool.OfferPool.GetPool()).Where(w => w.Wallet == wallet).ToList();

            if (offers.Count() >= size)
                return offers.Take(size).ToList();

            result.AddRange(offers);
            size = size - result.Count;

            offers = chain.BookManager.GetOpenOffers(wallet);

            if (offers.Count() >= size)
            {
                result.AddRange(offers.Take(size));
                return result;
            }

            result.AddRange(offers);
            size = size - offers.Count;

            offers = chain.GetProcessedOffers(wallet);

            if (offers.Count() >= size)
            {
                result.AddRange(offers.Take(size));
                return result;
            }

            result.AddRange(offers);
            size = size - offers.Count;

            result.AddRange(await offerRepository.GetByWallet(wallet, size));

            return result;
        }

        public async Task<IList<Offer>> ListOpenOffersByWallet(string wallet, string reference, int size)
        {
            var result = new List<Offer>();
            var referenceBook = chain.GetBook(reference);

            var offers = (await chain.Pool.OfferPool.GetPool()).Where(w => w.Wallet == wallet).ToList();

            if (offers.Count() >= size)
                return offers.Take(size).Select(o => o.Convert(referenceBook.LastPrice)).ToList();

            result.AddRange(offers.Select(o => o.Convert(referenceBook.LastPrice)));
            size = size - result.Count;

            offers = chain.BookManager.GetOpenOffers(wallet);

            result.AddRange(offers.Take(size).Select(o => o.Convert(referenceBook.LastPrice)));
            return result;
        }

        public async Task<bool> AppendOffer(Offer of)
        {
            if (of.Timestamp < chain.GetLastBlockTime() || of.Timestamp > DateTime.Now.ToUniversalTime())
                return false;

            if (!CryptoHelper.IsValidBizancAddress(of.Wallet))
            {
                Log.Error("Received offer with invalid bizanc address");
                Log.Error(of.ToString());
                return false;
            }

            if (!CryptoHelper.IsValidSignature(of.ToString(), of.Wallet, of.Signature))
            {
                Log.Error("Offer with invalid signature");
                Log.Error(of.ToString());
                return false;
            }

            of.BuildHash();
            if (!await chain.Contains(of) && await chain.Append(of))
            {
                foreach (var f in forks.Values)
                    await f.Append(of);

                Notify(of);
                return true;
            }

            return false;
        }

        public async Task<bool> AppendOfferCancel(OfferCancel of)
        {
            if (of.Timestamp < chain.GetLastBlockTime() || of.Timestamp > DateTime.Now.ToUniversalTime())
                return false;

            if (!CryptoHelper.IsValidBizancAddress(of.Wallet))
            {
                Log.Error("Received offer cancel with invalid bizanc address");
                Log.Error(of.ToString());
                return false;
            }

            of.BuildHash();
            if (!await chain.Contains(of))
            {
                if (!CryptoHelper.IsValidSignature(of.ToString(), of.Wallet, of.Signature))
                {
                    Log.Error("Offer Cancel with invalid signature");
                    Log.Error(of.ToString());
                    return false;
                }

                if (await chain.Append(of))
                {
                    foreach (var f in forks.Values)
                        await f.Append(of);

                    Notify(of);
                    return true;
                }
            }

            return false;
        }

        private async Task AppendDeposit(Deposit deposit)
        {
            if (!CryptoHelper.IsValidBizancAddress(deposit.TargetWallet))
            {
                Log.Error("Received deposit with invalid bizanc address");
                Log.Error(deposit.ToString());
                return;
            }

            deposit.BuildHash();
            if (!await chain.Contains(deposit))
                if (!await depositRepository.Contains(deposit.HashStr))
                {
                    if (await chain.Append(deposit))
                    {
                        foreach (var f in forks.Values)
                            await f.Append(deposit);
                    }
                }
        }

        private async Task AppendWithdraw(WithdrawInfo withdraw)
        {
            if (!await withdrawInfoRepository.Contains(withdraw.HashStr))
                await withdrawInfoRepository.Save(withdraw);
        }

        public async Task<Transaction> GetTransationById(string id)
        {
            Transaction result = await chain.Pool.GetTransaction(id);

            if (result == null)
                result = chain.GetTransaction(id);

            if (result == null)
                result = await transactionRepository.Get(id);

            return result;
        }

        public async Task<IList<Transaction>> ListTransactions(int size)
        {
            var result = await chain.GetAllTransactions();

            if (result.Count < size)
            {
                var dbResult = await transactionRepository.GetLast(size - result.Count);
                dbResult.AddRange(result);
                result = dbResult;
            }

            return result.Take(size).ToList();
        }

        public async Task<List<Transaction>> ListTransactionsPool(int size)
        {
            return await chain.Pool.TransactionPool.Take(size);
        }

        public async Task<IList<Transaction>> ListTransactionsBySourceWallet(string wallet, int size)
        {
            var result = new List<Transaction>();

            var txs = (await chain.GetAllTransactions()).Where(t => t.Wallet == wallet);

            if (txs.Count() >= size)
                return txs.ToList();

            result.AddRange(txs);
            size = size - result.Count;

            result.AddRange(await transactionRepository.GetBySource(wallet, size));

            return result;
        }

        public async Task<IList<Transaction>> ListTransactionsByTargetWallet(string wallet, int size)
        {
            var result = new List<Transaction>();

            var txs = (await chain.GetAllTransactions()).Where(t => t.Outputs.Any(o => o.Wallet == wallet));

            if (txs.Count() >= size)
                return txs.ToList();

            result.AddRange(txs);
            size = size - result.Count;

            result.AddRange(await transactionRepository.GetByTarget(wallet, size));

            return result;
        }

        public async void AppendTransactionAsync(Transaction tx)
        {
            await AppendTransaction(tx);
        }

        public async Task<bool> AppendTransaction(Transaction tx)
        {
            if (tx.Timestamp < chain.GetLastBlockTime() || tx.Timestamp > DateTime.Now.ToUniversalTime())
                return false;

            if (!CryptoHelper.IsValidBizancAddress(tx.Wallet)
                || tx.Outputs.Any(o => !CryptoHelper.IsValidBizancAddress(o.Wallet)))
            {
                Log.Error("Received deposit with invalid bizanc address");
                Log.Error(tx.ToString());
                return false;
            }

            if (tx.Outputs.Any(o => o.Size < 0))
            {
                Log.Error("Transaction with invalid output");
                return false;
            }

            if (!CryptoHelper.IsValidSignature(tx.ToString(), tx.Wallet, tx.Signature))
            {
                Log.Error("Transaction with invalid signature");
                Log.Error(tx.ToString());
                return false;
            }

            tx.BuildHash();
            if (!await chain.Contains(tx) && await chain.Append(tx))
            {
                foreach (var f in forks.Values)
                    await f.Append(tx);

                Notify(tx);
                return true;
            }

            return false;
        }

        public async Task<Withdrawal> GetWithdrawalById(string id)
        {
            Withdrawal result = await chain.Pool.GetWithdrawal(id);

            if (result == null)
                result = chain.GetWithdrawal(id);

            if (result == null)
                result = await withdrawalRepository.Get(id);

            return result;
        }

        public async Task<WithdrawInfo> GetWithdrawInfoById(string id)
        {
            return await withdrawInfoRepository.Get(id);
        }

        public async Task<IList<Withdrawal>> ListWithdrawals(int size)
        {
            var result = await chain.GetAllWithdrawals();

            if (result.Count < size)
            {
                var dbResult = await withdrawalRepository.GetLast(size - result.Count);
                dbResult.AddRange(result);
                result = dbResult;
            }

            return result.Take(size).ToList();
        }

        public async Task<IList<Withdrawal>> ListWithdrawals(int size, int skip)
        {
            var result = await chain.GetAllWithdrawals(skip);

            return result.Take(size).ToList();
        }

        public async Task<List<Withdrawal>> ListWithdrawalsPool(int size)
        {
            return await chain.Pool.WithdrawalPool.Take(size);
        }

        public async Task<IList<Withdrawal>> ListWithdrawalsBySourceWallet(string wallet, int size)
        {
            var result = await chain.GetAllWithdrawals();
            result = result.Where(w => w.SourceWallet == wallet).ToList();

            if (result.Count >= size)
                return result.Take(size).ToList();

            size = size - result.Count;

            result.AddRange(await withdrawalRepository.GetBySource(wallet, size));

            return result;
        }

        public async Task<IList<Withdrawal>> ListWithdrawalsByTargetWallet(string wallet, int size)
        {
            var result = await chain.GetAllWithdrawals();
            result = result.Where(w => w.TargetWallet == wallet).ToList();

            if (result.Count >= size)
                return result.Take(size).ToList();

            size = size - result.Count;

            result.AddRange(await withdrawalRepository.GetByTarget(wallet, size));

            return result;
        }

        public async Task<bool> AppendWithdrawal(Withdrawal wd)
        {
            if (wd.Timestamp < chain.GetLastBlockTime() || wd.Timestamp > DateTime.Now.ToUniversalTime())
                return false;

            if (!CryptoHelper.IsValidBizancAddress(wd.SourceWallet))
            {
                Log.Error("Received withdraw with invalid bizanc address");
                Log.Error(wd.ToString());
                return false;
            }

            if (wd.Asset == "BTC")
            {
                if (!CryptoHelper.IsValidBitcoinAddress(wd.TargetWallet))
                {
                    Log.Error("!!! Withdrawal with invalid target wallet !!!");
                    Log.Error(wd.ToString());
                    return false;
                }
            }
            else
            {
                if (!CryptoHelper.IsValidEthereumAddress(wd.TargetWallet))
                {
                    Log.Error("!!! Withdrawal with invalid target wallet !!!");
                    Log.Error(wd.ToString());
                    return false;
                }
            }

            if (!CryptoHelper.IsValidSignature(wd.ToString(), wd.SourceWallet, wd.Signature))
            {
                Log.Error("!!! Withdrawal with invalid signature !!!");
                Log.Error(wd.ToString());
                return false;
            }

            wd.BuildHash();
            if (!await chain.Contains(wd) && await chain.Append(wd))
            {
                foreach (var f in forks.Values)
                    await f.Append(wd);

                Notify(wd);
                return true;
            }

            return false;
        }

        public async Task<IList<Block>> ListBlocks(int size)
        {
            return await GetBlocks(chain.CurrentBlock.Header.Depth - size);
        }

        public async Task<IList<Block>> ListBlocksFromDepth(long depth)
        {
            return await GetBlocks(depth);
        }

        public IList<string> ListPeers()
        {
            return peerDictionary.Values.Select(p => p.Address).ToList();
        }

        public async Task<IDictionary<string, decimal>> GetBalance(string address)
        {
            return await Task.Run(() =>
            {
                var balance = chain.GetBalance(address);
                var result = balance.ToDictionary(i => i.Key, i => i.Value);

                return balance;
            });
        }

        public async Task<Block> GetBlockByHash(string hash)
        {
            var result = chain.GetBlocksNewToOld().FirstOrDefault(b => b.HashStr.SequenceEqual(hash));

            if (result == null)
                result = await blockRepository.Get(hash);

            return result;
        }

        public async Task<List<Block>> GetBlocks(long offSet)
        {
            try
            {
                var result = new List<Block>();
                var chainBlocks = chain.GetBlocksOldToNew();
                if (chainBlocks.Count == 0)
                    return result;

                var blocksToSend = chainBlocks.Where(b => b.Header.Depth >= offSet).OrderBy(b => b.Header.Depth).ToList();

                if (blocksToSend.Count > 0 && offSet < blocksToSend.First().Header.Depth)
                {
                    var dbBlocks = (await blockRepository.Get(offSet, blocksToSend.First().Header.Depth - 1));

                    while (await dbBlocks.Reader.WaitToReadAsync())
                        result.Add(await dbBlocks.Reader.ReadAsync());

                    result.AddRange(blocksToSend);

                    return result;
                }
                else
                    return blocksToSend;
            }
            catch (Exception e)
            {
                Log.Error("GetBlocks Faiiled: " + e.ToString());
            }
        }

        public async Task GetBlocks(long offSet, IPeer sender)
        {
            var blockResponse = new BlockResponse();
            var chainBlocks = chain.GetBlocksOldToNew();
            if (chainBlocks.Count == 0)
            {
                blockResponse.End = true;
                sender.SendMessage(blockResponse);
                return;
            }

            Channel<Block> dbBlocks = null;
            List<Block> blocksToSend = new List<Block>();

            try
            {
                await persistLock.EnterReadLock();

                blocksToSend = chainBlocks.Where(b => b.Header.Depth >= offSet).OrderBy(b => b.Header.Depth).ToList();

                if (blocksToSend.Count > 0 && offSet < blocksToSend.First().Header.Depth)
                {
                    dbBlocks = (await blockRepository.Get(offSet));
                }
            }
            finally
            {
                persistLock.ExitReadLock();
            }

            if (dbBlocks != null)
            {
                while (await dbBlocks.Reader.WaitToReadAsync())
                {
                    blockResponse.Blocks.Add(await dbBlocks.Reader.ReadAsync());

                    if (blockResponse.Blocks.Count == 10)
                    {
                        sender.SendMessage(blockResponse);
                        blockResponse = new BlockResponse();
                    }
                }
            }

            blockResponse.Blocks.AddRange(blocksToSend);
            sender.SendMessage(blockResponse);

            if (blocksToSend.Count > 0 && blocksToSend.Last().Header.Depth < chain.CurrentBlock.Header.Depth)
                await GetBlocks(blocksToSend.Last().Header.Depth, sender);
            else
                sender.SendMessage(new BlockResponse() { End = true });
        }

        private async void PushTransaction(Transaction tx)
        {
            await transactionStream.Writer.WriteAsync(tx);
        }

        private async void PushOffer(Offer of)
        {
            await offerStream.Writer.WriteAsync(of);
        }

        private async void PushWithdrawal(Withdrawal wd)
        {
            await withdrawalStream.Writer.WriteAsync(wd);
        }

        public async Task<OfferBook> GetOfferBook(string asset, string reference)
        {
            return await Task<OfferBook>.Run(() =>
            {
                var referenceBook = chain.GetBook(reference);
                OfferBook result = chain.GetBook(asset);

                result = result.Convert(referenceBook.LastPrice);

                return result;
            });
        }

        public async Task<IList<Trade>> ListTrades(string asset, string reference, DateTime from)
        {
            var trades = await tradeRepository.List(asset, from);
            var referenceBook = chain.GetBook(reference);
            trades.AddRange(chain.GetTrades(asset));

            return trades.Select(t => t.Convert(referenceBook.LastPrice)).ToList();
        }

        public async Task<List<Candle>> GetCandle(string asset, string reference, DateTime from, CandlePeriod period = CandlePeriod.minute_1)
        {
            var candle = new Candle();
            var candles = new List<Candle>();

            var trades = (await ListTrades(asset, reference, from)).OrderBy(t => t.Timestamp);

            if (trades.Count() == 0)
                return new List<Candle>() { candle };

            foreach (var trade in trades)
            {
                if (trade.Timestamp < candle.Date)
                {
                    candle.Close = trade.Price;
                    if (trade.Price > candle.High) { candle.High = trade.Price; }
                    if (trade.Price < candle.Low || candle.Low == 0) { candle.Low = trade.Price; }
                    candle.Volume += trade.Quantity * trade.Price;
                }
                else
                {
                    candle = new Candle();
                    candles.Add(candle);
                    candle.Date = trade.Timestamp.AddSeconds(60 * (int)period - trade.Timestamp.Second);
                    candle.Date = candle.Date.AddTicks(-(candle.Date.Ticks % TimeSpan.TicksPerSecond));
                    candle.Open = trade.Price;
                    candle.High = trade.Price;
                    candle.Low = trade.Price;
                    candle.Close = trade.Price;
                    candle.Volume += trade.Quantity * trade.Price;
                }
            }

            return candles;
        }

        public async Task<List<Quote>> GetQuotes(string reference)
        {
            var result = new List<Quote>();

            foreach (var book in chain.BookManager.Dictionary.Values)
            {
                var quote = new Quote();
                var candle = (await GetCandle(book.Asset, reference, DateTime.Now.AddHours(-24), CandlePeriod.day_1)).Last();
                quote.Asset = book.Asset;
                quote.LastPrice = candle.Close;
                quote.High = candle.High;
                quote.Low = candle.Low;
                quote.Open = candle.Open;
                quote.Volume = candle.Volume;

                result.Add(quote);
            }

            return result.OrderByDescending(q => q.Volume).ToList();
        }

        public List<Asset> GetAssets()
        {
            var assets = chain.BookManager.Dictionary.Values.
            Select(b => new Asset()
            {
                Id = b.Asset,
                LastPrice = b.LastPrice,
                BestBid = b.BestBid,
                BestAsk = b.BestAsk
            }).ToList();

            assets.Add(new Asset
            {
                Id = "BIZ",
                LastPrice = 1,
                BestBid = 1,
                BestAsk = 1
            });

            return assets;
        }

        public async Task<Stats> GetStats()
        {
            var stats = await blockRepository.GetBlockStats();
            stats.Blocks.LastBlockTime = chain.CurrentBlock.Timestamp;
            var chainBlocks = chain.GetBlocksOldToNew();
            if (stats.Blocks.TotalCount == 0)
                stats.Blocks.FirstBlockTime = chainBlocks.First().Timestamp;
            stats.Blocks.TotalCount += chainBlocks.Count;
            stats.Blocks.TotalOpCount += chainBlocks.Sum(b => b.DepositsDictionary.Count + b.OffersDictionary.Count + b.TransactionsDictionary.Count + b.WithdrawalsDictionary.Count);
            stats.Deposits.Total += chainBlocks.Sum(b => b.DepositsDictionary.Count);
            stats.Transactions.Total += chainBlocks.Sum(b => b.TransactionsDictionary.Count);
            stats.Offers.Total += chainBlocks.Sum(b => b.OffersDictionary.Count);
            stats.Withdrawals.Total += chainBlocks.Sum(b => b.WithdrawalsDictionary.Count);

            var totalSeconds = (stats.Blocks.LastBlockTime - stats.Blocks.FirstBlockTime).TotalSeconds;

            stats.Deposits.Pool = await chain.Pool.DepositPool.Count();
            stats.Deposits.AvgBlock = ((double)stats.Deposits.Total / stats.Blocks.TotalCount);
            stats.Deposits.AvgSecond = ((double)stats.Deposits.Total / totalSeconds);

            stats.Offers.Pool = await chain.Pool.OfferPool.Count();
            stats.Offers.AvgBlock = ((double)stats.Offers.Total / stats.Blocks.TotalCount);
            stats.Offers.AvgSecond = ((double)stats.Offers.Total / totalSeconds);

            stats.Transactions.Pool = await chain.Pool.TransactionPool.Count();
            stats.Transactions.AvgBlock = ((double)stats.Transactions.Total / stats.Blocks.TotalCount);
            stats.Transactions.AvgSecond = ((double)stats.Transactions.Total / totalSeconds);

            stats.Withdrawals.Pool = await chain.Pool.WithdrawalPool.Count();
            stats.Withdrawals.AvgBlock = ((double)stats.Withdrawals.Total / stats.Blocks.TotalCount);
            stats.Withdrawals.AvgSecond = ((double)stats.Withdrawals.Total / totalSeconds);

            return stats;
        }
    }
}