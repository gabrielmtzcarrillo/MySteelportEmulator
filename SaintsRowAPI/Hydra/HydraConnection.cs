using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Net.Security;
using System.Security.Authentication;

using SaintsRowAPI.Hydra.DataTypes;

namespace SaintsRowAPI.Hydra
{
    public class HydraConnection
    {
        public Socket Socket { get; private set; }
        public SslStream Session { get; private set; }
        public IPAddress IPAddress { get; private set; }

        public System.Diagnostics.Stopwatch Timer { get; private set; }

        public HydraConnection(Socket socket)
        {
            Socket = socket;
        }

        MemoryStream bufferStream = new MemoryStream();
        private byte ReadByte()
        {
            if (bufferStream.Position == bufferStream.Length)
            {
                bufferStream.SetLength(0);
                bufferStream.Position = 0;

                byte[] readBuffer = new byte[4096];
                int bytesRead = Session.Read(readBuffer, 0, readBuffer.Length);

                if (bytesRead <= 0)
                {
                    throw new IOException("TLS stream closed while reading.");
                }

                bufferStream.Write(readBuffer, 0, bytesRead);
                bufferStream.Position = 0;
            }

            byte b = (byte)bufferStream.ReadByte();
            return b;
        }

        public string ReadStringToCrLf()
        {
            StringBuilder sb = new StringBuilder();
            bool done = false;
            bool lastWasCr = false;
            
            while (!done)
            {
                byte b = ReadByte();
                if (lastWasCr && b == 0x0A)
                {
                    return sb.ToString(0, sb.Length - 1);
                }
                
                sb.Append(Encoding.ASCII.GetChars(new byte[] { b })[0]);
                
                lastWasCr = (b == 0x0D);
            }

            return null;
        }
        public byte[] ReadBytes(long count)
        {
            byte[] buffer = new byte[count];
            for (long i = 0; i < count; i++)
            {
                buffer[i] = ReadByte();
            }

            return buffer;
        }

        public void WriteStringAndCrLf(string format, params object[] args)
        {
            string line = String.Format(format, args) + "\r\n";
            byte[] buffer = Encoding.ASCII.GetBytes(line);
            Session.Write(buffer, 0, buffer.Length);
            Session.Flush();
        }

        public void WriteBytes(byte[] bytes)
        {
            Session.Write(bytes, 0, bytes.Length);
            Session.Flush();
        }

        public void Handle()
        {
            NetworkStream networkStream  = null;

            try
            {
                bool loop = true;

                IPEndPoint ipep = (IPEndPoint)Socket.RemoteEndPoint;
                IPAddress = ipep.Address;

                Console.Write("New connection: ", IPAddress);
                Console.WriteLine();

                networkStream = new NetworkStream(Socket, ownsSocket: false);

                try
                {
                    Session = new SslStream(networkStream, leaveInnerStreamOpen: false);
                    Session.AuthenticateAsServer(
                        Certificates.Certificate,
                        clientCertificateRequired: false,
                        enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                        checkCertificateRevocation: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
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
                Session?.Dispose();
                networkStream?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Sesion close EX] " + ex.Message);
            }
            finally
            {
                Socket.Disconnect(false);
                Socket.Dispose();
            }

        }
    }
}
