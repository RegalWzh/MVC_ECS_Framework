//=====================================================
// - FileName:      NetMgr.cs
// - Created:       mahuibao
// - UserName:      2019-12-01
// - Email:         1023276156@qq.com
// - Description:   网络管理基础层
// -  (C) Copyright 2018 - 2019
// -  All Rights Reserved.
//======================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zero.ZeroEngine.Core;
using System;
using Zero.ZeroEngine.Util;
using UnityEngine.Networking;
using XLua;
using System.Net.Sockets;
using Zero.ZeroEngine.Common;
using System.Text;
using System.Threading;

//=====================================================
// - c# and lua
// - fix 检查过命名规则，是否使用对象池、类对象池优化过。
//=====================================================
// - 1.
// - 2.
// - 3.
// - 4.
// - 5.
// - 6.
//======================================================

namespace Zero.ZeroEngine.Net
{
    /// <summary>
    /// 网络连接结果枚举
    /// </summary>
    public enum NetResultType
    {
        Exception = 0,//报错
        Disconnect = 1,//断开
        ConnectTimeOut = 2,//连接超时
    }

    /// <summary>
    /// 网络连接状态
    /// </summary>
    public enum ConnectStateType
    {
        None = 0,//无
        Connecting = 1,//连接中
        Connected = 2,//已连接
        ConnectFail = 3,//连接失败
        OffLine = 4,//已下线
        Disconnect = 5,//断开
    }

    public class NetConst
    {
        public const int TEMP_MSG_SIZE = 4096;//临时存储包的长度
        public const int MSG_DATA_LEN_LEN = 4;//包头中标识包长的字节数
        public const int MSG_ID_LEN = 4;//包头中标识ID的字节数
        public const int MSG_HEAD_LEN = MSG_DATA_LEN_LEN + MSG_ID_LEN;//整个包头总长度
    }

    /// <summary>
    /// 已经序列化的网络数据包类
    /// </summary>
    public struct NetMsgPacket
    {
        //协议ID
        public int msgId;
        //数据
        public byte[] data;
    }

    /// <summary>
    /// 网络管理基础层
    /// </summary>
    public class NetMgr : SingletonMono<NetMgr>
    {
        //Socket是否连接
        public bool isConnected
        {
            get
            {
                return m_ClientSocket != null && m_ClientSocket.Connected && m_ConnectState == ConnectStateType.Connected;
            }
        }
        //连接状态
        private ConnectStateType m_ConnectState = ConnectStateType.None;
        //客户端Socket
        private Socket m_ClientSocket = null;
        //ip地址
        private string m_IP = "127.0.0.1";
        //端口
        private int m_Port = 0;
        //连续失败重连连接次数
        public int connectTimes = 0;
        //开启广播连接成功
        private bool m_DispatchConnectBoo = false;
        //丢失心跳数量
        private int m_lostHeartTime = 0;

        //协议加密----- no edit

        //临时缓存数据包
        private byte[] m_TempReceiveData = new byte[NetConst.TEMP_MSG_SIZE];
        //接收数据线程
        private Thread m_ReceiveThread = null;
        //存储已经解析出来长度跟数据的数据包队列
        private Queue<NetMsgPacket> m_MsgQueue = new Queue<NetMsgPacket>();

        private const string NET_SEND_COR_PRE = "NetSendCor_";
        private int net_Send_Cor_Post = 1;
        private const string NET_DEAL_MSG_COR = "NetDealMsgCor";


        public void Init()
        {
            ZLogger.Info("网络管理基础层初始化");
            StarDealMsgCor();
            StarReceiveThread();
        }

        public void Clear()
        {

        }

        public void AfterInit()
        {

        }

        #region 发起连接
        //发送连接请求
        public void SendConnect(string canHost,int canPort)
        {
            if (UtilTool.NetAvailable)
            {
                SetIpAndPort(canHost, canPort);
                SocketConnect();
            }
            else
            {
                m_ConnectState = ConnectStateType.ConnectFail;
                OnDisconnect(NetResultType.ConnectTimeOut, "连接游戏服务器失败!(404)");
            }
        }

