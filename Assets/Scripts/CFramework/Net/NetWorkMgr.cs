//=====================================================
// - FileName:      NetWorkMgr.cs
// - Created:       mahuibao
// - UserName:      2019-01-01
// - Email:         1023276156@qq.com
// - Description:   网络运作层
// -  (C) Copyright 2018 - 2019
// -  All Rights Reserved.
//======================================================
using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zero.ZeroEngine.Core;
using Zero.ZeroEngine.Message;
using Zero.ZeroEngine.Util;

//=====================================================
// - c#
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
    public delegate void NetEvenetCallBack(byte[] canData);

    /// <summary>
    /// 网络运作层
    /// </summary>
    public class NetWorkMgr : Singleton<NetWorkMgr>
    {
        //需要发送的数据队列
        private Queue<NetMsgPacket> m_SendMessageQue = new Queue<NetMsgPacket>();
        //接收到的数据队列
        private Queue<NetMsgPacket> m_ReceiveMessageQue = new Queue<NetMsgPacket>();

        //协议事件字典
        private Dictionary<int, NetEvenetCallBack> m_MsgDelegateDic = new Dictionary<int, NetEvenetCallBack>();

        private int connectFailTimes = 0;
        private string m_Ip = string.Empty;
        private int m_Port = 0;
        private bool isBreakNet = false;

        private const string NET_WORK_SEND_COR = "NetWorkSendCor";

        private const string NET_WORK_DEAL_COR = "NetWorkDealCor";
        //处理数据协程每次处理数据量
        private const int m_DealCorCount = 50;

        public virtual void Init()
        {
            CoroutineMgr.Instance.StartCoroutine(NET_WORK_SEND_COR, NetWorkSendCor());
            CoroutineMgr.Instance.StartCoroutine(NET_WORK_DEAL_COR, NetWorkDealCor());
        }
        public virtual void AfterInit()
        { }
        public virtual void Clear()
        {
            m_MsgDelegateDic.Clear();
            m_SendMessageQue.Clear();
            m_ReceiveMessageQue.Clear();
            CoroutineMgr.Instance.StopCoroutine(NET_WORK_SEND_COR, NetWorkSendCor());
            CoroutineMgr.Instance.StopCoroutine(NET_WORK_DEAL_COR, NetWorkDealCor());
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        public void LinkServer(string canIP,int canPort)
        {
            m_Ip = canIP;
            m_Port = canPort;
            ZLogger.Info("连接------->>id: {0} , port: {1}", m_Ip, m_Port);
            NetMgr.Instance.SendConnect(m_Ip, m_Port);
        }

        /// <summary>
        /// 重新连接服务器
        /// </summary>
        public void ReLinkServer()
        {
            NetMgr.Instance.SendConnect(m_Ip, m_Port);
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public void CloseSocket()
        {
            isBreakNet = true;
            m_SendMessageQue.Clear();
            m_ReceiveMessageQue.Clear();
            NetMgr.Instance.CloseSocket();
        }

        /// <summary>
        /// 主动断开并重新启动连接
        /// </summary>
        public void BreakSocket()
        {
            isBreakNet = true;
            NetMgr.Instance.DoDisconnect();
        }

        /// <summary>
        /// 供外部层调用的发送消息
        /// </summary>
        public void SendMsg(int canMsgID,byte[] canMsgData)
        {
            NetMsgPacket tempMsgData = new NetMsgPacket();
            tempMsgData.msgId = canMsgID;
            tempMsgData.data = canMsgData;
            m_SendMessageQue.Enqueue(tempMsgData);
        }

        /// <summary>
        /// 供外部层调用的接收信息
        /// </summary>
        public void OnMsg(NetMsgPacket canMsgPacket)
        {
            NetMsgPacket tempMsgPacket = new NetMsgPacket();
            tempMsgPacket = canMsgPacket;
            m_ReceiveMessageQue.Enqueue(tempMsgPacket);
        }

        /// <summary>
        /// 供外部层调用的解析（反序列化）网络数据
        /// </summary>
        /// <returns></returns>
        public IMessage ParseMsg(IMessage canIMessage, byte[] canMsgData)
        {
            return canIMessage.Descriptor.Parser.ParseFrom(canMsgData);
        }
        

        /// <summary>
        /// 协议注册
        /// </summary>
        public void RegistProtocal(MessageID canMsgType, NetEvenetCallBack canCallBack)
        {
            if (m_MsgDelegateDic.ContainsKey((int)canMsgType))
            {
                ZLogger.Warning("多次订阅同一个协议：{0}", canMsgType);
            }
            else
            {
                m_MsgDelegateDic.Add((int)canMsgType, canCallBack);
            }
        }
        /// <summary>
        /// 移除协议
        /// </summary>
        public void RemoveProtocal(MessageID canType)
        {
            if (m_MsgDelegateDic.ContainsKey((int)canType))
            {
                m_MsgDelegateDic.Remove((int)canType);
            }
        }

        /// <summary>
        /// 是否在连接网状态
        /// </summary>
        private bool IsConnected()
        {
            return NetMgr.Instance.isConnected;
        }

        /// <summary>
        /// 重置连接Socket次数（重新登录使用）
        /// </summary>
        public void ResetLinkTimes()
        {
            NetMgr.Instance.connectTimes = 0;
        }

        //发送信息协程
        IEnumerator NetWorkSendCor()
        {
            while (true)
            {
                if (m_SendMessageQue.Count > 0)
                {
                    NetMsgPacket tempMsgPacket = m_SendMessageQue.Dequeue();
                    if (IsConnected() && !isBreakNet)
                    {
                        if (tempMsgPacket.data != null)
                        {
                            NetMgr.Instance.SendMsg(tempMsgPacket.data, tempMsgPacket.msgId);
                        }
                    }
                    else
                    {
                        BreakSocket();
                    }
                }
                yield return null;
            }
        }

        //处理信息协程
        IEnumerator NetWorkDealCor()
        {
            while (true)
            {
                for(int i = 0; i < m_DealCorCount; i++)
                {
                    if (m_ReceiveMessageQue.Count > 0)
                    {
                        NetMsgPacket tempNetPacket = m_ReceiveMessageQue.Dequeue();
                        NetEvenetCallBack tempEventCallBack;
                        if(m_MsgDelegateDic.TryGetValue(tempNetPacket.msgId,out tempEventCallBack))
                        {
                            if (tempNetPacket.data != null)
                                tempEventCallBack(tempNetPacket.data);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                yield return null;
            }
        }

        /// <summary>
        /// 连接成功时，基础层调用这里传达到上层
        /// </summary>
        public void OnConnect()
        {
            connectFailTimes = 0;
            isBreakNet = false;
            //事件广播 no edit
        }

        /// <summary>
        /// 重新连接时，基础层调用这里传达到上层
        /// </summary>
        public void OnReConnect()
        {
            connectFailTimes = 0;
            isBreakNet = false;
            //事件广播 no edit
        }

        /// <summary>
        /// 主动断开连接时，基础层调用这里传达到上层
        /// </summary>
        public void OnDisconnect()
        {
            connectFailTimes = connectFailTimes + 1;
            isBreakNet = true;
            //事件广播 no edit
        }

        /// <summary>
        /// 连接网络超时或不同提示，跟UI界面接入，基础层调用这里传达到上层
        /// </summary>
        public void ShowTimeOut(string canMsg)
        {
            //no edit
        }
    }
}
