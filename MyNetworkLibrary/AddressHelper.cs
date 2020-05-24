using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MyNetworkLibrary
{
    public static class AddressHelper
    {
        /// <summary>
        /// 获取本机IPv4地址的集合
        /// </summary>
        /// <returns></returns>
        public static IPAddress[] GetLocalhostIPv4Addresses()
        {
            String LocalhostName = Dns.GetHostName();

            IPHostEntry host = Dns.GetHostEntry(LocalhostName);
            List<IPAddress> addresses = new List<IPAddress>();
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    addresses.Add(ip);
            }
            return addresses.ToArray();
        }

        /// <summary>
        /// 以交互方式生成有效的远程主机访问终结点,适用于控制台程序
        /// </summary>
        /// <returns></returns>
        public static IPEndPoint GetRemoteMachineIPEndPoint()
        {
            IPEndPoint iep = null;
            try
            {
                Console.Write("请输入远程主机的IP地址：");
                IPAddress address = IPAddress.Parse(Console.ReadLine());
                Console.Write("请输入远程主机打开的端口号：");
                int port = Convert.ToInt32(Console.ReadLine());
                if (port > 65535 || port < 1024)
                    throw new Exception("端口号应该为[1024,65535]范围内的整数");
                iep = new IPEndPoint(address, port);

            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("输入的数据有误！");
            }
            catch (FormatException)
            {
                Console.WriteLine("输入的数据有误！");
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return iep;
        }

        /// <summary>
        /// 获取本机当前可用的端口号，此方法是线程安全的
        /// </summary>
        /// <returns></returns>
        public static int GetOneAvailablePortInLocalhost()
        {
            Mutex mtx = new Mutex(false, "MyNetworkLibrary.AddressHelper.GetOneAvailablePort");
            try
            {
                mtx.WaitOne();
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                using (Socket tempSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    tempSocket.Bind(ep);
                    IPEndPoint ipep = tempSocket.LocalEndPoint as IPEndPoint;
                    return ipep.Port;
                }
            }
            finally
            {
                mtx.ReleaseMutex();
            }
        }
    }
}