        //设置ip地址跟端口port
        public void SetIpAndPort(string canHost,int canPort)
        {
            m_IP = canHost;
            m_Port = canPort;
        }

        //连接
        public void SocketConnect()
        {
            if (isConnected || m_ConnectState == ConnectStateType.Connecting)
            {
                ZLogger.Info("Socket已经连接上了，别再重复进行连接");
                return;
            }
            string tempStrNewIp = "";
            AddressFamily tempAddressFamily = AddressFamily.InterNetwork;
            //这里准备以后引入一个解析IP地址的库，从而可以ipv4，还是ipv6 no edit
            ZLogger.Info("Socket AddressFaminly:{0}  原始IP:{1},  结果IP:{2},  端口:{3}", tempAddressFamily, m_IP, tempStrNewIp, m_Port);

            m_ClientSocket = new Socket(tempAddressFamily, SocketType.Stream, ProtocolType.Tcp);
            m_ClientSocket.SendTimeout = 1000;//指定同步 Send 调用将超时的时间长度
            m_ClientSocket.ReceiveTimeout = 5000;//指定同步 Receive 调用将超时的时间长度
            m_ClientSocket.NoDelay = true;//指定流 Socket 是否正在使用 Nagle 算法
            m_ClientSocket.SendBufferSize = 1024 * 8;//指定 Socket 发送缓冲区的大小
            m_ClientSocket.ReceiveBufferSize = NetConst.TEMP_MSG_SIZE;//获取或设置此 ServicePoint 使用的套接字的接收缓冲区的大小
            m_ClientSocket.Blocking = true;//指示 Socket 是否处于阻止模式

            m_ConnectState = ConnectStateType.Connecting;

            try
            {
                m_ClientSocket.BeginConnect(tempStrNewIp, m_Port, new AsyncCallback(OnConnectCB), m_ClientSocket);
            }
            catch(Exception e)
            {
                //设置失败次数++
                ZLogger.Info("连接不通:{0}  ==>  {1}", e.Message, e.ToString());
                m_ConnectState = ConnectStateType.ConnectFail;
                OnDisconnect(NetResultType.ConnectTimeOut, "连接游戏服务器失败!(-1)");
            }
        }

        //连接上服务器回调
        private void OnConnectCB(IAsyncResult asr)
        {
            try
            {
                Socket tempClientSocket = (Socket)asr.AsyncState;
                if (tempClientSocket.Connected)
                {
                    tempClientSocket.EndConnect(asr);
                    //重置失败次数=0
                    tempClientSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                    m_ConnectState = ConnectStateType.Connected;
                    m_DispatchConnectBoo = true;
                    //进行广播
                    ZLogger.Info("socket 连接成功 net Connected ip={0},prot={1}", m_IP, m_Port);
                }
                else
                {
                    //设置连接失败次数++
                    m_ConnectState = ConnectStateType.ConnectFail;
                    OnDisconnect(NetResultType.ConnectTimeOut, "连接超时!(404)");
                }
            }
            catch(Exception e)
            {
                //设置连接失败次数++
                m_ConnectState = ConnectStateType.ConnectFail;
                OnDisconnect(NetResultType.Exception, "连接超时:" + e.Message);
            }
        }

        #endregion


        #region 线程处理接收到的数据相关

        //开启一个接收数据的线程
        private void StarReceiveThread()
        {
            m_ReceiveThread = new Thread(new ThreadStart(ReceiveHandle));
            m_ReceiveThread.IsBackground = true;//指示某个线程是否为后台线程
            m_ReceiveThread.Start();
        }

        //接收到数据回调
        private void ReceiveHandle()
        {
            while (true)
            {
                ReceiveExec();
                Thread.Sleep(5);
            }
        }

