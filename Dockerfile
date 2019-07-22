FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build-env
WORKDIR /app
RUN mkdir output

COPY ./ ./
WORKDIR /app/Catalyst.Node/src
RUN dotnet restore

WORKDIR /app/Catalyst.Dfs.SeedNode
RUN dotnet restore

# Copy everything else and build
WORKDIR /app
COPY . ./
RUN dotnet publish Catalyst.Dfs.SeedNode/Catalyst.Dfs.SeedNode.csproj -c Debug -o output

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/sdk:2.2
WORKDIR /app
COPY --from=build-env /app/Catalyst.Dfs.SeedNode/output .
CMD ["dotnet", "Catalyst.Dfs.SeedNode.dll", "--ipfs-password", "test"]