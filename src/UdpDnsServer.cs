using Serilog;
using Makaretu.Dns;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using Makaretu.Dns.Resolving;

namespace Catalyst.Node.Dfs
{
    /// <summary>
    ///   A DNS name server.
    /// </summary>
    /// <remarks>
    ///   Responds to a UDP DNS question using the <see cref="Resolver"/>.
    ///   <para>
    ///   Set the properties and then call the <see cref="Start"/> method.
    ///   </para>
    /// </remarks>
    class UdpDnsServer : IDisposable
    {
        ConcurrentDictionary<string, Message> outstandingRequests = new ConcurrentDictionary<string, Message>();
        List<UdpClient> listeners = new List<UdpClient>();

        /// <summary>
        ///   Creates a new instance of the <see cref="UdpDnsServer"/>.
        /// </summary>
        public UdpDnsServer()
        {
        }

        /// <summary>
        ///   Creates a new instance of the <see cref="UdpDnsServer"/> with
        ///   the specified DNS master file.
        /// </summary>
        /// <param name="zone">
        ///   The path to the DNS master file.
        /// </param>
        public UdpDnsServer(string zone)
        {
            var catalog = new Catalog();
            using (var reader = new StreamReader(zone))
            {
                catalog.Include(new PresentationReader(reader), authoritative: true);
            }

            Resolver = new NameServer { Catalog = catalog };
        }

        /// <summary>
        ///   Something that can resolve a DNS query.
        /// </summary>
        /// <value>
        ///   A client to a recursive DNS Server.
        /// </value>
        public IResolver Resolver { get; set; }

        /// <summary>
        ///   The port to listen to.
        /// </summary>
        /// <value>
        ///   Defaults to 53.
        /// </value>
        public int Port { get; set; } = 5333;

        /// <summary>
        ///   Start the DNS server.
        /// </summary>
        public void Start()
        {
            foreach (var address in Addresses)
            {
                var endPoint = new IPEndPoint(address, Port);
                var listener = new UdpClient(endPoint);
                listeners.Add(listener);
                ReadRequestsAsync(listener);
            }

            Log.Information("started DNS server");
        }

        /// <summary>
        ///   The addresses of the server.
        /// </summary>
        /// <value>
        ///   Defaults to <see cref="IPAddress.Any"/> and <see cref="IPAddress.IPv6Any"/>.
        /// </value>
        public IEnumerable<IPAddress> Addresses { get; set; } = new[]
        {
            IPAddress.Any,
            IPAddress.IPv6Any
        };

        public void Dispose()
        {
            foreach (var listener in listeners)
            {
                listener.Dispose();
            }
            listeners.Clear();
        }

        private async Task ReadRequestsAsync(UdpClient listener)
        {
            Log.Information($"Listening on {listener.Client.LocalEndPoint}");

            while (true)
            {
                try
                {
                    var request = await listener.ReceiveAsync().ConfigureAwait(false);
                    ProcessAsync(request, listener);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // eat it.
                }
                catch (Exception e)
                {
                    Log.Error(e, "listener failer");
                }
            }
        }

        private async Task ProcessAsync(UdpReceiveResult request, UdpClient listener)
        {
            try
            {
                var query = (Message)new Message().Read(request.Buffer);

                // Check for a duplicate request.
                var qid = query.Id.ToString() + "-" + request.RemoteEndPoint.ToString();
                if (!outstandingRequests.TryAdd(qid, query))
                    return;

                try
                {
                    // Get a response.
                    var response = await Resolver.ResolveAsync(query);
                    var responseBytes = response.ToByteArray();
                    await listener.SendAsync(responseBytes, responseBytes.Length, request.RemoteEndPoint);
                }
                finally
                {
                    outstandingRequests.TryRemove(qid, out Message _);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "process request failure");
            }
        }

    }
}