        //接收到数据回调执行
        private void ReceiveExec()
        {
            if (m_ClientSocket == null) return;

            if (m_ConnectState != ConnectStateType.Connected) return;

            int tempLen = m_ClientSocket.Receive(m_TempReceiveData);

            if (tempLen > 0)
            {
                NetMsgHelper.Instance.AddData(m_TempReceiveData, tempLen);//将刚接收到的数据，未处理过的放入网络辅助层

                NetMsgPacket tempMsgPacket = new NetMsgPacket();
                //一次取一条，直到把辅助层中已经处理过的数据包取完为止
                lock (m_MsgQueue)
                {
                    while(NetMsgHelper.Instance.GetOneMsg(ref tempMsgPacket))//一次取一条，直到把辅助层中已经处理过的数据包取完为止
                    {
                        if(tempMsgPacket.msgId > 0)
                        {
                            m_MsgQueue.Enqueue(tempMsgPacket);
                        }
                    }
                }
            }
        }

        //销毁线程
        private void DestroyReceiveThread()
        {
            if(m_ReceiveThread != null)
            {
                m_ReceiveThread.Abort();
                m_ReceiveThread = null;
            }
        }
        #endregion


        #region 发送消息相关

        /// <summary>
        /// 发送消息 c# and lua 共用
        /// 先转成protobuf再传入这里
        /// </summary>
        public void SendMsg(byte[] canMsg, int canMsgId)
        {
            CoroutineMgr.Instance.StartCoroutine(NET_SEND_COR_PRE + net_Send_Cor_Post, SendMessageCor(canMsg, canMsgId));
            net_Send_Cor_Post++;
        }

        // 发送消息协程
        // 这里主要处理加密，以及发送数据。至于序列化在运作层处理，因为为了xlua与c#共用脚本，所以做两层架构
        IEnumerator SendMessageCor(byte[] canMsg, int canMsgId)
        {
            if (m_ClientSocket != null)
            {
                if (m_ClientSocket.Connected)
                {
                    byte[] tempSendData = canMsg;// no edit 此处要编写加密
                    try
                    {
                        if (m_lostHeartTime > 3)
                        {
                            DoDisconnect();
                            yield break;
                        }
                        else if (canMsgId == 1200)
                        {
                            m_lostHeartTime++;
                        }
                        m_ClientSocket.Send(tempSendData, SocketFlags.None);
                    }
                    catch(Exception e)
                    {
                        if (m_ConnectState == ConnectStateType.Connected)
                            DoDisconnect();
                        else
                        {
                            OnDisconnect(NetResultType.Exception, "发送协议失败，协议ID:" + canMsgId + "\n原因" + e.ToString());
                        }
                    }
                    tempSendData = null;
                }
                else
                {
                    if (m_ConnectState == ConnectStateType.Connected)
                        DoDisconnect();
                }
            }
            yield break;
        }
        #endregion

        public void Update()
        {
            if(!UtilTool.NetAvailable && m_ClientSocket != null)
            {
                CloseSocket();
                m_ConnectState = ConnectStateType.OffLine;
            }
            if (AppConst.USE_LUA_BOO) UpdateForLua();
            else UpdateForC();
        }

        //更新函数 c#
        private void UpdateForC()
        {
            if (m_ConnectState == ConnectStateType.Connected)
            {
                if (m_DispatchConnectBoo)
                {
                    m_DispatchConnectBoo = false;
                    ++connectTimes;
                    if (connectTimes > 1)
                    {
                        NetWorkMgr.Instance.OnReConnect();
                        ZLogger.Info("重新连接到 网关服务器 ---> 重新请求");
                    }
                    else
                    {
                        NetWorkMgr.Instance.OnConnect();
                        ZLogger.Info("成功连接到 网关服务器 ---> 开始游戏");
                    }
                }
            }
            else if (m_ConnectState != ConnectStateType.None)
            {
                if (m_ConnectState == ConnectStateType.ConnectFail)
                {
                    NetWorkMgr.Instance.ShowTimeOut("连接游戏服务器失败, 请稍候再尝试连接！");
                }
                else if (m_ConnectState == ConnectStateType.Disconnect)
                {
                    NetWorkMgr.Instance.OnDisconnect();
                }
                //多次心跳接收不到掉线
                else if (m_ConnectState == ConnectStateType.OffLine)
                {
                    NetWorkMgr.Instance.ShowTimeOut("您的网络已断开, 请稍候再尝试连接！");
                }
            }
        }

