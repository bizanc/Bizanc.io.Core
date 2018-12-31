

# Bizanc Blockchain

## What is Bizanc?
Bizanc is a decentralized platform for commercialization of digital assets, operating on a Blockchain architecture, allowing trading of cryptocurrencies such as Bitcoin, Ether and Tokens of the Ethereum network, as well as the issuance of Tokens within the Bizanc network itself. The platform aims to enable, in addition to a market of strictly digital assets, the tokenization and negotiation of conventional tangible and financial assets, such as: bonds, commodities, derivatives, reward and loyalty program points, and other fiduciary currencies. The decentralized structure of the Bizanc network confers a highly resilient, superior environment in availability and security by eliminating single points of failure, and reducing transaction costs compared to conventional solutions. Bizanc aims to provide greater liquidity to cryptocurrencies and accelerate the adoption of decentralized solutions by the market.

More information: 
- website - https://bizanc.io
- block explorer - https://bizanc.io/explorer
- wallet - https://bizanc.io/wallet
- market - https://bizanc.io/market
- whitepaper draft (Not Technical) - https://bizanc.io/documentos/Bizanc-whitepaper-draft.pdf
- API - https://bizanc.io/api
- API docs - https://bizanc.io/api-docs/index.html


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
