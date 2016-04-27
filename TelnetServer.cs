using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Telnet
{
    public class Server
    {
        public bool bThreadStarted = false;
        public TcpListener oTcpListener;
        public Thread tListener;
        private readonly Func<Client, Client> fCallback;
        public string sDelimiter = "";

        public Server(IPAddress address, int port, string sDelimiter, Func<Client, Client> callbackFunction)
        {
            this.fCallback = callbackFunction;
            this.sDelimiter = sDelimiter;

            this.oTcpListener = new TcpListener(address, port);
            this.oTcpListener.Start();

            this.bThreadStarted = true;
            this.tListener = new Thread(() => waitForClient());
            this.tListener.Start();
        }

        public void stop()
        {
            this.bThreadStarted = false;
            this.tListener.Join();
        }

        public void waitForClient()
        {
            while (this.bThreadStarted)
            {
                if (!this.oTcpListener.Pending())
                {
                    Thread.Sleep(10);
                    continue; // skip to next iteration of loop
                }

                TcpClient c = this.oTcpListener.AcceptTcpClient();

                ThreadParameter o = new ThreadParameter(c, this.sDelimiter, this.fCallback);

                ThreadPool.QueueUserWorkItem(this.addClientToThreadPool, o);
            }

        }

        void addClientToThreadPool(object oParameter)
        {
            // Console.WriteLine("new client");
            new Client((ThreadParameter)oParameter);
        }
    }

    public class ThreadParameter
    {
        public TcpClient Client;
        public string sDelimiter = "";
        public readonly Func<Client, Client> callback;

        public ThreadParameter(TcpClient c, string s, Func<Client, Client> f)
        {
            this.Client     = c;
            this.sDelimiter = s;
            this.callback   = f;
        }
    }

    public class Client
    {
        private TcpClient oClient;
        private NetworkStream oStream;
        public string data = "";

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public Client(ThreadParameter oParamter)
        {
            TcpClient c = oParamter.Client;
            string sDelimiter = oParamter.sDelimiter;
            string sStr = ""; // recived data as string
            byte[] data = new byte[1024]; // recevice buffer
            MemoryStream ms = new MemoryStream(); // recevice buffer as stream
            try
            {
                oClient = c;
                oStream = c.GetStream();

                Console.WriteLine("Started new thread for: " + ((IPEndPoint)oClient.Client.RemoteEndPoint).Address.ToString());
                
                int numBytesRead;
                while (oClient.Connected && (numBytesRead = oStream.Read(data, 0, data.Length)) > 0)
                {
                    ms.Write(data, 0, numBytesRead);
                    sStr = Encoding.ASCII.GetString(ms.ToArray(), 0, (int)ms.Length);

                    if (sStr.EndsWith(sDelimiter))
                    {
                        this.data = sStr;
                        oParamter.callback(this);
                        // clear memoryStream
                        ms = new MemoryStream();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public bool Write(string sData)
        {
            byte[] senddata = GetBytes(sData);
            oStream.Write(senddata, 0, senddata.Length);
            return true;
        }
        public bool Close()
        {
            this.oClient.Close();

            return true;
        }
    }
}
