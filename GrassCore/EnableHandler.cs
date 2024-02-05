using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrassCore
{
    // Defines sets of enable flags & callers
    public abstract class EnableHandler
    {
        public static List<EnableHandler> all = new();

        public HashSet<Type> users = new();
        public abstract bool Get();

        protected delegate void ParamsAction(params object[] arguments);

        public abstract void OnEnable();
        public abstract void OnDisable();

        public EnableHandler()
        {
            all.Add(this);
        }

        public void Set(Type caller, bool enabled)
        {
            List<bool> former = GetBools();

            if (enabled) { users.Add(caller); }
            else { users.Remove(caller); }

            List<bool> after = GetBools();

            for (int i = 0; i < all.Count; i++)
            {
                if (former[i] == false && after[i] == true) 
                {
                    Log($"Enabling {all[i]}...");
                    all[i].OnEnable();
                }
                if (former[i] == true && after[i] == false) 
                {
                    Log($"Disabling {all[i]}...");
                    all[i].OnDisable(); 
                }
            }
        }

        public static List<bool> GetBools()
        {
            List<bool> temp = new();
            foreach (var enable in all)
            {
                temp.Add(enable.Get());
            }
            return temp;
        }

        public static void Log(object message) => GrassCore.Instance.Log(message);
    }

    public class WeedKillerEnableHandler : EnableHandler
    {
        public static EnableHandler Instance;

        public WeedKillerEnableHandler() : base()
        {
            Instance = this;
        }

        public override bool Get() => users.Count > 0;

        public override void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += WeedKiller.Instance.DestroyBlacklistedGrass;
        }

        public override void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= WeedKiller.Instance.DestroyBlacklistedGrass;
        }
    }

    public class DisconnectWeedKillerHandler : EnableHandler
    {
        public static EnableHandler Instance;

        public DisconnectWeedKillerHandler() : base()
        {
            Instance = this;
        }

        public override bool Get() => users.Count > 0;

        public override void OnEnable()
        {
            WeedKiller.Instance.Blacklist = new(); // This looks stupid, but downstream can use this dict as desired as long as they don't disable it themselves.
        }

        public override void OnDisable()
        {
            WeedKiller.Instance.Blacklist = GrassRegister.Instance._grassStates; // Go back to default connected behaviour
        }

    }

    public class UniqueCutEnableHandler : EnableHandler
    {
        public static EnableHandler Instance;

        public UniqueCutEnableHandler() : base()
        {
            Instance = this;
        }

        public override bool Get() => users.Count > 0;

        public override void OnEnable()
        {
            GrassEventDispatcher.Instance.GrassWasCut += GrassEventDispatcher.Instance.Check_Unique;

            #if DEBUG
            GrassEventDispatcher.Instance.UniqueGrassWasCut += GrassCore.Instance.Log_Unique;
            #endif
        }

        public override void OnDisable()
        {
            GrassEventDispatcher.Instance.GrassWasCut -= GrassEventDispatcher.Instance.Check_Unique;

            #if DEBUG
            GrassEventDispatcher.Instance.UniqueGrassWasCut -= GrassCore.Instance.Log_Unique;
            #endif
        }
    }

    public class CutsEnableHandler : EnableHandler
    {
        public static EnableHandler Instance;

        public CutsEnableHandler() : base()
        {
            Instance = this;
        }

        public override bool Get() => users.Count > 0 || UniqueCutEnableHandler.Instance.Get();

        public override void OnEnable()
        {
            GrassEventDispatcher.Instance.Raw_GrassWasCut += GrassEventDispatcher.Instance.Filter_RawGrassCut;

            #if DEBUG
            GrassEventDispatcher.Instance.GrassWasCut += GrassCore.Instance.Log_Grass;
            #endif
        }

        public override void OnDisable()
        {
            GrassEventDispatcher.Instance.Raw_GrassWasCut -= GrassEventDispatcher.Instance.Filter_RawGrassCut;

            #if DEBUG
            GrassEventDispatcher.Instance.GrassWasCut -= GrassCore.Instance.Log_Grass;
            #endif
        }
    }

    public class RawCutsEnableHandler : EnableHandler
    {
        public static EnableHandler Instance;

        public RawCutsEnableHandler() : base()
        {
            Instance = this;
        }

        public override bool Get() => users.Count > 0 || CutsEnableHandler.Instance.Get();

        public override void OnEnable()
        {
            // Grass cut handling from GrassyKnight. These ensure the static GrassyBox is filled when ShouldCut is called.
            On.GrassBehaviour.OnTriggerEnter2D += GrassCutListener.Instance.HandleGrassCollisionEnter;
            On.GrassCut.OnTriggerEnter2D += GrassCutListener.Instance.HandleGrassCollisionEnter;
            On.TownGrass.OnTriggerEnter2D += GrassCutListener.Instance.HandleGrassCollisionEnter;
            On.GrassSpriteBehaviour.OnTriggerEnter2D += GrassCutListener.Instance.HandleGrassCollisionEnter;

            // Triggered when real grass is being cut for real
            On.GrassCut.ShouldCut += GrassCutListener.Instance.HandleShouldCut;

            #if DEBUG
            GrassEventDispatcher.Instance.Raw_GrassWasCut += GrassCore.Instance.Log_Raw_Grass;
            #endif
        }

        public override void OnDisable()
        {
            
            On.GrassBehaviour.OnTriggerEnter2D -= GrassCutListener.Instance.HandleGrassCollisionEnter;
            On.GrassCut.OnTriggerEnter2D -= GrassCutListener.Instance.HandleGrassCollisionEnter;
            On.TownGrass.OnTriggerEnter2D -= GrassCutListener.Instance.HandleGrassCollisionEnter;
            On.GrassSpriteBehaviour.OnTriggerEnter2D -= GrassCutListener.Instance.HandleGrassCollisionEnter;

            
            On.GrassCut.ShouldCut -= GrassCutListener.Instance.HandleShouldCut;

            #if DEBUG
            GrassEventDispatcher.Instance.Raw_GrassWasCut -= GrassCore.Instance.Log_Raw_Grass;
            #endif
        }
    }
}