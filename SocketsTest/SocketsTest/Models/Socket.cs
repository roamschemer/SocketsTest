using System;
using System.Text;
using Prism.Mvvm;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace SocketsTest.Models
{
    /// <summary>
    /// Soket通信用
    /// </summary>
    class Socket : BindableBase
    {
        private string ipAddress = "192.168.1.1";
        public string IpAddress
        {
            get => this.ipAddress;
            set => this.SetProperty(ref this.ipAddress, value);
        }
        private int port = 9999;
        public int Port
        {
            get => this.port;
            set => this.SetProperty(ref this.port, value);
        }
        private bool isServer;
        public bool IsServer
        {
            get => this.isServer;
            set => this.SetProperty(ref this.isServer, value);
        }
        private bool isOpen;
        public bool IsOpen
        {
            get => this.isOpen;
            set => this.SetProperty(ref this.isOpen, value);
        }
        private string sendData;
        public string SendData
        {
            get => this.sendData;
            set => this.SetProperty(ref this.sendData, value);
        }

        public ObservableCollection<string> ActionInfo { get; } = new ObservableCollection<string>();

        //ユーザ定義メンバ変数
        private TcpListener tcplistener = null;
        private TcpClient tcpClient = null;

        //ストリーム
        private NetworkStream networkStream = null;
        private StreamReader streamReader = null;
        private StreamWriter streamWriter = null;

#pragma warning disable 4014 //Task.Runの同期と非同期が混同してるが別に間違いじゃないので警告回避。

        /// <summary>
        /// 通信開始
        /// </summary>
        public async void Open()
        {
            string methodName = (IsServer ? "Server" : "Client") + "Open";
            try
            {
                IsOpen = true;
                //サーバー側の挙動
                if (IsServer)
                {
                    ActionInfo.Insert(0, DateTime.Now.ToString("hh:mm:ss") + "," + methodName + "," + "接続待機");
                    tcplistener = new TcpListener(IPAddress.Any, Port);
                    tcplistener.Start();
                    Task.Run(async () =>
                    {
                        //クライアントの要求があったら、接続を確立する(接続があるかtcplistener.Stop()が実行されるまで待機する)
                        await Task.Run(() => tcpClient = tcplistener.AcceptTcpClient());
                        ActionInfo.Insert(0, DateTime.Now.ToString("hh:mm:ss") + "," + methodName + "," + "接続完了");
                        networkStream = tcpClient.GetStream();
                        streamReader = new StreamReader(networkStream, Encoding.UTF8);
                        streamWriter = new StreamWriter(networkStream, Encoding.UTF8);
                        Task.Run(() => Receive());
                    });
                }
                //クライアント側の挙動
                if (!IsServer)
                {
                    ActionInfo.Insert(0, DateTime.Now.ToString("hh:mm:ss") + "," + methodName + "," + "接続待機");
                    //サーバーが見つかったら、接続を確立する(接続があるかタイムアウトまで待機する)
                    await Task.Run(() => tcpClient = new TcpClient(IpAddress, Port));
                    ActionInfo.Insert(0, DateTime.Now.ToString("hh:mm:ss") + "," + methodName + "," + "接続完了");
                    networkStream = tcpClient.GetStream();
                    streamReader = new StreamReader(networkStream, Encoding.UTF8);
                    streamWriter = new StreamWriter(networkStream, Encoding.UTF8);
                    Task.Run(() => Receive());
                }
            }
            catch (Exception ex)
            {
                ActionInfo.Insert(0, DateTime.Now.ToString("hh:mm:ss") + "," + methodName + "," + ex.Message);
                Close();
                IsOpen = false;
            }
        }

        /// <summary>
        /// 接続終了
        /// </summary>
        public void Close()
        {
            string methodName = (IsServer ? "Server" : "Client") + "Close";
            try
            {
                if (IsServer) tcplistener.Stop();
                if (!IsServer)
                {
                    if ((tcpClient != null) && tcpClient.Connected)
                    {
                        tcpClient.Close();
                        tcpClient.Dispose();
                    }
                }
                if (networkStream != null)
                {
                    networkStream.Close();
                    networkStream.Dispose();
                }
                tcplistener = null;
                tcpClient = null;
                networkStream = null;
                ActionInfo.Insert(0, DateTime.Now.ToString("hh:mm:ss") + "," + methodName + "," + "接続終了");
            }
            catch (Exception ex)
            {
                ActionInfo.Insert(0, DateTime.Now.ToString("hh:mm:ss") + "," + methodName + "," + ex.Message);
            }
            IsOpen = false;
        }

        /// <summary>
        /// 信号送信
        /// </summary>
        public void Send()
        {
            string methodName = (IsServer ? "Server" : "Client") + "Send";
            try
            {
                byte[] tmp = Encoding.UTF8.GetBytes(SendData);
                networkStream.Write(tmp, 0, tmp.Length);//送信 引数は（データ , データ書き込み開始位置 , 書き込むバイト数）
                ActionInfo.Insert(0, DateTime.Now.ToString("hh:mm:ss") + "," + methodName + "," + SendData);
            }
            catch (Exception ex)
            {
                ActionInfo.Insert(0, DateTime.Now.ToString("hh:mm:ss") + "," + methodName + "," + ex.Message);
            }
        }

        /// <summary>
        /// 信号受信(受信があるまで待機する)
        /// </summary>
        private async void Receive()
        {
            string methodName = (IsServer ? "Server" : "Client") + "Receive";
            try
            {
                await Task.Run(() =>
                {
                    while (true)
                    {
                        string res = null;
                        byte[] data = new byte[256];
                        string receiveData = string.Empty;
                        int bytes = networkStream.Read(data, 0, data.Length);
                        if (bytes == 0) break;
                        res = Encoding.UTF8.GetString(data, 0, bytes);
                        ActionInfo.Insert(0, DateTime.Now.ToString("hh:mm:ss") + "," + methodName + "," + res);
                    }
                    ActionInfo.Insert(0, DateTime.Now.ToString("hh:mm:ss") + "," + methodName + "," + "接続が遮断されました。");
                    Close();
                });
            }
            catch (Exception ex)
            {
                //遮断エラー(IOException(SocketExceptionじゃないのは何故…？))の場合はメッセージ不要
                if (!(ex is IOException)) ActionInfo.Insert(0, DateTime.Now.ToString("hh:mm:ss") + "," + methodName + "," + ex.Message);
            }
        }

#pragma warning restore 4014 //Task.Runの同期と非同期が混同してるが別に間違いじゃないので警告回避。

    }
}