# Catalyst.Node.Seed
Seed Node For Catalyst Network

# Purpose

## Seeding
A seed node is a well known node that allows other Nodes to discover each other. 

## DNS

The seed node also provides a DNS name server.  The authoritative names are defined in the [seed.zone](config/seed.zone) file;
a standard DNS [master file](https://en.wikipedia.org/wiki/Zone_file).

To test the DNS server, try this

```console
> start dotnet Catalyst.Dfs.SeedNode.dll -p mypassword
> nslookup -q=TXT seed.catalystnetwork. 127.0.0.1
Server:  UnKnown
Address:  127.0.0.1

seed.catalystnetwork    text =

        "peer=0x41437c30317c39322e3230372e337382e3139387c34323036397c3031323334353637383930313233343536373839323232323232323232323232"
```

## Usage

```console
> dotnet Catalyst.Dfs.SeedNode.dll --help
Catalyst.Dfs.SeedNode 0.0.1
Copyright © 2019 AtlasCity.io

  -p, --ipfs-password    The password for IPFS.  Defaults to prompting for the password.
  --help                 Display this help screen.
  --version              Display version information.

## Docker
### To build dockerfile

docker build .

### Tag docker image with has from build step

docker tag $IMG_HASH catalyst.network/catalyst.dfs.seednode  

### Run container with interactive tty

docker run --interactive --tty catalyst.network/catalyst.dfs.seednode:latest

### deploy local devnet docker network

docker-compose -f devnet-docker-compose.yml up -d

### deploy testnet to DO swarm

(on swarm master)
docker stack deploy 
