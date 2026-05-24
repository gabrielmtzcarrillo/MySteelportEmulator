using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SaintsRowAPI;

namespace SaintsRowAPI.ConsoleHost
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "My Steelport Emulator";
            Console.WriteLine("My Steelport Emulator {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(2));
            Console.WriteLine("https://www.saintsrowmods.com/forum/threads/my-steelport-emulator.17361/");
            Console.WriteLine();

            try
            {
                Certificates.Load();
            }
            catch (CertificateLoadException ex)
            {
                Console.WriteLine("[Certificate Error] " + ex.Message);

                if (ex.InnerException != null)
                    Console.WriteLine("    Inner exception: {0}: {1}", ex.InnerException.GetType().Name, ex.InnerException.Message);

                Console.WriteLine("    Startup aborted before opening the listener.");
                Console.WriteLine("    Check the embedded PKCS#12 payload in SaintsRowAPI/Certificates.cs and its password.");
                Console.WriteLine();
                Environment.ExitCode = 1;
                return;
            }

            ConnectionListener Listener = new ConnectionListener();
            Listener.Listen();

            while (Listener.IsConnected)
            {
                System.Threading.Thread.Sleep(1000);
            }

            Console.WriteLine("Press any key to continue...");
            Console.WriteLine();

            Console.ReadKey(true);
        }
    }
}
