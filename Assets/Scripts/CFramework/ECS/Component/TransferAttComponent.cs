//=====================================================
// - FileName:      TransferIDComponent.cs
// - Created:       mahuibao
// - UserName:      2019-01-01
// - Email:         1023276156@qq.com
// - Description:   
// -  (C) Copyright 2018 - 2019
// -  All Rights Reserved.
//======================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zero.ZeroEngine.ECS
{
    public class TransferAttComponent : BaseComponent
    {
        public int transferID = 0;//传送点ID
        public TransferExcel transferData = null;//传送点数据表
        public void Reset()
        {
            transferID = 0;
            transferData = null;
        }
    }
}
