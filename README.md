

# Bizanc Blockchain

## What is Bizanc?
Bizanc is an Open Source Fully Descentralized Exchange - DEX that enables instant liquidity between crypto assets anywhere in the world.
Bizanc blockchain was developed from scratch, with native crosschain and trade functionalities.  

More information: 
website - https://bizanc.io
block explorer - https://bizanc.io/explorer
wallet - https://bizanc.io/wallet
market - https://bizanc.io/market
whitepaper - https://bizanc.io/documentos/Bizanc-whitepaper-draft.pdf
API - https://bizanc.io/api
API docs - https://bizanc.io/api-docs/index.html


## What Bizanc Ominichain Network makes possible ?
- Asset Trading
- Exchange White Label
- Integrated Market
- Asset Tokenization
- Bitcoin and Ethereum
- Payments

The platform development is ongoing, testnet is planned to start February 2019.

# Documentation

## LOCALHOST NODE

### Run - Docker

```
git clone https://github.com/bizanc/Bizanc.io.Core.git
cd Bizanc.io.Core
docker-compose -f docker-compose.yml up -d --build
```
To test the application locally, the blockchain generates a Public key / Private key to mine, to get those keys follow the instructions bellow: 

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
git clone https://github.com/bizanc/Bizanc.io.Core.git
cd Bizanc.io.Core
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
https://bizanc.io/api-docs/index.html

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
