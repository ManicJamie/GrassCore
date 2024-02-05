using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GrassCore
{
    /// <summary>
    /// Removes grass on scene load.
    /// </summary>
    public class WeedKiller
    {
        public static WeedKiller Instance { get; set; }

        public Dictionary<string, Dictionary<GrassKey, GrassState>> Blacklist = new();

        public WeedKiller() 
        {
            Instance = this;
        }

        private List<GameObject> GetGrassInScene()
        {
            List<GameObject> result = new();

            foreach (GameObject maybeGrass in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (GrassList.Contains(maybeGrass)) { result.Add(maybeGrass); }
            }

            return result;
        }

        /// <summary>
        /// Removes all grass marked as blacklisted in the scene
        /// </summary>
        public void DestroyBlacklistedGrass(UnityEngine.SceneManagement.Scene source, UnityEngine.SceneManagement.Scene target)
        {
            List<GameObject> GrassGameObjects = GetGrassInScene();
            if (!Blacklist.TryGetValue(target.name, out Dictionary <GrassKey, GrassState> sceneDict)) { return; } // Get scene, if not present we don't need to kill

            foreach (var go in GrassGameObjects)
            {
                GrassKey key = new(go);
                if (sceneDict.ContainsKey(key) && sceneDict[key] == GrassState.Cut)
                {
                    GameObject.Destroy(go);
                }
            }
        }
    }
}
