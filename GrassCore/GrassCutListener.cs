using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GrassCore.GrassCoreMod;
using UnityEngine;
using System.Reflection;

namespace GrassCore
{
    /// <summary>
    /// Handles ingame grass cut events
    /// </summary>
    public class GrassCutListener
    {
        /// <summary>
        /// Time to wait before allowing a second Cut event to register for the same grass object. Allows cutting, respawning grass then cutting again.
        /// </summary>
        private const float cutDelay = 0.1f;

        private static GrassCutListener instance;
        public static GrassCutListener Instance { get { instance ??= new GrassCutListener(); return instance; } }



        /* We need to detect cut grass. Grass may have 1 or more of 4 Grass-related components, 
         * all of which call static GrassCut.ShouldCut when they actually check to be cut.
         * The static ShouldCut does not receive an instance of the grass itself though, only the collider activating it.
         * 
         * Hence we'll copy itsjohncs/flibber-hk/GrassyKnight & cache the grass object for the collision,
         * then hook the static GrassCut.ShouldCut method to actually register the cut. 
         * This works for UNIQUE, but we get multiple RAW/GRASS on some grass!
         * 
         * Some grasses have 2 or more of the Grass-related components, so will call shouldCut multiple times for each cut event.
         * (Reasonably) Assuming OnTriggerEnter2D is called for all components on the GameObject before another GameObject,
         * we can simply remember the last grass cut & only fire an event if it's unique. 
         * 
         * We set an expiry to ensure if we go back to that grass later (ie reload scene), we still correctly get a RAW event.
         * This allows us to avoid comparing against an overall state (which UNIQUE does),
         * which downstream mods may want to manage, and avoids a heavy-ish dict lookup.
         * 
         * GrassBehaviour also checks shouldCut even when it is already cut; we reflect the private isCut field &
         * check before invoking RAW to avoid getting spurious events after a grass is already cut.
         */
        private GrassKey currentCut;
        private float expiry;

        public void HandleGrassCollisionEnter<OrigFunc, Component>(
            OrigFunc orig,
            Component self,
            Collider2D collision)
        where Component : MonoBehaviour
        where OrigFunc : MulticastDelegate
        {
            GrassyBox.Component = self; // Store grass component for ShouldCut to grab
            try
            {
                // Call the original function (which may call our ShouldCut static hook)
                orig.DynamicInvoke(new object[] { self, collision });
            }
            finally
            {
                GrassyBox.Dispose(); // Empty grass box so we don't accidentally pass ShouldCut the wrong grass!
            }
        }

        /// <summary>
        /// Reflected private field of GrassBehaviour.isCut for checking if the GrassBehaviour is supposed to be silent
        /// </summary>
        private static readonly FieldInfo isCutFI = typeof(GrassBehaviour).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where((f) => f.Name == "isCut").First();

        public bool HandleShouldCut(On.GrassCut.orig_ShouldCut orig, Collider2D collision)
        {
            bool shouldCut = orig(collision);

            if (shouldCut)
            {
                MonoBehaviour component = GrassyBox.Component;
                GrassKey key = new(component.gameObject);
                // Check for GrassBehaviour to avoid cut events on grass that's already been cut
                if (component.GetType() == typeof(GrassBehaviour))
                {
                    GrassBehaviour gb = (GrassBehaviour)component;
                    bool isCut = (bool)isCutFI.GetValue(gb);
                    if (isCut) {
                        GrassCoreMod.Instance.LogFine($"Not firing RAW on false GrassBehaviour event for {key}");
                        return shouldCut; 
                    } 
                }

                // Check this isn't another component on the same object as the last ShouldCut call
                if (currentCut == key)
                {
                    if (Time.fixedTime < expiry) 
                    {
                        GrassCoreMod.Instance.LogFine($"Not firing RAW on duplicate grass event for {key}");
                        return shouldCut;
                    }
                }

                currentCut = key;
                expiry = Time.fixedTime + cutDelay;
                GrassEventDispatcher.Raw_GrassWasCut_Invoke(key);
            }

            return shouldCut;
        }
    }

    // From GrassyKnight;
    // A handy box to store some grass in. Used to store a reference to the
    // grass that ShouldCut is getting called for because ShouldCut is a
    // static function.
    static class GrassyBox
    {
        private static MonoBehaviour _component = null;

        public static MonoBehaviour Component { get {
                if (_component != null) { return _component; }
                else { throw new InvalidOperationException("Nothing in box"); }
            } set {
                _component = value;
            }
        }

        public static void Dispose()
        {
            _component = null;
        }
    }
}
