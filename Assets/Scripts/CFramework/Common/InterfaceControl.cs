//=====================================================
// - FileName:      InterfaceControl.cs
// - Created:       mahuibao
// - UserName:      2019-01-20
// - Email:         1023276156@qq.com
// - Description:   
// -  (C) Copyright 2018 - 2019
// -  All Rights Reserved.
//======================================================
using Google.Protobuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zero.ZeroEngine.Core;
using Zero.ZeroEngine.Message;
using Zero.ZeroEngine.Net;
using Zero.ZeroEngine.Util;

namespace Zero.ZeroEngine.Common
{
    public class InterfaceControl
    {
        private List<MessageID> m_ProtocList = new List<MessageID>();

        public virtual void Init()
        { }
        public virtual void AfterInit()
        { }
        public virtual void Clear()
        {
            ClearProtocal();
        }

        public void RegistProtocal(MessageID canMsgType, NetEvenetCallBack canCallBack)
        {
            if (m_ProtocList.Contains(canMsgType))
            {
                ZLogger.Warning("协议事件重复注册");
                return;
            }
            else
            {
                m_ProtocList.Add(canMsgType);
            }
            NetWorkMgr.Instance.RegistProtocal(canMsgType, canCallBack);
        }

        public void RemoveProtocal(MessageID canType)
        {
            NetWorkMgr.Instance.RemoveProtocal(canType);
        }

        public void SendMsg(int canMsgID, byte[] canMsgData)
        {
            NetWorkMgr.Instance.SendMsg(canMsgID, canMsgData);
        }

        public IMessage ParseMsg(IMessage canIMessage, byte[] canMsgData)
        {
            return NetWorkMgr.Instance.ParseMsg(canIMessage, canMsgData);
        }

        private void ClearProtocal()
        {
            foreach(MessageID tempId in m_ProtocList)
            {
                RemoveProtocal(tempId);
            }
        }
    }
}