//=====================================================
// - FileName:      TransferEntity.cs
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
    public class TransferEntity : BaseEntity
    {
        public AudioComponent audioCom;
        public GameObjectComponent gameObjCom;
        public NameComponent nameCom;
        public TransferAttComponent transferIDCom;

        public TransferEntity()
        {
            audioCom = new AudioComponent() { baseEntity = this };
            gameObjCom = new GameObjectComponent() { baseEntity = this };
            nameCom = new NameComponent() { baseEntity = this };
            transferIDCom = new TransferAttComponent() { baseEntity = this };
        }

        public override void Reset()
        {
            base.Reset();

            audioCom.Reset();
            gameObjCom.Reset();
            nameCom.Reset();
            transferIDCom.Reset();
        }
    }
}