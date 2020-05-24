using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MyNetworkLibrary;
using System.Windows.Forms;
using System.IO;

namespace client
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

            IPEndPoint ipep = null;

            //服务器地址设置为本机8079端口
            IPAddress localAddress = IPAddress.Parse("127.0.0.1");
            int LocalPort = 8079;
            //创建IP终结点
            ipep = new IPEndPoint(localAddress, LocalPort);

            ////要求用户输入有效的远程主机IP地址和端口
            //while (true)
            //{
            //    ipep = AddressHelper.GetRemoteMachineIPEndPoint();
            //    if (ipep != null)
            //        break;
            //}

            //创建套接字，使用TCP协议
            Socket server = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

            String stringData = "";
            //连接服务器
            try
            {
                server.Connect(ipep);
                Console.WriteLine("服务端已连接");
            }
            catch (SocketException e)
            {
                Console.WriteLine("无法连接远程主机 {0} ,原因：{1}，NativeErrorCode：{2},SocketErrorCode:{3}", ipep.Address, e.Message, e.NativeErrorCode, e.SocketErrorCode);
                Console.WriteLine("敲任意键退出...");
                Console.ReadKey();
                return;
            }
            //接收服务器介绍
            int recv = server.Receive(data);
            stringData = Encoding.UTF8.GetString(data, 0, recv);
            Console.WriteLine(stringData);

            String UserInput = "";
            String path = "";
            while (true)
            {
                //用户输入指令
                UserInput = Console.ReadLine();
                if (UserInput == "")
                    continue;
                byte[] SentBytes = Encoding.UTF8.GetBytes(UserInput);
                string filename = "";
                string filepath = "";
                //如果指令是download，则再输入下载文件保存地址，以download^地址的格式传输给server
                if(UserInput == "download")
                {
                    Console.WriteLine("请输入要下载的文件名称：");
                    filename = Console.ReadLine();
                    SentBytes = Encoding.UTF8.GetBytes(UserInput + "^" + filename);
                    Console.WriteLine("请输入文件保存位置：");
                    filepath = Console.ReadLine();
                }
                //将用户指令发送给server
                server.Send(SentBytes);
                //如果用户发送exit指令，断开和server的连接，结束client程序
                if (UserInput == "exit")
                {
                    server.Shutdown(SocketShutdown.Both);
                    server.Close();
                    Console.WriteLine("断开与服务端的连接。");
                    break;
                }
                //如果用户发送upload指令
                else if (UserInput == "upload")
                {
                    //输入要上传的本机文件地址
                    Console.WriteLine("请输入文件地址：");
                    path = Console.ReadLine();

                    try
                    {
                        //建立文件流
                        using (FileStream reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            //获取文件名称、长度，发送给server
                            long send = 0L, length = reader.Length;
                            string senStr = "namelength," + Path.GetFileName(path) + "," + length.ToString();
                            string fileName = Path.GetFileName(path);
                            server.Send(Encoding.UTF8.GetBytes(senStr));
                            //接收server回复
                            server.Receive(data);
                            string mes = Encoding.UTF8.GetString(data);
                            //若server回复中包含OK，开始上传
                            if (mes.Contains("OK"))
                            {
                                Console.WriteLine("Sending files:" + fileName + ".Please wait...");
                                int read, sent;
                                while ((read = reader.Read(data, 0, BufferSize)) != 0)
                                {
                                    sent = 0;
                                    while ((sent += server.Send(data, sent, read, SocketFlags.None)) < read)
                                    {
                                        send += (long)sent;
                                    }
                                }
                                Console.WriteLine("文件已上传.\n");
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                //如果用户发送view指令
                else if (UserInput == "view")
                {
                    //接收服务器发来的文件列表信息并展示
                    recv = server.Receive(data);
                    stringData = Encoding.UTF8.GetString(data, 0, recv);
                    Console.WriteLine(stringData);
                }
                //如果用户发送download命令，下载文件
                else if (UserInput.Contains("download"))
                {
                    Console.WriteLine("下载开始...");
                    FileStream newfile = new FileStream(filepath + filename, FileMode.Create, FileAccess.Write);
                    byte[] filerec = new byte[5000];
                    int revcount = server.Receive(filerec, 0, 5000, SocketFlags.None);
                    byte[] filereal = new byte[revcount];
                    for (int i = 0; i < revcount; i++) filereal[i] = filerec[i];
                    newfile.Write(filereal, 0, filereal.Length);
                    filereal = null;
                    newfile.Close();
                    Console.WriteLine("下载已完成。\n");
                }
                else
                {
                    Console.WriteLine("无法识别的指令，请重新输入。\n");
                }
            }
            Console.WriteLine("敲任意键退出……");
            Console.ReadKey();
        }
    }
}
