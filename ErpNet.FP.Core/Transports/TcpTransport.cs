﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace ErpNet.FP.Core.Transports
{
    /// <summary>
    /// TCP/IP transport.
    /// </summary>
    /// <seealso cref="ErpNet.FP.Core.Transport" />
    public class TcpTransport : Transport
    {
        public override string TransportName => "tcp";

        protected readonly int DefaultPort = 80;

        private readonly IDictionary<string, TcpTransport.Channel?> openedChannels =
            new Dictionary<string, TcpTransport.Channel?>();

        public override IChannel OpenChannel(string address)
        {
            if (openedChannels.ContainsKey(address))
            {
                var channel = openedChannels[address];
                if (channel == null)
                {
                    throw new TimeoutException("disabled due to timeout");
                }
                return channel;
            }
            try
            {
                var (hostName, port) = ParseAddress(address);
                var channel = new Channel(hostName, port);
                openedChannels.Add(address, channel);
                return channel;
            }
            catch (TimeoutException e)
            {
                openedChannels.Add(address, null);
                throw e;
            }
        }

        protected (string, int) ParseAddress(string address)
        {
            var parts = address.Split(':');
            if (parts.Length == 1) return (address, DefaultPort);
            var hostName = parts[0];
            var port = parts.Length > 1 ? int.Parse(parts[1]) : DefaultPort;
            return (hostName, port);
        }

        public class Channel : IChannel
        {
            private readonly TcpClient tcpClient;
            private readonly NetworkStream netStream;

            private string HostName { get; }
            private int Port { get; }

            public string Descriptor => $"{HostName}:{Port}";

            public Channel(string hostName, int port)
            {
                HostName = hostName;
                Port = port;
                tcpClient = new TcpClient(hostName, port);
                netStream = tcpClient.GetStream();
            }

            public void Dispose()
            {
                tcpClient.Close();
                // Closing the tcpClient instance does not close the network stream.
                netStream.Close();
            }

            /// <summary>
            /// Reads data from the tcp connection.
            /// </summary>
            /// <returns>The data which was read.</returns>
            public byte[] Read()
            {
                if (netStream.CanRead)
                {
                    byte[] data = new byte[tcpClient.ReceiveBufferSize];

                    // This method blocks until at least one byte is read.
                    netStream.Read(data, 0, (int)tcpClient.ReceiveBufferSize);
                    return data;
                }
                throw new TimeoutException($"timeout occured while reading from tcp connection {HostName}:{Port}");
            }

            /// <summary>
            /// Writes the specified data to the tcp connection.
            /// </summary>
            /// <param name="data">The data to write.</param>
            public void Write(Byte[] data)
            {
                if (netStream.CanWrite)
                {
                    // This method blocks until data.Length bytes are written.
                    netStream.Write(data, 0, data.Length);
                    return;
                }
                tcpClient.Close();
                netStream.Close();
                throw new TimeoutException($"timeout occured while writing to tcp connection {HostName}:{Port}");
            }
        }

    }
}