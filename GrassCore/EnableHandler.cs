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
        protected abstract void OnEnable();
        protected abstract void OnDisable();

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

        protected static List<bool> GetBools() => new List<bool>(all.Select((e) => e.Get()));

        public static void Log(object message) => GrassCore.Instance.Log(message);
    }

    public class WeedKillerEnableHandler : EnableHandler
    {
        private static WeedKillerEnableHandler instance;
        public static WeedKillerEnableHandler Instance { get { instance ??= new WeedKillerEnableHandler(); return instance; } }

        public override bool Get() => users.Count > 0;

        protected override void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += WeedKiller.Instance.DestroyBlacklistedGrass;
        }

        protected override void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= WeedKiller.Instance.DestroyBlacklistedGrass;
        }
    }

    public class DisconnectWeedKillerHandler : EnableHandler
    {
        private static DisconnectWeedKillerHandler instance;
        public static DisconnectWeedKillerHandler Instance { get { instance ??= new DisconnectWeedKillerHandler(); return instance; } }

        public override bool Get() => users.Count > 0;

        protected override void OnEnable()
        {
            WeedKiller.Instance.Blacklist = new(); // This looks stupid, but downstream can use this dict as desired as long as they don't disable it themselves.
        }

        protected override void OnDisable()
        {
            WeedKiller.Instance.Blacklist = GrassRegister_Global.Instance._grassStates; // Go back to default connected behaviour
        }

    }

    public class UniqueCutEnableHandler : EnableHandler
    {
        private static UniqueCutEnableHandler instance;
        public static UniqueCutEnableHandler Instance { get { instance ??= new UniqueCutEnableHandler(); return instance; } }

        public override bool Get() => users.Count > 0;

        protected override void OnEnable()
        {
            GrassEventDispatcher.GrassWasCut += GrassEventDispatcher.Check_Unique;
        }

        protected override void OnDisable()
        {
            GrassEventDispatcher.GrassWasCut -= GrassEventDispatcher.Check_Unique;
        }
    }

    public class CutsEnableHandler : EnableHandler
    {
        private static CutsEnableHandler instance;
        public static CutsEnableHandler Instance { get { instance ??= new CutsEnableHandler(); return instance; } }

        public override bool Get() => users.Count > 0 || UniqueCutEnableHandler.Instance.Get();

        protected override void OnEnable()
        {
            GrassEventDispatcher.Raw_GrassWasCut += GrassEventDispatcher.Check_IsGrass;
        }

        protected override void OnDisable()
        {
            GrassEventDispatcher.Raw_GrassWasCut -= GrassEventDispatcher.Check_IsGrass;
        }
    }

    public class RawCutsEnableHandler : EnableHandler
    {
        private static RawCutsEnableHandler instance;
        public static RawCutsEnableHandler Instance { get { instance ??= new RawCutsEnableHandler(); return instance; } }

        public override bool Get() => users.Count > 0 || CutsEnableHandler.Instance.Get();

        protected override void OnEnable()
        {
            // GrassBox fillers
            On.GrassCut.OnTriggerEnter2D += GrassCutListener.Instance.HandleGrassCollisionEnter;
            On.TownGrass.OnTriggerEnter2D += GrassCutListener.Instance.HandleGrassCollisionEnter;
            On.GrassSpriteBehaviour.OnTriggerEnter2D += GrassCutListener.Instance.HandleGrassCollisionEnter;
            On.GrassBehaviour.OnTriggerEnter2D += GrassCutListener.Instance.HandleGrassCollisionEnter;
            // Cut dispatcher - we need to track grass state
            On.GrassCut.ShouldCut += GrassCutListener.Instance.HandleShouldCut;
        }

        protected override void OnDisable()
        {
            On.GrassCut.OnTriggerEnter2D -= GrassCutListener.Instance.HandleGrassCollisionEnter;
            On.TownGrass.OnTriggerEnter2D -= GrassCutListener.Instance.HandleGrassCollisionEnter;
            On.GrassSpriteBehaviour.OnTriggerEnter2D -= GrassCutListener.Instance.HandleGrassCollisionEnter;
            On.GrassBehaviour.OnTriggerEnter2D -= GrassCutListener.Instance.HandleGrassCollisionEnter;

            On.GrassCut.ShouldCut -= GrassCutListener.Instance.HandleShouldCut;
        }
    }
}