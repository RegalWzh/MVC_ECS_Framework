//=====================================================
// - FileName:      SingletonMono.cs
// - Created:       mahuibao
// - UserName:      2019-01-01
// - Email:         1023276156@qq.com
// - Description:   MOMO单例
// -  (C) Copyright 2018 - 2019
// -  All Rights Reserved.
//======================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zero.ZeroEngine.Util;


//=====================================================
// - c#
// - fix 检查过命名规则，是否使用对象池、类对象池优化过。
//=====================================================
// - 1.
// - 2.
// - 3.
// - 4.
// - 5.
// - 6.
//======================================================
namespace Zero.ZeroEngine.Core
{
    /// <summary>
    /// MOMO单例
    /// </summary>
    public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj;
                    if (typeof(T).Name.Equals("UIMgr"))
                    {
                        obj = GameObject.Find("UIRoot");
                    }
                    else
                    {
                        obj = new GameObject(typeof(T).Name);
                    }
                    DontDestroyOnLoad(obj);
                    instance = obj.GetOrCreatComPonent<T>();
                }
                return instance;
            }
        }
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public virtual void Dispose()
        {

        }
    }
}
