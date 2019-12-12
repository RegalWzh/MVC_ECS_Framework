//=====================================================
// - FileName:      NpcRoleEntity.cs
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
    public class NpcRoleEntity : BaseEntity
    {
        public ActionComponent actionCom;
        public AudioComponent audioCom;
        public GameObjectComponent gameObjCom;
        public HealthComponent healthCom;
        public NameComponent nameCom;

        public NpcAttComponent npcAttCom;
        public PathComponent pathCom;
        public RoleComponent roleCom;
        public SkillAttComponent skillAttCom;
        public WeaponComponent weaponCom;

        public NpcRoleEntity()
        {
            actionCom = new ActionComponent() { baseEntity = this };
            audioCom = new AudioComponent() { baseEntity = this };
            gameObjCom = new GameObjectComponent() { baseEntity = this };
            healthCom = new HealthComponent() { baseEntity = this };
            nameCom = new NameComponent() { baseEntity = this };

            npcAttCom = new NpcAttComponent() { baseEntity = this };
            pathCom = new PathComponent() { baseEntity = this };
            roleCom = new RoleComponent() { baseEntity = this };
            skillAttCom = new SkillAttComponent() { baseEntity = this };
            weaponCom = new WeaponComponent() { baseEntity = this };
        }

        public override void Reset()
        {
            base.Reset();
            actionCom.Reset();
            audioCom.Reset();
            gameObjCom.Reset();
            healthCom.Reset();
            nameCom.Reset();

            npcAttCom.Reset();
            pathCom.Reset();
            roleCom.Reset();
            skillAttCom.Reset();
            weaponCom.Reset();
        }

    }
}

