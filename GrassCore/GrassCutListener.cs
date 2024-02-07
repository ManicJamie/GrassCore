using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GrassCore.GrassCore;
using UnityEngine;

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
         * (Reasonably) Assuming OnTriggerEnter2D is called for all components on the GameObject prior to that on another GameObject,
         * we can simply remember the last grass cut & only fire an event if it's unique. 
         * 
         * We set an expiry to ensure if we go back to that grass later (ie reload scene), we still correctly get a RAW event.
         * This allows us to avoid comparing against an overall state (which UNIQUE does),
         * which downstream mods may want to manage, and avoids a heavy-ish dict lookup.
         * 
         * WARN: Some grass components do not disable themselves & call ShouldCut even after being cut, so RAW/GRASS may be called when slashing an already cut grass.
         * //TODO: read these components' internal state before firing our event, so we know for sure that a RAW or GRASS event is an actual cut.
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
            var context = new GrassyBox(self.gameObject); // Store grass object for ShouldCut static hook
            try
            {
                // Call the original function (which may call our ShouldCut static hook)
                orig.DynamicInvoke(new object[] { self, collision });
            }
            finally
            {
                context.Dispose(); // Empty grass box so we don't accidentally pass ShouldCut the wrong grass!
            }
        }

        public bool HandleShouldCut(On.GrassCut.orig_ShouldCut orig, Collider2D collision)
        {
            bool shouldCut = orig(collision);

            if (shouldCut)
            {
                GameObject grass = GrassyBox.GetValue();
                GrassKey key = new(grass);
                
                // Check this isn't another component on the same object
                if (currentCut == key)
                {
                    if (Time.fixedTime < expiry) 
                    {
                        GrassCore.Instance.LogDebug($"Not firing RAW on duplicate grass event for {key}");
                        return shouldCut;
                    }
                }

                currentCut = key;
                expiry = Time.fixedTime + cutDelay;
                GrassEventDispatcher.Raw_GrassWasCut_Invoke(key);
                
            }

            return shouldCut;
        }

        private bool ShouldCut(Collider2D collision) // Mirror of static GrassCut.ShouldCut
        {
            return (collision.tag == "Nail Attack") || (collision.tag == "Sharp Shadow") ||
                    (collision.tag == "HeroBox" && HeroController.instance.cState.superDashing);
        }
    }

    // From GrassyKnight;
    // A handy box to store some grass in. Used to store a reference to the
    // grass that ShouldCut is getting called for because ShouldCut is a
    // static function.
    class GrassyBox : IDisposable
    {
        private static GameObject _value = null;
        private static bool _hasValue = false;

        public static GameObject GetValue()
        {
            if (_hasValue)
            {
                return _value;
            }
            else
            {
                throw new InvalidOperationException("Nothing in box");
            }
        }

        public GrassyBox(GameObject value)
        {
            if (_hasValue)
            {
                GrassCore.Instance.LogError(
                    $"Already have value in box (current value is {_value}, " +
                    $"trying to store value {value}).");
            }
            else
            {
                _value = value;
                _hasValue = true;
            }
        }

        public void Dispose()
        {
            _value = null;
            _hasValue = false;
        }
    }
}
