using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Text;
using System.Net;
using Telnet;

namespace exampleServer
{
    class Program
    {

        static void Main(string[] args)
        {

            Telnet.Server TelnetServer = new Telnet.Server(IPAddress.Parse("127.0.0.1"), 9999, "<ack>",TelnetResponse);
            
            Console.WriteLine("Started telnetserver");
            Console.ReadKey();

            TelnetServer.stop();
        }

        public static Telnet.Client TelnetResponse(Telnet.Client oClient)
        {
            Console.WriteLine("Got:" + oClient.data);

            oClient.Write("Hello there!\r\n");

            if (oClient.data.Contains("<quit>"))
                oClient.Close();

            return oClient;
        }
    }
}
