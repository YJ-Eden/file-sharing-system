using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MyNetworkLibrary;

namespace Server
{ 

    class MainClass
    {
        /// <summary>
        /// 数据接收缓冲区尺寸
        /// </summary>
        private const int BufferSize = 1024;     

        static void Main(string[] args)
        {
            byte[] data = new byte[BufferSize];
            string path = "/Users/eden/Desktop/server/";
            Thread listenThread;

            //使用本机8079端口作为server端套接字
            //IPAddress localAddress = AddressHelper.GetLocalhostIPv4Addresses().First();
            IPAddress localAddress = IPAddress.Parse("127.0.0.1");
            int LocalPort = 8079;
            //创建IP终结点
            IPEndPoint ipep = new IPEndPoint(localAddress, LocalPort);
            Console.WriteLine("欢迎进入服务端，请输入文件存储目录：");
            path = Console.ReadLine();
            Console.WriteLine("服务端文件存储目录已设置。\n");
            while (true)
            {
                //创建套接字，使用TCP协议
                Socket newsock = new Socket(AddressFamily.InterNetwork,
                        SocketType.Stream, ProtocolType.Tcp);
                using (newsock)
                {
                    //绑定
                    newsock.Bind(ipep);
                    //开始监听
                    newsock.Listen(1);
                    Console.WriteLine("主机 {0} 正在监听端口 {1} ，等待客户端连接……", localAddress, ipep.Port);
                    //如果有客户端连接……
                    Socket client = newsock.Accept();

                    IPEndPoint clientep = (IPEndPoint)client.RemoteEndPoint;

                    Console.WriteLine("已接收客户端连接，客户端IP地址：{0} 开放端口：{1}",
                            clientep.Address, clientep.Port);
                    string welcome = "\n欢迎使用本网络服务,请输入英文指令：\n" +
                        "upload 上传文件\n" +
                        "view 查看服务器中文件列表\n" +
                        "download 下载服务器中文件\n" +
                        "exit 退出\n";
                    data = Encoding.UTF8.GetBytes(welcome);
                    //向客户端发送欢迎语句、指令列表
                    client.Send(data, data.Length,
                             SocketFlags.None);
                    int recv = 0;
                    string StringSentByClient = "";
                    while (true)
                    {
                        //接收客户端指令并展示
                        recv = client.Receive(data);
                        StringSentByClient = Encoding.UTF8.GetString(data, 0, recv);
                        Console.WriteLine("客户端传来：{0}", StringSentByClient);
                        //客户端发来exit，关闭与当前client的连接，重新开始监听
                        if (StringSentByClient == "exit")
                        {
                            Console.WriteLine("断开客户端 {0} 连接\n", clientep.Address);
                            client.Close();
                            break;
                        }
                        //客户端发来upload指令
                        if (StringSentByClient == "upload")
                        {
                            byte[] buffer = new byte[BufferSize];
                            //接收client端发来的文件名称、长度
                            int count = client.Receive(buffer);
                            Console.WriteLine("收到" + clientep.Address + ":" + Encoding.UTF8.GetString(buffer, 0, count));
                            string[] command = Encoding.UTF8.GetString(buffer, 0, count).Split(',');
                            string fileName;
                            long length;
                            if (command[0] == "namelength")
                            {
                                fileName = command[1];
                                length = Convert.ToInt64(command[2]);
                                //向client返回OK，开始上传
                                client.Send(Encoding.UTF8.GetBytes("OK"));
                                long receive = 0L;
                                Console.WriteLine("Receiving file:" + fileName + ".Please wait...");
                                using (FileStream writer = new FileStream(Path.Combine(path,fileName), FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    int received;
                                    while(receive < length)
                                    {
                                        received = client.Receive(buffer);
                                        writer.Write(buffer, 0, received);
                                        writer.Flush();
                                        receive += (long)received;
                                    }

                                }
                                Console.WriteLine("Receive finish.");
                                
                            }

                        }
                        //client发来view指令，获取server文件夹文件列表并发送给client
                        if (StringSentByClient == "view")
                        {
                            var files = Directory.GetFiles(path, "*.txt");
                            byte[] EchoStringBytes;
                            string names = "";
                            foreach (var file in files)
                            {
                                names += (Path.GetFileName(file) + "\n");
                            }
                            names += "全部文件已列出。\n";
                            EchoStringBytes = Encoding.UTF8.GetBytes(names);
                            client.Send(EchoStringBytes);
                            Console.WriteLine("已向客户端发送文件列表。");
                        }
                        //客户端发来download+文件名字，发送server端相应文件。
                        if (StringSentByClient.Contains("download"))
                        {
                            string fn = StringSentByClient.Split('^')[1];
                            FileInfo fileinfo = new FileInfo(path+fn);
                            Console.WriteLine("传输开始...");
                            FileStream filestream = fileinfo.OpenRead();
                            byte[] file = new byte[fileinfo.Length];
                            filestream.Read(file, 0, file.Length);
                            client.Send(file, 0, file.Length, SocketFlags.None);
                            filestream.Close();
                            Console.WriteLine("传输已完成");
                        }
                        
                    }
                }
            }
        }
    }
}
