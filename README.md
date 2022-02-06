# Cardano Tx Submission Service

A micro-service that accepts Cardano transactions and relays it to [cardano-submit-api](https://github.com/input-output-hk/cardano-node/tree/master/cardano-submit-api). The goal is to intercept the transaction and provide statistics / customization on how to handle the submission flow. 

 ![ADAPH-TX-SUBMIT](/wwwroot/images/tx-submit.png)

 ## System Requirements

 - Linux based operating system is [**recommended**] but other OS can work too.
 - .NET 6
 - PostgreSQL Server
 - cardano-node
 - [cardano-submit-api](https://github.com/input-output-hk/cardano-node/tree/master/cardano-submit-api)
 - [Blockfrost API](https://blockfrost.io/) free account

 ## Installing dependencies

### Assumptions

This guide assumes:
- That you have compiled `cardano-node` and fully synced to your intended network.
- That you have compiled `cardano-submit-api`, It is compiled along when you do `cabal build all` the `cardano-node` repository.
- That you have a running `postgresql` instance.
- Signed up for a free `Blockfrost API` account for the intended network.

### .NET 6 SDK

#### dependencies
```bash
sudo apt install -y libc6 libgcc1 libgssapi-krb5-2 libicu67 libssl1.1 libstdc++6 zlib1g build-essential
```

#### Install .NET ***6.0.101***

```bash
mkdir -p $HOME/dotnet && tar zxf dotnet-sdk-6.0.101-linux-x64.tar.gz -C $HOME/dotnet
# Add this to your .bashrc or .zshrc
export DOTNET_ROOT=$HOME/dotnet
export PATH=$PATH:$HOME/dotnet
```

#### Check if .NET is installed properly

```bash
// reload shell
exec bash
//or
exec zsh
// check version
dotnet --version
> 6.0.101
```

#### Running `cardano-submit-api`

```bash
cardano-submit-api --config rest-config.json --mainnet --socket-path db/node.socket
```

The `rest-config.json` file can be downloaded officially from IOHK: https://hydra.iohk.io/build/8111119/download/1/index.html

Once it is running you should see something like this: 

```
Running server on 127.0.0.1:8090
```

### Installing, Building and Running the `TxSubmit` Service

#### Clone the github repository: 

```bash
git clone https://github.com/ADAPhilippines/ADAPH.TxSubmit.git
cd ADAPH.TxSubmit
```

#### Edit the configuration
Find the file `appsettings.sample.json` and rename it to `appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "TxSubmitDb": "<postgresql connection string>"
  },
  "CardanoTxSubmitEndpoint": "http://localhost:8090/api/submit/tx",
  "BlockfrostProjectId": "<blockfrost project id>",
  "CardanoNetwork": "<mainnet or testnet>"
}
```

Replace the values as necessary.

#### Build & Run the code

```bash
start.sh
```

If everything went well, you should see something like this: 

```bash
2|ADAPH.TxSubmit  | info: Microsoft.Hosting.Lifetime[14]
2|ADAPH.TxSubmit  |       Now listening on: http://localhost:1337
2|ADAPH.TxSubmit  | info: Microsoft.Hosting.Lifetime[0]
2|ADAPH.TxSubmit  |       Application started. Press Ctrl+C to shut down.
2|ADAPH.TxSubmit  | info: Microsoft.Hosting.Lifetime[0]
2|ADAPH.TxSubmit  |       Hosting environment: Production
2|ADAPH.TxSubmit  | info: Microsoft.Hosting.Lifetime[0]
```

Congratulations you can now submit transactions via `http://localhost:1337/api/v1.0/tx/submit` 🚀🚀🚀!

You can setup a reverse proxy webserver if you want to serve this publicly.

## Customization

It should be very easy to customize the dashboard to your branding but it is not yet streamlined into the configuration.

Simply open `Pages/Index.razor` and start customizing!

If you have any questions don't hesitate to contact us at: https://adaph.io/contact-us

Do you like our project? feel free to donate some $ADA 😜:

`addr1q8nrqg4s73skqfyyj69mzr7clpe8s7ux9t8z6l55x2f2xuqra34p9pswlrq86nq63hna7p4vkrcrxznqslkta9eqs2nscfavlf`
