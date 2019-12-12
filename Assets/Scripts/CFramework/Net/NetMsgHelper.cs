//=====================================================
// - FileName:      NetMsgHelper.cs
// - Created:       mahuibao
// - UserName:      2019-01-01
// - Email:         1023276156@qq.com
// - Description:   网络管理辅助层
// -  (C) Copyright 2018 - 2019
// -  All Rights Reserved.
//======================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zero.ZeroEngine.Core;

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
    /// 网络管理辅助层
    /// </summary>
    public class NetMsgHelper : Singleton<NetMsgHelper>
    {
        //内部缓冲区
        private byte[] m_BufferData;
        //内部缓冲区默认大小
        private int m_DefaultLen = NetConst.TEMP_MSG_SIZE;
        //标识接收到的字节数（也就是缓冲区中，最后一个数据的位置）
        private int m_CurPos = 0;

        //当前处理的包中的协议ID
        private int m_MsgId;
        //当前处理的包总长度
        private int m_MsgTotalLen = 0;
        //当前处理的包中的数据部分长度
        private int m_MsgDataLen = 0;

        public void Init()
        {
            m_BufferData = new byte[m_DefaultLen];
        }

        public void Clear()
        {
            m_BufferData = new byte[m_DefaultLen];
            m_DefaultLen = NetConst.TEMP_MSG_SIZE;
            m_CurPos = 0;
            m_MsgId = 0;
            m_MsgTotalLen = 0;
            m_MsgDataLen = 0;
        }

        public void AfterInit()
        {

        }
        
        /// <summary>
        /// 添加Socket接收的数据到缓冲区中
        /// </summary>
        public void AddData(byte[] canData,int canLen)
        {
            if(canLen > m_BufferData.Length - m_CurPos)
            {
                byte[] tempBufferData = new byte[m_CurPos + canLen];
                Array.Copy(m_BufferData, 0, tempBufferData, 0, m_CurPos);
                Array.Copy(canData, 0, tempBufferData, m_CurPos, canLen);
                m_BufferData = tempBufferData;
                tempBufferData = null;
            }
            else
            {
                Array.Copy(canData, 0, m_BufferData, m_CurPos, canLen);
            }
            m_CurPos += canLen;
        }

        /// <summary>
        /// 从缓冲区中，提取出一个数据包
        /// </summary>
        public bool GetOneMsg(ref NetMsgPacket canMsgPacket)
        {
            if(m_MsgTotalLen <= 0)
            {
                UpdateLength();
            }
            if (m_MsgTotalLen > 0 && m_MsgTotalLen <= m_CurPos)
            {
                canMsgPacket.msgId = m_MsgId;
                canMsgPacket.data = new byte[m_MsgDataLen];
                Array.Copy(m_BufferData, NetConst.MSG_HEAD_LEN, canMsgPacket.data, 0, m_MsgDataLen);

                m_CurPos -= m_MsgTotalLen;
                byte[] tempBufferData = new byte[m_CurPos > NetConst.TEMP_MSG_SIZE ? m_CurPos : NetConst.TEMP_MSG_SIZE];
                Array.Copy(m_BufferData, m_MsgTotalLen, tempBufferData, 0, m_CurPos);
                m_BufferData = tempBufferData;
                tempBufferData = null;

                m_MsgTotalLen = 0;
                m_MsgDataLen = 0;
                m_MsgId = 0;
                return true;
            }
            return false;
        }

        //更新当前缓冲区中，存在的第一个需要处理的包的总长度、协议id、数据长度
        private void UpdateLength()
        {
            if(m_MsgDataLen == 0 && m_CurPos >= NetConst.MSG_HEAD_LEN)
            {
                //解析缓冲区中当前处理包的总长度
                byte[] tempLenData = new byte[NetConst.MSG_DATA_LEN_LEN];
                Array.Copy(m_BufferData, 0, tempLenData, 0, NetConst.MSG_DATA_LEN_LEN);
                m_MsgTotalLen = BitConverter.ToInt32(tempLenData, 0);

                //解析缓冲区中当前处理包的协议ID
                byte[] tempIdData = new byte[NetConst.MSG_ID_LEN];
                Array.Copy(m_BufferData, NetConst.MSG_DATA_LEN_LEN, tempIdData, 0, NetConst.MSG_ID_LEN);
                m_MsgId = BitConverter.ToInt32(tempIdData, 0);

                m_MsgDataLen = m_MsgTotalLen - NetConst.MSG_HEAD_LEN;
            }
        }


    }
}
