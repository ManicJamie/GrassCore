﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GrassCore
{
    public class GrassEventDispatcher
    {

        public static GrassEventDispatcher Instance;

        public GrassEventDispatcher() 
        {
            Instance = this;
        }

        public delegate void GrassWasCut_EventHandler(GrassKey key);
        /// <summary>
        /// Called whenever a grass-like object was cut.
        /// </summary>
        public event GrassWasCut_EventHandler Raw_GrassWasCut;

        public void Raw_GrassWasCut_Invoke(GrassKey key)
        {
            Raw_GrassWasCut?.Invoke(key);
        }

        /// <summary>
        /// Called whenever a verified grass object was cut.
        /// </summary>
        public event GrassWasCut_EventHandler GrassWasCut;

        public void Filter_RawGrassCut(GrassKey key)
        {
            if (GrassList.Contains(key))
            {
                GrassWasCut?.Invoke(key);
            }
        }

        /// <summary>
        /// Called whenever a previously uncut grass object was cut.
        /// </summary>
        public event GrassWasCut_EventHandler UniqueGrassWasCut;

        public void Check_Unique(GrassKey key)
        {
            if (!GrassRegister.Instance.Contains(key))
            {
                if (!GrassRegister.Instance.TryCut(key)) { GrassCore.Instance.LogError($"Tried to cut grass {key} but failed!"); }
                UniqueGrassWasCut?.Invoke(key);
            }
        }
    }
}