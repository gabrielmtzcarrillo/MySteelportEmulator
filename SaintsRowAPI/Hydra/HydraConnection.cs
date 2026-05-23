using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;

using SaintsRowAPI.Hydra.DataTypes;

namespace SaintsRowAPI.Hydra
{
    public class HydraConnection
    {
        public Socket Socket { get; private set; }
        public TlsServerProtocol Protocol { get; private set; }
        public Stream Stream { get; private set; }
        public IPAddress IPAddress { get; private set; }

        public System.Diagnostics.Stopwatch Timer { get; private set; }

        public HydraConnection(Socket socket)
        {
            Socket = socket;
        }

        private byte ReadByte()
        {
            int b = Stream.ReadByte();
            if (b == -1)
                throw new EndOfStreamException();
            return (byte)b;
        }

        public string ReadStringToCrLf()
        {
            StringBuilder sb = new StringBuilder();
            bool lastWasCr = false;
            
            while (true)
            {
                byte b = ReadByte();
                if (lastWasCr && b == 0x0A)
                {
                    return sb.ToString(0, sb.Length - 1);
                }
                
                sb.Append(Encoding.ASCII.GetChars(new byte[] { b })[0]);
                
                lastWasCr = (b == 0x0D);
            }
        }
        public byte[] ReadBytes(long count)
        {
            byte[] buffer = new byte[count];
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = Stream.Read(buffer, totalRead, (int)count - totalRead);
                if (read == 0)
                    throw new EndOfStreamException();
                totalRead += read;
            }

            return buffer;
        }

        public void WriteStringAndCrLf(string format, params object[] args)
        {
            string line = String.Format(format, args) + "\r\n";
            byte[] buffer = Encoding.ASCII.GetBytes(line);
            Stream.Write(buffer, 0, buffer.Length);
            Stream.Flush();
        }

        public void WriteBytes(byte[] bytes)
        {
            Stream.Write(bytes, 0, bytes.Length);
            Stream.Flush();
        }

        public void Handle()
        {
            try
            {
                bool loop = true;

                IPEndPoint ipep = (IPEndPoint)Socket.RemoteEndPoint;
                IPAddress = ipep.Address;

                Console.Write("New connection: ", IPAddress);
                Console.WriteLine();

                NetworkStream ns = new NetworkStream(Socket, false);
                Protocol = new TlsServerProtocol(ns, new SecureRandom());

                try
                {
                    if (Certificates.BcCertificate == null || Certificates.BcPrivateKey == null)
                    {
                        Console.WriteLine("[SSL Error] Bouncy Castle Certificate or Private Key is NULL!");
                        loop = false;
                    }
                    else
                    {
                        HydraTlsServer tlsServer = new HydraTlsServer(Certificates.BcCertificate, Certificates.BcPrivateKey);
                        Protocol.Accept(tlsServer);
                        Stream = Protocol.Stream;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[SSL Auth EX] " + ex.ToString());
                    loop = false;
                }


                while (loop)
                {
                    HydraRequest request = null;
                    try
                    {
                        request = new HydraRequest(this);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Hydra Request EX] " + ex.Message);
                        break;
                    }

                    Modules.IModule module = null;

                    Console.Write("Module Request: {0}", request.Module);
                    Console.WriteLine();

                    switch (request.Module)
                    {
                        case "feed":
                            {
                                module = new Modules.FeedModule(this);
                                break;
                            }
                        case "profile":
                            {
                                module = new Modules.ProfileModule(this);
                                break;
                            }
                        case "onesite_proxy":
                            {
                                module = new Modules.OnesiteProxyModule(this);
                                break;
                            }
                        case "ugc":
                            {
                                module = new Modules.UgcModule(this);
                                break;
                            }

                        default:
                            {
                                HydraResponse response = new HydraResponse(this);
                                response.StatusCode = 404;
                                response.Status = "File Not Found";
                                response.Payload = new byte[0];
                                response.Send();
                                break;
                            }
                    }

                    if (module != null)
                        module.HandleRequest(request);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Handle EX] " + ex.Message);
            }

            try
            {
                if (Stream != null)
                {
                    Stream.Close();
                }
                else if (Protocol != null)
                {
                    // If handshake didn't complete, Protocol.Close() might throw alerts
                    // We try to close it but swallow TLS specific errors
                    Protocol.Close();
                }
            }
            catch (IOException) { /* Ignore socket reset/closed during alert send */ }
            catch (Exception ex)
            {
                Console.WriteLine("[Session close EX] " + ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                try
                {
                    if (Socket.Connected)
                    {
                        Socket.Disconnect(false);
                    }
                }
                catch { }
                Socket.Dispose();
            }

        }
    }
}
