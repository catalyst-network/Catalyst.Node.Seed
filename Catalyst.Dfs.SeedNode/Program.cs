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
using System.Reflection;
using System.Security;
using System.Threading;
using Autofac;
using Autofac.Configuration;
using Autofac.Extensions.DependencyInjection;
using AutofacSerilogIntegration;
using Catalyst.Common.Config;
using Catalyst.Common.Kernel;
using Catalyst.Common.Types;
using Catalyst.Common.FileSystem;
using Catalyst.Common.Interfaces;
using Catalyst.Common.Interfaces.Registry;
using Catalyst.Common.Util;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Catalyst.Dfs.SeedNode
{
    class Options
    {
        [Option('p', "ipfs-password", HelpText = "The password for IPFS.  Defaults to prompting for the password.")]
        public string IpfsPassword { get; set; }

        [Option('o', "overwrite-config", HelpText = "Overwrite the data directory configs.")]
        public bool OverwriteConfig { get; set; }

    }

    /// <summary>
    ///   An IPFS seed node.
    /// </summary>
    /// <remarks>
    ///   A catalyst seed node is a semi-trusted IPFS node that is used
    ///   to find other IPFS nodes in the catalyst private network.
    /// </remarks>
    internal static class Program
    {

        private static readonly Kernel Kernel;
        private static Options _options;

        static Program()
        {
            Kernel = Kernel.Initramfs(default, "Catalyst.SeedNode..log");
            
            AppDomain.CurrentDomain.UnhandledException += Kernel.LogUnhandledException;
            AppDomain.CurrentDomain.ProcessExit += Kernel.CurrentDomain_ProcessExit;
        }
        
        public static int Main(string[] args)
        {
            
            // Parse the arguments.
            Parser.Default
                .ParseArguments<Options>(args)
                .WithParsed(Run);

            return Environment.ExitCode;
        }

        private static void Run(Options options)
        {
            _options = options;
            
            try {

                Kernel.WithDataDirectory()
                    .WithSerilogConfigFile()
                    .WithConfigCopier(new SeedNodeConfigCopier())
                    .WithConfigurationFile("seed.components.json")
                    .BuildKernel(options.OverwriteConfig)
                    .WithPassword(PasswordRegistryTypes.IpfsPassword, options.IpfsPassword)
                    .StartNode();

                Environment.ExitCode = 0;
            }
            catch (Exception e)
            {
                Kernel.Logger.Fatal(e, "Catalyst.SeedNode stopped unexpectedly");
                Environment.ExitCode = 1;
            }
        }

    }
}