        //更新函数 lua
        private void UpdateForLua()
        {
            if (m_ConnectState == ConnectStateType.Connected)
            {
                if (m_DispatchConnectBoo)
                {
                    m_DispatchConnectBoo = false;
                    ++connectTimes;
                    if (connectTimes > 1)
                    {
                        NetCallMethod("OnReConnect");
                        ZLogger.Info("重新连接到 网关服务器 ---> 重新请求");
                    }
                    else
                    {
                        NetCallMethod("OnConnect");
                        ZLogger.Info("成功连接到 网关服务器 ---> 开始游戏");
                    }
                }
            }
            else if(m_ConnectState != ConnectStateType.None)
            {
                if (m_ConnectState == ConnectStateType.ConnectFail)
                {
                    NetCallMethod("ShowTimeOut", "连接游戏服务器失败, 请稍候再尝试连接！");
                }
                else if(m_ConnectState == ConnectStateType.Disconnect)
                {
                    NetCallMethod("OnDisconnect");
                }
                //多次心跳接收不到掉线
                else if(m_ConnectState == ConnectStateType.OffLine)
                {
                    NetCallMethod("ShowTimeOut","您的网络已断开, 请稍候再尝试连接！");
                }
            }
        }

        //根据不同语言，开启处理接收到数据的协程
        private void StarDealMsgCor()
        {
            if (AppConst.USE_LUA_BOO)
            {
                CoroutineMgr.Instance.StartCoroutine(NET_DEAL_MSG_COR, DealMsgCor());
            }
            else
            {
                CoroutineMgr.Instance.StartCoroutine(NET_DEAL_MSG_COR, DealMsgCor());
            }
        }

        private void StopDealMsgCor()
        {
            if (AppConst.USE_LUA_BOO)
            {
                CoroutineMgr.Instance.StopCoroutine(NET_DEAL_MSG_COR, DealMsgCor());
            }
            else
            {
                CoroutineMgr.Instance.StopCoroutine(NET_DEAL_MSG_COR, DealMsgCor());
            }
        }

        //处理协议数据协程 c#
        //这里仅仅是提取了数据到运作层。至于反序列化，以及收到消息的回调，都在在运作层处理。
        IEnumerator DealMsgCor()
        {
            while (true)
            {
                lock (m_MsgQueue)
                {
                    while (m_MsgQueue.Count > 0)
                    {
                        NetMsgPacket tempMsgPacket = m_MsgQueue.Dequeue();
                        int tempMsgId = tempMsgPacket.msgId;
                        if (tempMsgId <= 0)
                        {
                            ZLogger.Warning("网络数据不正常，不处理此协议，协议ID：{0}", tempMsgId);
                        }
                        else
                        {
                            try
                            {
                                NetWorkMgr.Instance.OnMsg(tempMsgPacket);
                                if (tempMsgId == 1201) m_lostHeartTime = 0;//暂定心跳协议ID为1201，重置心跳网络检测
                            }
                            catch(Exception e)
                            {
                                OnDisconnect(NetResultType.Exception, "消息报错: 协议Id:" + tempMsgId + " >> " + e.Message + "|" + e.StackTrace);
                            }
                        }

                    }
                }
                yield return null;
            }
        }

