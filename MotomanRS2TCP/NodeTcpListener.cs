using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MotomanRS2TCP
{
    public class NodeTcpListener
    {
        private Thread mainTread;
        private TcpListener server = null;
        private readonly List<TcpClient> clients = new List<TcpClient>();
        private Form1 form;
        private CYasnac xrc;

        //public NodeTcpListener(string ip, int port)
        public NodeTcpListener(Form1 _form, CYasnac _xrc, int port)
        {
            form = _form;
            xrc = _xrc;

            mainTread = new Thread(delegate ()
            {
                //IPAddress localAddr = IPAddress.Parse(ip);
                server = new TcpListener(IPAddress.Any, port);
                server.Start();
                StartListener();
            });
            mainTread.Start();
            form.WriteLine("    TCP is listening");
        }

        public void StopServer()
        {
            try
            {
                foreach (TcpClient client in clients) client.Close();
                server.Stop();
                mainTread.Join();
            }
            catch (Exception e)
            {
                Console.WriteLine("StopServerException: {0}", e);
            }
        }

        public void StartListener()
        {
            try
            {
                while (true)
                {
                    //Console.WriteLine("Waiting for a connection...");
                    TcpClient client = server.AcceptTcpClient();
                    clients.Add(client);
                    form.WriteLine("    TCP client " + ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());
                    Thread t = new Thread(new ParameterizedThreadStart(HandleDeivce));
                    t.Start(client);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                server.Stop();
            }
        }
        public void HandleDeivce(Object obj)
        {
            TcpClient client = (TcpClient)obj;
            string ipAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            var stream = client.GetStream();
            string imei = String.Empty;
            string data = null;
            Byte[] bytes = new Byte[256];
            int i;
            try
            {
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    string hex = BitConverter.ToString(bytes);
                    data = Encoding.UTF8.GetString(bytes, 0, i);
                    form.WriteLine("    TCP from " + ipAddress + " >> " + data);
                    //Console.WriteLine("{1}: Received: {0}", data, Thread.CurrentThread.ManagedThreadId);
                    string str = "Hey Device!";
                    Byte[] reply = Encoding.UTF8.GetBytes(str);
                    stream.Write(reply, 0, reply.Length);
                    //Console.WriteLine("{1}: Sent: {0}", str, Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
                client.Close();
            }
        }

    }
}
