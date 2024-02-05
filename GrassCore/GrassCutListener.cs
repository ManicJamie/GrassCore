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

        public static GrassCutListener Instance;
        private GrassEventDispatcher grassEventDispatcher = GrassEventDispatcher.Instance;

        public GrassCutListener()
        {
            Instance = this;
        }

        /* We need to detect cut grass. While hooking GrassCut.OnTriggerEnter2D _should_ be sufficient,
        there's no guarantee some weird grass isn't missing a GrassCut somewhere.
        Hence we'll copy itsjohncs/flibber-hk/GrassyKnight & cache the grass object for the collision,
        then hook the static GrassCut.ShouldCut method to actually register the cut. */


        public void HandleGrassCollisionEnter<OrigFunc, Component>(
            OrigFunc orig,
            Component self,
            Collider2D collision)
        where Component : MonoBehaviour
        where OrigFunc : MulticastDelegate
        {
            var context = new GrassyBox(self.gameObject);
            try
            {
                orig.DynamicInvoke(new object[] { self, collision });
            }
            finally
            {
                context.Dispose();
            }
        }

        public bool HandleShouldCut(On.GrassCut.orig_ShouldCut orig, Collider2D collision)
        {
            // Find out whether the original game code thinks this should be
            // cut. We'll pass this value through no matter what.
            bool shouldCut = orig(collision);

            try
            {
                if (shouldCut)
                {
                    // ShouldCut is a static function so we've hooked every
                    // function that calls ShouldCut. Our hooks will store the
                    // GameObject whose component's method is calling ShouldCut
                    // in this box so that we can grab it out. This could also
                    // be done by walking the stack upwards IF C# let us
                    // examine the argument values of stack frames, but C# does
                    // not give us a good way to do that so here we are.
                    GameObject grass = GrassyBox.GetValue();
                    GrassEventDispatcher.Instance.Raw_GrassWasCut_Invoke(new GrassKey(grass));
                }
            }
            catch (Exception e)
            {
                GrassCore.Instance.LogException("Error in HandleShouldCut", e);

                // Exception stack traces seem to terminate once we're out
                // of this assembly... It doesn't show who called ShouldCut
                // anyways. And that's exactly the information we want if we're
                // looking for more functions to hook HandleGrassCollisionEnter
                // into.
                GrassCore.Instance.LogDebug("More complete stack trace:");
                GrassCore.Instance.LogDebug(IndentString(Environment.StackTrace));
            }

            return shouldCut;
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
