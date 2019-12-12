﻿//=====================================================
// - FileName:      MonsterBelongComponent.cs
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
    public class MonsterAttComponent : BaseComponent
    {
        public int monsterID = 0;//怪物ID
        public int monsterBelongID = 0;//怪物所属刷新点ID
        public int monsterSummonBoo = 0;//怪物是否属于召唤物
        public int monsterMasterID = 0;//怪物主人
        public MonsterExcel monsterData = null;//怪物数据表

        public void Reset()
        {
            monsterID = 0;
            monsterBelongID = 0;
            monsterSummonBoo = 0;
            monsterMasterID = 0;
            monsterData = null;
        }
    }
}
