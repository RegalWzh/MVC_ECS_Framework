﻿//=====================================================
// - FileName:      PlayerBornEntity.cs
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
    public class PlayerBornEntity : BaseEntity
    {
        public NameComponent nameCom;
        public PositionComponent positionCom;

        public PlayerBornEntity()
        {
            nameCom = new NameComponent() { baseEntity = this };
            positionCom = new PositionComponent() { baseEntity = this };
        }

        public override void Reset()
        {
            base.Reset();
            nameCom.Reset();
            positionCom.Reset();
        }
    }
}
