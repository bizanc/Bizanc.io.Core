pragma solidity 0.5.7;

contract ERC20 {
    function symbol() public returns(string memory);
    function transfer(address to, uint256 value) public returns (bool);
    function allowance(address owner, address spender) public view returns (uint256);
    function transferFrom(address from, address to, uint256 value) public returns (bool);
}

contract MultiAccess {
  event AllowAccessEvent(address indexed _address);
  event DenyAccessEvent(address indexed _address);

  mapping(address => bool) accessAllowed;

  constructor () public {
    accessAllowed[msg.sender] = true;
    emit AllowAccessEvent(msg.sender);
  }

  modifier canAccess() {
    require(accessAllowed[msg.sender]);
    _;
  }

  function isAllowedAccess() view internal {
    require(accessAllowed[msg.sender]);
  }

  function allowAccess(address _address) canAccess public {
    accessAllowed[_address] = true;
    emit AllowAccessEvent(_address);
  }

  function denyAccess(address _address) canAccess public {
    accessAllowed[_address] = false;
    emit DenyAccessEvent(_address);
  }
}

contract BizancioGateway is MultiAccess {
    
    event logDeposit (address from, string destination, uint amount, string asset, address assetId);
    event logWithdrawal (string withdrawHash, address to, address origin, uint amount, string asset, address assetId);
    
    // call to deposit ETH
    // destination: public address in Bizanc.io 
    // msg value will be deposited to destination
    function depositEth (string memory destination) public payable {
        emit logDeposit (msg.sender, destination, msg.value, "ETH", address(0x0));
    }
    
    // call to deposit tokens
    // destination: public address in Bizanc.io 
    // token: token address
    // the whole allowance to this contract will be deposited to destination
    function depositERC20 (string memory destination, address token) public {
        ERC20 tokenContract = ERC20(token);
        uint value = tokenContract.allowance(msg.sender, address(this));
        require(tokenContract.transferFrom(msg.sender, address(this), value));
        emit logDeposit (msg.sender, destination, value,  tokenContract.symbol(), token);
    }
    
    // call to send ETH or tokens to users
    // only owner can call the function
    // to: Ethereum address to receive funds
    // origin: public address in Bizanc.io 
    // value: value in wei
    function withdrawEth (string memory withdrawHash, address payable to, address origin, uint value) public canAccess {
        if(address(to).send(value))
            emit logWithdrawal (withdrawHash, to, origin, value, "ETH", address(0x0));
    }
    
    // call to send tokens to users
    // only owner can call the function
    // to: Ethereum address to receive funds
    // origin: public address in Bizanc.io 
    // value: value in wei
    // token: token address
    function withdrawERC20 (string memory withdrawHash, address to, address origin, uint value, address token ) public canAccess {
        ERC20 tokenContract = ERC20(token);
        tokenContract.transfer(to, value);
        emit logWithdrawal (withdrawHash, to, origin, value, tokenContract.symbol(), token);
    }
    
}