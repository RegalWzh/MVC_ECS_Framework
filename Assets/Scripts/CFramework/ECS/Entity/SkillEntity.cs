//=====================================================
// - FileName:      SkillEntity.cs
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
    public class SkillEntity : BaseEntity
    {
        public AudioComponent audioCom;
        public SkillComponent skillCom;
        public SkillGameObjectComponent skillGameObjectCom;
        public SkillPathComponent skillPathCom;

        public SkillEntity()
        {
            audioCom = new AudioComponent() { baseEntity = this };
            skillCom = new SkillComponent() { baseEntity = this };
            skillGameObjectCom = new SkillGameObjectComponent() { baseEntity = this };
            skillPathCom = new SkillPathComponent() { baseEntity = this };
        }
    }
}
