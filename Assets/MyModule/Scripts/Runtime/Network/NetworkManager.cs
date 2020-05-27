using System;
using System.Text;
using System.Collections.Generic;
using XLua;

namespace CenturyGame.Framework.Network
{
    public abstract class NetworkClient
    {
        public abstract void Connect(string host, int port);

        public abstract void Disconnect();

        public abstract bool IsConnect();

        public abstract void SendMessage(byte[] data);

        public abstract void SetMsgProcesser(IMsgProcesser processer);
    }

    public class NetworkManager : FrameworkModule
    {
        private enum EConnectType
        {
            TCP,
        }

        private EConnectType ConnectType {get; set;}

        public NetworkClient Client { get; private set; }

        private Queue<SendPackage> SendQueue = new Queue<SendPackage>(Const.Max_Msg_Capacity);
        private Queue<OptionMsg> OptionQueue = new Queue<OptionMsg>(Const.Max_OptionMsg_Capacity);
        private Queue<byte[]> RecvQueue = new Queue<byte[]>(Const.Max_Msg_Capacity);

        public HttpAgent HttpAgent { get; private set; }

        private Dictionary<string, string> HttpArgs = new Dictionary<string, string>(64);
        private Dictionary<string, string> HttpHeads = new Dictionary<string, string>(32);

        public event Action onConnectSuccess;
        public event Action onConnectFailed;
        public event Action<string> onDisconnect; 

        public IMsgProcesser MsgProcesser { get; private set; }

        public override void Init()
        {
            HttpAgent = new HttpAgent();
        }

        public override void Update(float elapseTime, float realElapseTime)
        {
            TrySend();
            ProcessOptionMsg();
            ProcessMsg();
        }

        public override void LateUpdate()
        {
            Trace.Instance.update(TraceUpdate);
        }

        public override void Shutdown()
        {
            SendQueue.Clear();
            OptionQueue.Clear();
            RecvQueue.Clear();
            if (Client != null)
                Client.Disconnect();
        }

        private StringBuilder netLogBuilder = new StringBuilder();
        private readonly string netLogPath = System.Environment.CurrentDirectory + "\\NetworkTrace.log";
        private void TraceUpdate(DateTime a_dataTime, ETracerLevel a_level, string a_context, string a_file, int a_line)
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            netLogBuilder.Length = 0;
            netLogBuilder.Append(a_dataTime.ToString("[yyyy-MM-dd HH:mm:ss]"));
            netLogBuilder.Append("[" + a_level.ToString() + "]");
            netLogBuilder.Append(a_context);
            netLogBuilder.Append("[" + a_file + "(" + a_line.ToString() + ")]");
            using (System.IO.StreamWriter sw = System.IO.File.AppendText(netLogPath))
            {
                if (a_level >= ETracerLevel.WARN)
                {
                    UnityEngine.Debug.LogWarning(a_context + "[" + a_file + "(" + a_line.ToString() + ")]");
                }
                sw.WriteLine(netLogBuilder.ToString());
            }
#endif
        }

        public void SetMsgProcesser(IMsgProcesser processer)
        {
            MsgProcesser = processer;
        }

        public void EnqueueOptionMsg(OptionMsg msg)
        {
            OptionQueue.Enqueue(msg);
        }

        private void ProcessOptionMsg()
        {
            while (true)
            {
                if (OptionQueue.Count > 0)
                {
                    var msg = OptionQueue.Dequeue();
                    switch (msg.Type)
                    {
                        case EOptionMsgType.Connect:
                            var op_connect = msg as ConnectMsg;
                            OnConnect(op_connect.Success);
                            break;
                        case EOptionMsgType.Disconnect:
                            var op_disconnect = msg as DisconnectMsg;
                            OnDisconnect(op_disconnect.Reason);
                            break;
                        default:
                            break;
                    }
                }
                else
                    break;
            }
        }

        public void StartTcpConnect(string host, int port)
        {
            ConnectType = EConnectType.TCP;
            TcpClient tcp = new TcpClient();
            tcp.PostOptionMsgEvent += EnqueueOptionMsg;
            tcp.PostMsgEvent += EnqueueRecvMsg;
            Client = tcp;
            Client.SetMsgProcesser(MsgProcesser);
            Client.Connect(host, port);
        }

