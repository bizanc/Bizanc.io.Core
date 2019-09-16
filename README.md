
<p align="center"><a href="https://bizanc.io" target="_blank" rel="noopener noreferrer"><img width="100" src="https://bizanc.io/images/bizanc-logo.png" alt="Bizanc logo"></a></p>

# Bizanc Blockchain

<p align="center">
  <a href="bizanc.io"><img src="https://img.shields.io/circleci/project/github/vuejs/vue/dev.svg" alt="Build Status"></a>
  <a href="bizanc.io"><img src="https://img.shields.io/npm/l/vue.svg" alt="License"></a>
  <a href="bizanc.io"><img src="https://img.shields.io/badge/chat-on%20discord-7289da.svg" alt="Chat"></a>
  <br>
</p>

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

## Partners

<p align="center">
    <a href="https://www.blockchainsper.com/" target="_blank"><img src="https://bizanc.io/images/blockchain-insper-logo.png" alt="BT Advogados" width="200px" height="200px" class="img-responsive"></a>
    <a href="https://www.mercuriuscrypto.com/" target="_blank"><img src="https://bizanc.io/images/mercurius-logo.jpeg" alt="blockchain-explorer" width="200px" height="200px" class="img-responsive"></a>
    <a href="https://bndesgaragem.com.br/aprovadas-criacao/" target="_blank"><img src="https://bizanc.io/images/bndes-logo.png" alt="BNDES" width="200px" height="200px" class="img-responsive"></a>
    <a href="https://bitpreco.com/" target="_blank"><img src="https://bizanc.io/images/bitpreco-logo.png" alt="Bitpreco" width="200px" height="200px" class="img-responsive"></a>
    <a href="https://www.theblockchainvip.com/" target="_blank"><img src="https://bizanc.io/images/blockchain-vip-logo.png" alt="blockchain-explorer" width="200px" height="200px" class="img-responsive"></a>
</p>
<p align="center">
    <a href="https://www.clubinvest.io/" target="_blank"><img src="https://bizanc.io/images/clubinvest-logo.jpeg" alt="ClubInvest" width="200px" height="200px" class="img-responsive"></a>
    <a href="https://br-pt.wayra.co/" target="_blank"><img src="https://bizanc.io/images/wayra-logo.png" alt="Wayra"  width="200px" height="200px" class="img-responsive"></a>
    <a href="https://liga.ventures/" target="_blank"><img src="https://bizanc.io/images/liga-ventures-logo.png" alt="Liga Ventures" width="200px" height="200px" class="img-responsive"></a>
    <a href="https://transferoswiss.ch/" target="_blank"><img src="https://bizanc.io/images/transfero-logo.png" alt="Transferoswiss" width="200px" height="200px" class="img-responsive"></a>
    <a href="https://www.brztoken.io/" target="_blank"><img src="https://bizanc.io/images/BRZ-logo.png" alt="BRZtoken" width="200px" height="200px" class="img-responsive"></a>
    
</p>

<p align="center">
    <a href="https://www.impacta.edu.br" target="_blank"><img src="https://bizanc.io/images/faculdade-impacta-logo.png" width="200px" height="200px"  alt="Impacta" class="img-responsive"></a>
    <a href="http://www.dmk3.com.br/" target="_blank"><img src="https://bizanc.io/images/dmk3-logo.png"  width="200px" height="200px"  alt="DMK3" class="img-responsive"></a>
    <a href="https://www.linkedin.com/company/nabukodonosor-in-blockchain-we-trust/about/" target="_blank"><img src="https://bizanc.io/images/nabuko-logo.png" alt="blockchain-explorer" width="200px" height="200px"  class="img-responsive"></a>
    <a href="https://bertoluccitorres.adv.br/" target="_blank"><img src="https://bizanc.io/images/btAdvogados-logo.png" alt="BT Advogados" width="200px" height="200px"  class="img-responsive"></a>
</p>

# Documentation

## Bizanc.io.Core - Quick Start

*PRE-REQUISITS*:
- Docker - Version 18.09 +(Higher)
- Docker Compose - Version 1.18.0 +(Higher)
- Disk Space - 10 Giga (09/09/2019).

***Warning: Windows version 8 or lower, does not work properly with docker, we recommend install VirtualBox and Linux distro, to execute with docker.
*** 

### Docker Instalation

Docker docs: https://docs.docker.com/install/

Docker tutorial: 
- Linux Ubuntu - https://www.digitalocean.com/community/tutorials/how-to-install-and-use-docker-on-ubuntu-18-04

- Linux Centos / Debian -  https://www.digitalocean.com/community/tutorials/how-to-install-and-use-docker-on-ubuntu-18-04

- Windows 10 Desktop - https://hub.docker.com/editions/community/docker-ce-desktop-windows

- Mac Desktop - https://hub.docker.com/editions/community/docker-ce-desktop-mac

Docker Compose tutorial:

```
pip install docker-compose

OR

sudo pip install docker-compose
```

### Download code
Download the code on: https://github.com/bizanc/Bizanc.io.Core

Unzip on select path.

OR

Using git:

```
cd ~/Desktop/myrepo$
git clone https://github.com/bizanc/Bizanc.io.Core.git

```

### Update code

```
cd Bizanc.io.Core
git pull origin master

```
If it fails:

```
git stash
git pull origin master

```

### Settings code

```
cd Bizanc.io.Core/Bizanc.io.Matching.App

```
Open appsettings.json

Add, MinerAddress with PUBLIC_KEY that you want to receive block rewards and fees and save it.

```
{
  "Node": {
    "Network": "betanet",
    "ListenPort": 5556,
    "SeedAddress": "betaseed.bizanc.io",
    "SeedPort": 5556,
    "OracleBTCAddres": "mmP2k8Ybzi6nfmFKJWUQMkxCfTSJ978LNQ",
    "OracleETHAddres": "0x55fC9dA1a76533b1213B528eb7ABf80F71189B40",
    "BTCEndpoint": "http://seed.bizanc.io:24445",
    "ETHEndpoint": "https://rinkeby.infura.io/v3/eb70546acdca4a0daef93322a3edf383",
    "Mine": "true",
    "Threads": 1,
    "ApiEndpoint": "http://0.0.0.0:5001",
    "MinerAddress": "PUBLIC_KEY_ADDRESS"
  }
}

```

## LOCALHOST NODE

### Run - Docker

```
cd Bizanc.io.Core
docker-compose -f docker-compose.yml up -d --build
```

Docker Logs:

``` 
docker logs -f "container_hash_id"
```

Once the docker has been update, into docker logs you will be able to see the logs, starting genesis block, get the initial Private and Public Key with funds and checkout the recents mined blocks

### Stop Run - Docker
```
cd bizancio.core
docker-compose -f docker-compose.yml down
```

### Remove Volumes - Docker

List and Remove:
```
docker volume ls

docker volume rm volume_name volume_name

```

### Troubleshooting Volumes - Docker

```
docker volume rm bizanciocoremaster_biznode_datadir1

Error response from daemon: remove bizanciocoremaster_biznode_datadir1: volume is in use - [b93aaa66fe904eee9046b834e6b9c4c46b13ca211da54c2ba00329e32d9899bb]

```

To solve that issue, shutdown the container.

```
docker-compose -f docker-compose.yml down

docker volume rm bizanciocoremaster_biznode_datadir1

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
- localhost:5001/api
PATH:
- /blocks
- /offerbooks
- /peers
- /transactions
- /wallet
- /withdrawal

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

