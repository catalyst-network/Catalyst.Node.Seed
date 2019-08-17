#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces;
using Catalyst.Common.Interfaces.FileSystem;
using Catalyst.Common.Interfaces.Modules.Consensus;
using Ipfs.CoreApi;
using Serilog;

namespace Catalyst.Dfs.SeedNode
{
    /// <summary>
    ///   An IPFS seed node.
    /// </summary>
    public class SeedNode
        : ICatalystNode
    {
        private readonly ILogger _logger;
        private readonly ICoreApi _ipfs;
        private readonly IFileSystem _fileSystem;

        public SeedNode(
            ICoreApi ipfs,
            IFileSystem fileSystem,
            ILogger logger)
        {
            _ipfs = ipfs;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public IConsensus Consensus => null;

        public async Task RunAsync(CancellationToken ct)
        {
            // Start the DNS server.
            var zone = Path.Combine(_fileSystem.GetCatalystDataDir().FullName, "seed.zone");
            var dns = new UdpDnsServer(zone);
            dns.Start();

            // Start the IPFS seed node, which is just a normal IPFS peer node.;
            var peer = await _ipfs.Generic.IdAsync();
            _logger.Information($"seed node {peer.Id}");
            foreach (var addr in peer.Addresses)
            {
                _logger.Information($"  listening on {addr.WithoutPeerId()}");
            }

            // Wait for a user commanded exit.
            bool exit;
            do
            {
                _logger.Information("Type 'exit' to exit, anything else to continue");
                exit = string.Equals(Console.ReadLine(), "exit", StringComparison.OrdinalIgnoreCase);
            } while (!ct.IsCancellationRequested && !exit);

            _logger.Information("Stopping the seed node");
        }

        public Task StartSockets()
        {
            return Task.CompletedTask;
        }
    }
}
