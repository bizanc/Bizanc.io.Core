

# Bizancio.Core Ominichain Network

## What is Bizanc Ominichain Network?
Bizanc is a Open Source Descentralized Exchange - DEX that enables instant liquidity between assets anywhere in the world.
Bizanc uses blockchain peer-to-peer technology to operate the booktrading system with no central authority: managing offers, order books, bids, asks, candle (prices, volume), transactions, deposits and withdrawal.

More information: 
website - http://bizanc.io
whitepaper - http://bizanc.io/wp

## What Bizanc Ominichain Network makes possible ?
- Asset Trading
- Exchange White Label
- Asset Tokenization
- Access Underbanked People
- Conversion between crypto and fiatmoney
- Payments, loans and "bank" credit

# Documentation

## LOCALHOST NODE

### Run - Docker

```
git clone https://http://bitbucket.org/leandro_lustosa/bizancio.core.git
cd bizancio.core
docker-compose -f docker-compose.yml up -d --build
```
To test the application locally, the blockchain generates a Public key / Private key with funds, to get those keys follow the instructions bellow: 

``` 
docker logs -f "container_hash_id"
```

Once the docker has been update, into docker logs you will be able to see the logs, starting genesis block, get the initial Private and Public Key with funds and checkout the recents mined blocks

### Stop Run - Docker
```
cd bizancio.core
docker-compose -f docker-compose.yml down
```
### DEBUG Start - VScode

```
git clone https://http://bitbucket.org/leandro_lustosa/bizancio.core.git
cd bizancio.core
```
On VScode, click on Debug (ctrl + shift + D) and click on green flag to start the process

Once the process has been started, into DEBUG CONSOLE you will be able to see the logs, starting genesis block, get the initial Private and Public Key with funds and checkout the recents mined blocks

### DEBUG Stop - VScode
```
cd bizancio.core
```
On VScode, click on Debug (ctrl + shift + D) and click on red flag (shift + f5) to stop the process

### API Docs

To get the complete documentation about Bizanc API, please go to our swagger
bizanc.io/docs

### API URL`s
BASE: 
- localhost:5000/api
PATH:
- /blocks
- /offerbooks
- /peers
- /transactions
- /wallet
- /withdrawal

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details
