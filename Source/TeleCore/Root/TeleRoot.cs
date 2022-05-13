using System;
using UnityEngine;
using Verse;

namespace TeleCore
{
    /// <summary>
    /// Experimental Updating of custom core related parts
    /// </summary>
    public class TeleRoot : MonoBehaviour
    {
        private TeleTickManager internalTickManager;

        public TeleTickManager TickManager => internalTickManager;

        public virtual void Start()
        {
            try
            {
                TFind.TRoot = this;
                internalTickManager = new TeleTickManager();
            }
            catch (Exception arg)
            {
                Log.Error("Error in TiberiumRoot.Start(): " + arg);
            }
        }

        public virtual void Update()
        {
            try
            {
                internalTickManager?.Update();
            }
            catch (Exception arg)
            {
                Log.Error("Error in TiberiumRoot.Update(): " + arg);
            }
        }
    }
}
