# Catalyst.Dfs.SeedNode
Dfs Seed Node For Catalyst Network

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
