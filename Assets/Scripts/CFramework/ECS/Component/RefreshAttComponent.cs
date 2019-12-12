//=====================================================
// - FileName:      RefreshTimerComponent.cs
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
    public class RefreshAttComponent : BaseComponent
    {
        public int refreshID = 0;//刷新点ID
        public int refreshCount = 0;//目前所拥有数量
        public TransferExcel transferData = null;//刷新点数据表

        public void Reset()
        {
            refreshID = 0;
            refreshCount = 0;
            transferData = null;
        }
    }
}