        public void SendMessage(string fullName, byte[] data)
        {
            SendPackage sp = new SendPackage
            {
                name = fullName,
                data = data
            };
            SendQueue.Enqueue(sp);
        }

        public void EnqueueRecvMsg(byte[] data)
        {
            RecvQueue.Enqueue(data);
        }

        private void ProcessMsg()
        {
            while (true)
            {
                if (RecvQueue.Count > 0)
                {
                    byte[] msgData = RecvQueue.Dequeue();
                    MsgProcesser.DispatchMsg(msgData);
                }
                else
                    break;
            }
        }

        /// <summary>
        /// 发送HttpPost请求(CS层使用)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="header"></param>
        /// <param name="postData"></param>
        /// <param name="callback"></param>
        public void PostHttpRequest(string url, Dictionary<string, string> header, byte[] postData, Action<byte[]> callback)
        {
            HttpAgent.Post(url, postData, header, callback);
        }

        /// <summary>
        /// 发送HttpGet请求(CS层使用)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="header"></param>
        /// <param name="args"></param>
        /// <param name="callback"></param>
        public void GetHttpRequest(string url, Dictionary<string, string> header, Dictionary<string, string> args, Action<byte[]> callback)
        {
            HttpAgent.Get(url, args, header, callback);
        }

        /// <summary>
        /// 发送HttpPost请求(Lua层使用)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="header"></param>
        /// <param name="postData"></param>
        /// <param name="callback"></param>
        public void PostHttpRequest(string url, LuaTable header, byte[] postData, Action<byte[]> callback)
        {
            var headerKeys = header.GetKeys();
            var e = headerKeys.GetEnumerator();
            HttpHeads.Clear();
            while (e.MoveNext())
            {
                var key = e.Current.ToString();
                var value = header.Get<string>(key);
                HttpHeads.Add(key, value);
            }
            HttpAgent.Post(url, postData, HttpHeads, callback);
        }

        /// <summary>
        /// 发送HttpGet请求(Lua层使用)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="header"></param>
        /// <param name="args"></param>
        /// <param name="callback"></param>
        public void GetHttpRequest(string url, LuaTable header, LuaTable args, Action<byte[]> callback)
        {
            var headerKeys = header.GetKeys();
            var e = headerKeys.GetEnumerator();
            HttpHeads.Clear();
            while (e.MoveNext())
            {
                var key = e.Current.ToString();
                var value = header.Get<string>(key);
                HttpHeads.Add(key, value);
            }

            var argKeys = args.GetKeys();
            var e2 = argKeys.GetEnumerator();
            HttpArgs.Clear();
            while (e2.MoveNext())
            {
                var key = e2.Current.ToString();
                var value = args.Get<string>(key);
                HttpArgs.Add(key, value);
            }
            HttpAgent.Get(url, HttpArgs, HttpHeads, callback);
        }

        private void TrySend()
        {
            if (Client == null)
                return;
            if (!Client.IsConnect())
                return;
            if (SendQueue.Count == 0)
                return;
            SendPackage sp = SendQueue.Dequeue();
            Trace.Instance.debug("[SEND] {0}", sp.name);
            Client.SendMessage(sp.data);
        }

        public void Disconnect()
        {
            if (Client == null)
                return;
            if (!Client.IsConnect())
                return;
            Client.Disconnect();
            Client = null;
        }

        private void OnConnect(bool success)
        {
            if (success)
                onConnectSuccess?.Invoke();
            else
                onConnectFailed?.Invoke();
        }

        private void OnDisconnect(DisconnectReason reason)
        {
            onDisconnect?.Invoke(reason.ToString());
        }

        private void OnErrorResponse(int code, string message)
        {
            Trace.Instance.error("recv ErrorResponse. code = {0}, message = {1}", code, message);
        }

        sealed class SendPackage
        {
            public string name;
            public byte[] data;
        }
    }
}