        //处理协议数据协程 xlua
        //这里仅仅是提取了数据到运作层。至于反序列化，以及收到消息的回调，都在在运作层处理。
        IEnumerator DealMsgCorLua()
        {
            while (true)
            {
                lock (m_MsgQueue)
                {
                    while (m_MsgQueue.Count > 0)
                    {
                        NetMsgPacket tempMsgPacket = m_MsgQueue.Dequeue();
                        int tempMsgId = tempMsgPacket.msgId;
                        if (tempMsgId <= 0)
                        {
                            ZLogger.Warning("网络数据不正常，不处理此协议，协议ID：{0}", tempMsgId);
                        }
                        else
                        {
                            try
                            {
                                byte[] tempData = tempMsgPacket.data;
                                NetCallMethod("OnMessage", tempMsgId, tempData);
                                if (tempMsgId == 1201) m_lostHeartTime = 0;//暂定心跳协议ID为1201，重置心跳网络检测
                            }
                            catch (Exception e)
                            {
                                OnDisconnect(NetResultType.Exception, "消息报错: 协议Id:" + tempMsgId + " >> " + e.Message + "|" + e.StackTrace);
                            }
                        }

                    }
                }
                yield return null;
            }
        }

        //失去连接或连接失败
        private void OnDisconnect(NetResultType canType,string canStr = "")
        {
            CloseSocket();//关闭Socket
            //以下是根据不同情况输出
            if (canType == NetResultType.Disconnect)
            {
                ZLogger.Info("net 正常主动断线");
            }
            else if(canType == NetResultType.ConnectTimeOut)
            {
                ZLogger.Info("net 连接服务器超时");
            }
            else if(canType == NetResultType.Exception)
            {
                ZLogger.Error("net 异常断开服务器:{0}, type:{1}", canStr, canType.ToString());
            }
        }

        /// <summary>
        /// 主动断开Socket
        /// </summary>
        public void DoDisconnect()
        {
            m_ConnectState = ConnectStateType.Disconnect;
            OnDisconnect(NetResultType.Disconnect);
        }

        /// <summary>
        /// 关闭Socket
        /// </summary>
        public void CloseSocket()
        {
            if (m_ClientSocket != null)
            {
                m_ClientSocket.Close();
                m_ClientSocket = null;
            }
            m_lostHeartTime = 0;
            CloseReceiveThreadAndDealMsgCor();
        }

        //停止接收数据的线程和处理数据的协程
        private void CloseReceiveThreadAndDealMsgCor()
        {
            DestroyReceiveThread();
            StopDealMsgCor();
            m_MsgQueue.Clear();
        }

        public void OnDestroy()
        {
            if (m_ClientSocket != null)
            {
                m_ClientSocket.Close();
                m_ClientSocket = null;
            }
            CloseReceiveThreadAndDealMsgCor();
            ZLogger.Info("NetworkManager was destroy");
        }

        /// <summary>
        /// 网络基础层，执行Lua方法
        /// </summary>
        public void NetCallMethod(string tempFunc,params object[] tempArgs)
        {
            //no edit
        }


        #region Web请求共用
        //协程使用名字前缀跟后缀
        private const string WEB_GET_COR_PRE = "WebGetCor_";
        private int web_Get_Cor_Post = 1;
        private const string WEB_POST_COR_PRE = "WebPostCor_";
        private int web_Post_Cor_Post = 1;
        //进度
        public float webProgress = 0;
        #endregion Web请求共用


        #region Web请求 c#
        //Web请求 c# 回调
        private Action<WebCallBackArgs> m_WebCallBack;
        //Web请求回调数据类
        private WebCallBackArgs m_WebCallBackArgs = new WebCallBackArgs();

