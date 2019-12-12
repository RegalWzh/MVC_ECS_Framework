//=====================================================
// - FileName:      NpcIDComponent.cs
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
    public class NpcAttComponent : BaseComponent
    {
        public int npcID = 0;//NPC_ID
        public NpcExcel npcData = null;//NPC数据表
        public void Reset()
        {
            npcID = 0;
            npcData = null;
        }
    }
}
