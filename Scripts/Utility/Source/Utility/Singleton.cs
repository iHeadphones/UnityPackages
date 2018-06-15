﻿using UnityEngine;

namespace FK.Utility
{
    /// <summary>
    /// Base Class for Singleton MonoBehaviours
    /// 
    /// v1.0 06/2018
    /// Written by Fabian Kober
    /// fabian-kober@gmx.net
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        // ######################## PUBLIC VARS ######################## //
        /// <summary>
        /// The Instance of this Singleton. Might be null if the Singleton is not initialized yet
        /// </summary>
        public static T Instance { get; private set; }

        /// <summary>
        /// Returns true if the Instance of this Singleton is initialized
        /// </summary>
        public static bool IsInitialized
        {
            get { return Instance != null; }
        }

        // ######################## UNITY EVENT FUNCTIONS ######################## //
        void Awake()
        {
            if(!IsInitialized)
            {
                Instance = (T)this;
            } else if(Instance != this)
            {
                if(Application.isEditor)
                {
                    DestroyImmediate(this);
                } else
                {
                    Destroy(this);
                }

                Debug.LogWarningFormat("Tried to Instantiate second instance of Singleton {0}. Additional Instance was destroyed.", typeof(T).Name);
            }
        }

        private void OnDestroy()
        {
            if(Instance == this)
            {
                Instance = null;
            }
        }
    }
}