        // msgId 0 表示使用Get, 1 表示使用Post
        // param 必需规则为:["key", "value", "key", "value", ...]
        public void HttpRequest(int canType,string canUrl,object[] param,Action<WebCallBackArgs> canCallBack)
        {
            m_WebCallBack = canCallBack;
            Dictionary<string, string> tempDic = new Dictionary<string, string>();
            if (param != null)
            {
                if (param.Length % 2 != 0)
                {
                    ZLogger.Error("[NetMgr]参数规则有误！");
                    return;
                }

                for (int i = 0; i < param.Length; i = i + 2)
                {
                    tempDic.Add(param[i].ToString(), param[i + 1].ToString());
                }
            }
            if (canType == 0)
            {
                string tempStr = string.Concat(WEB_GET_COR_PRE, web_Get_Cor_Post);
                CoroutineMgr.Instance.StartCoroutine(tempStr, WebGetCor(canUrl, tempDic));
                web_Get_Cor_Post++;
            }
            else
            {
                string tempStr = string.Concat(WEB_POST_COR_PRE, web_Post_Cor_Post);
                CoroutineMgr.Instance.StartCoroutine(tempStr, WebPostCor(canUrl, tempDic));
                web_Post_Cor_Post++;
            }
                
        }

        //GET请求（url?传值、效率高、不安全 ）  
        IEnumerator WebGetCor(string url, Dictionary<string, string> get)
        {
            string tempParameters;
            bool tempFirstBoo;
            if (get.Count > 0)
            {
                tempFirstBoo = true;
                tempParameters = "?";
                //从集合中取出所有参数，设置表单参数（AddField()).  
                foreach (KeyValuePair<string, string> post_arg in get)
                {
                    if (tempFirstBoo)
                        tempFirstBoo = false;
                    else
                        tempParameters += "&";
                    tempParameters += post_arg.Key + "=" + post_arg.Value;
                }
            }
            else
            {
                tempParameters = "";
            }

            //直接URL传值就是get  
            UnityWebRequest tempWebRequest = UnityWebRequest.Get(url + tempParameters);
            yield return tempWebRequest;
            webProgress = tempWebRequest.downloadProgress;

            if (tempWebRequest.error != null)
            {
                //GET请求失败  
                m_WebCallBackArgs.isError = true;
                m_WebCallBackArgs.error = "error :" + tempWebRequest.error;
                m_WebCallBackArgs.content = "error :" + tempWebRequest.error;
                if (m_WebCallBack != null)
                    m_WebCallBack(m_WebCallBackArgs);
            }
            else
            {
                //GET请求成功
                m_WebCallBackArgs.isError = false;
                m_WebCallBackArgs.content = tempWebRequest.downloadHandler.text;
                if (m_WebCallBack != null)
                    m_WebCallBack(m_WebCallBackArgs);
            }
        }

        //POST请求(Form表单传值、效率低、安全 ，)  
        IEnumerator WebPostCor(string canUrl, Dictionary<string, string> canPost)
        {
            WWWForm tempForm = new WWWForm();//表单   
            //从集合中取出所有参数，设置表单参数（AddField()).  
            foreach (KeyValuePair<string, string> post_arg in canPost)
            {
                tempForm.AddField(post_arg.Key, post_arg.Value);
            }
            //表单传值，就是post   
            UnityWebRequest tempWebRequest = UnityWebRequest.Post(canUrl, tempForm);
            yield return tempWebRequest;
            webProgress = tempWebRequest.downloadProgress;

            if (tempWebRequest.error != null)
            {
                //POST请求失败
                m_WebCallBackArgs.isError = true;
                m_WebCallBackArgs.error = "error :" + tempWebRequest.error;
                m_WebCallBackArgs.content = "error :" + tempWebRequest.error;
                if (m_WebCallBack != null)
                    m_WebCallBack(m_WebCallBackArgs);
            }
            else
            { //POST请求成功
                m_WebCallBackArgs.isError = false;
                m_WebCallBackArgs.content = tempWebRequest.downloadHandler.text;
                if (m_WebCallBack != null)
                    m_WebCallBack(m_WebCallBackArgs);
            }
        }
        #endregion Web请求 c#


        #region Web请求 xLua
        //Web请求 lua 回调
        private LuaFunction m_WebCallBackLua;
        private string m_ContentLua;

