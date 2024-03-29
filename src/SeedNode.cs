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
using Autofac;
using Catalyst.Abstractions;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Core.Lib;
using Catalyst.Core.Lib.Cli;
using Catalyst.Core.Modules.Dfs;
using Serilog;

//using TheDotNetLeague.Ipfs.Core.Lib;

namespace Catalyst.Node.Seed
{
    /// <summary>
    ///   A Catalyst seed node.
    /// </summary>
    public class SeedNode
        : ICatalystNode
    {
        private readonly ILogger _logger;
        private readonly IIpfsAdapter _dfs;
        private readonly IFileSystem _fileSystem;

        public SeedNode(
            IIpfsAdapter dfs,
            IFileSystem fileSystem,
            ILogger logger)
        {
            _dfs = dfs;
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

            // Start the seed node, which is just a normal peer node.;
            var peer = await _dfs.Generic.IdAsync();
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

        public static void RegisterNodeDependencies(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterModule(new CoreLibProvider());
            containerBuilder.RegisterModule(new DfsModule());
            containerBuilder.RegisterType<ConsoleUserOutput>().As<IUserOutput>();
            containerBuilder.RegisterType<ConsoleUserInput>().As<IUserInput>();
            containerBuilder.RegisterType<SeedNode>().As<ICatalystNode>();
        }
    }
}
