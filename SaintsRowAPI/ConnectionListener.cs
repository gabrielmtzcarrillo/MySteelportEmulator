using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.Threading;

using SaintsRowAPI.Hydra;

namespace SaintsRowAPI
{
    public class ConnectionListener
    {
        private const string HydraHostName = "sr3.hydra.agoragames.com";
        private const int HydraPort = 443;

        private Socket ListenSocket;
        public bool IsConnected { get; private set; }

        public ConnectionListener()
        {
            IPAddress[] remoteAddresses = null;
            try
            {
                IPHostEntry host_remote = Dns.GetHostEntry(HydraHostName);
                remoteAddresses = host_remote.AddressList;
            }
            catch (Exception)
            {
                Console.WriteLine("[ERROR] Could not resolve {0}.", HydraHostName);
                Console.WriteLine("        Make sure you have pointed this host to 127.0.0.1 or ::1 in your hosts file.");
                return;
            }

            // Check if the remote host points to this machine
            IPAddress[] localAddresses = GetLocalAddresses();
            IPAddress[] bindCandidates = GetLocalBindCandidates(remoteAddresses, localAddresses);
            bool hasBindCandidates = bindCandidates.Any();

            if (hasBindCandidates)
            {
                SocketException bindException;
                IPAddress boundAddress;
                if (TryBind(bindCandidates, out boundAddress, out bindException))
                {
                    IsConnected = true;
                    Console.WriteLine("Connected IP: " + boundAddress.ToString());
                }
                else if (bindException != null && bindException.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    Console.WriteLine("[ERROR] Port {0} is in use.", HydraPort);
                }
                else if (bindException != null)
                {
                    Console.WriteLine("Can't open port: {0}", bindException.Message);
                }
            }

            if (!hasBindCandidates)
                Console.WriteLine("[ERROR] {0} ({1}) doesn't point to this machine.", HydraHostName, FormatAddresses(remoteAddresses));
        }

        private static IPAddress[] GetLocalAddresses()
        {
            return Dns.GetHostAddresses(Dns.GetHostName())
                .Concat(new[] { IPAddress.Loopback, IPAddress.IPv6Loopback })
                .Where(IsSupportedAddress)
                .Distinct()
                .ToArray();
        }

        private static IPAddress[] GetLocalBindCandidates(IPAddress[] remoteAddresses, IPAddress[] localAddresses)
        {
            HashSet<IPAddress> localAddressSet = new HashSet<IPAddress>(localAddresses);

            return remoteAddresses
                .Where(IsSupportedAddress)
                .Where(address => IPAddress.IsLoopback(address) || localAddressSet.Contains(address))
                .OrderByDescending(IPAddress.IsLoopback)
                .ToArray();
        }

        private static bool IsSupportedAddress(IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
                return true;

            return address.AddressFamily == AddressFamily.InterNetworkV6 && Socket.OSSupportsIPv6;
        }

        private bool TryBind(IPAddress[] bindCandidates, out IPAddress boundAddress, out SocketException bindException)
        {
            boundAddress = null;
            bindException = null;

            foreach (IPAddress bindAddress in bindCandidates)
            {
                Socket socket = new Socket(bindAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (bindAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    socket.DualMode = false;

                try
                {
                    socket.Bind(new IPEndPoint(bindAddress, HydraPort));
                    ListenSocket = socket;
                    boundAddress = bindAddress;
                    return true;
                }
                catch (SocketException sex)
                {
                    bindException = sex;
                    socket.Dispose();
                }
            }

            return false;
        }

        private static string FormatAddresses(IPAddress[] addresses)
        {
            return string.Join(", ", addresses.Select(address => address.ToString()));
        }
        
        private void AcceptCallback(IAsyncResult AR)
        {
            try
            {
                Socket clientSocket = ListenSocket.Accept();

                Console.WriteLine("New connection!");

                HydraConnection connection = new HydraConnection(clientSocket);
                ThreadStart ts = new ThreadStart(connection.Handle);
                Thread t = new Thread(ts)
                {
                    IsBackground = false
                };
                t.Start();
            }
            catch (ObjectDisposedException) // I cannot seem to avoid this (on exit when properly closing sockets)
            {
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Listen()
        {
            if (!IsConnected)
                return;

            ListenSocket.Listen(10);

            Console.WriteLine("Ready for connections.");

            while (true)
            {
                try
                {
                    Socket clientSocket = ListenSocket.Accept();

                    HydraConnection connection = new HydraConnection(clientSocket);
                    ThreadStart ts = new ThreadStart(connection.Handle);
                    Thread t = new Thread(ts)
                    {
                        IsBackground = false
                    };
                    t.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