        // msgId 0 表示使用Get, 1 表示使用Post
        // param 必需规则为:["key", "value", "key", "value", ...]
        public void HttpRequestLua(int canType, string canUrl, object[] param, LuaFunction canCallBack)
        {
            m_WebCallBackLua = canCallBack;
            Dictionary<string, string> tempDic = new Dictionary<string, string>();
            if (param != null)
            {
                if (param.Length % 2 != 0)
                {
                    ZLogger.Error("[NetMgr]参数规则有误！");
                    return;
                }

                for (int i = 0; i < param.Length; i = i + 2)
                {
                    tempDic.Add(param[i].ToString(), param[i + 1].ToString());
                }
            }
            if (canType == 0)
            {
                string tempStr = string.Concat(WEB_GET_COR_PRE, web_Get_Cor_Post);
                CoroutineMgr.Instance.StartCoroutine(tempStr, WebGetCor(canUrl, tempDic));
                web_Get_Cor_Post++;
            }
            else
            {
                string tempStr = string.Concat(WEB_POST_COR_PRE, web_Post_Cor_Post);
                CoroutineMgr.Instance.StartCoroutine(tempStr, WebPostCor(canUrl, tempDic));
                web_Post_Cor_Post++;
            }

        }

        //GET请求（url?传值、效率高、不安全 ）  
        IEnumerator WebGetCorLua(string url, Dictionary<string, string> get)
        {
            string tempParameters;
            bool tempFirstBoo;
            if (get.Count > 0)
            {
                tempFirstBoo = true;
                tempParameters = "?";
                //从集合中取出所有参数，设置表单参数（AddField()).  
                foreach (KeyValuePair<string, string> post_arg in get)
                {
                    if (tempFirstBoo)
                        tempFirstBoo = false;
                    else
                        tempParameters += "&";
                    tempParameters += post_arg.Key + "=" + post_arg.Value;
                }
            }
            else
            {
                tempParameters = "";
            }

            //直接URL传值就是get  
            UnityWebRequest tempWebRequest = UnityWebRequest.Get(url + tempParameters);
            yield return tempWebRequest;
            webProgress = tempWebRequest.downloadProgress;

            if (tempWebRequest.error != null)
            {
                //GET请求失败  
                m_ContentLua = "error :" + tempWebRequest.error;
                if (m_WebCallBackLua != null)
                    m_WebCallBackLua.Call(0, m_ContentLua);
            }
            else
            {
                //GET请求成功
                m_ContentLua = tempWebRequest.downloadHandler.text;
                if (m_WebCallBackLua != null)
                    m_WebCallBackLua.Call(1, m_ContentLua);
            }
        }

        //POST请求(Form表单传值、效率低、安全 ，)  
        IEnumerator WebPostCorLua(string canUrl, Dictionary<string, string> canPost)
        {
            WWWForm tempForm = new WWWForm();//表单   
            //从集合中取出所有参数，设置表单参数（AddField()).  
            foreach (KeyValuePair<string, string> post_arg in canPost)
            {
                tempForm.AddField(post_arg.Key, post_arg.Value);
            }
            //表单传值，就是post   
            UnityWebRequest tempWebRequest = UnityWebRequest.Post(canUrl, tempForm);
            yield return tempWebRequest;
            webProgress = tempWebRequest.downloadProgress;

            if (tempWebRequest.error != null)
            {
                //POST请求失败  
                m_ContentLua = "error :" + tempWebRequest.error;
                if (m_WebCallBackLua != null)
                    m_WebCallBackLua.Call(0, m_ContentLua);
            }
            else
            {
                //POST请求成功
                m_ContentLua = tempWebRequest.downloadHandler.text;
                if (m_WebCallBackLua != null)
                    m_WebCallBackLua.Call(1, m_ContentLua);
            }
        }
        #endregion Web请求 xLua
    }

    /// <summary>
    /// Web请求回调数据类
    /// </summary>
    public class WebCallBackArgs
    {
        //是否报错
        public bool isError;
        //错误原因
        public string error;
        //数据
        public string content;
    }
}

