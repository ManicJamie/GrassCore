using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrassCore
{
    /*
     Largely based on GrassyKnight.GrassDB
     */
    public class GrassRegister
    {
        public static GrassRegister Instance;

        // Maps from scene name to a dictionary mapping from grass key to
        // state. The separation of grass by scene is done only for query
        // speed, since GrassKey has the scene name in it already.
        public Dictionary<string, Dictionary<GrassKey, GrassState>> _grassStates = new();

        // Stats; global & per-scene
        public GrassStats _globalStats = new(); 
        public Dictionary<string, GrassStats> _sceneStats = new();

        /// <summary>
        /// Raised when a new grass is cut
        /// </summary>
        public event EventHandler OnStatsChanged;

        public GrassRegister()
        {
            Instance = this;
        }

        private void TryAddScene(string sceneName)
        {
            if (!_grassStates.ContainsKey(sceneName))
            {
                _grassStates.Add(sceneName,
                                new Dictionary<GrassKey, GrassState>());
            }

            if (!_sceneStats.ContainsKey(sceneName))
            {
                _sceneStats.Add(sceneName, new GrassStats());
            }
        }

        public bool TryCut(GrassKey k) => TrySet(k, GrassState.Cut);

        public bool TrySet(GrassKey k, GrassState newState) => TrySet(k, newState, false);

        public bool TrySet(GrassKey k, GrassState newState, bool allowUncut)
        {
            GrassKey canonical = GrassList.Canonical(k);

            TryAddScene(canonical.SceneName);

            GrassState? oldState = null;
            if (_grassStates[canonical.SceneName].TryGetValue(
                    canonical, out GrassState state))
            {
                oldState = state;
            }

            if (oldState == null || (int)oldState < (int)newState || allowUncut)
            {
                _grassStates[canonical.SceneName][canonical] = newState;

                _sceneStats[canonical.SceneName].HandleUpdate(oldState, newState);
                _globalStats.HandleUpdate(oldState, newState);
                OnStatsChanged?.Invoke(this, EventArgs.Empty);

                GrassCore.Instance.LogDebug(
                    $"Updated state of '{canonical}' to {newState} (was {oldState})");
                GrassCore.Instance.LogFine(
                    $"... Serialized key: {String.Join(";", canonical.Serialize())}");

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Contains(GrassKey k)
        {
            GrassKey canonical = GrassList.Canonical(k);
            if (_grassStates.TryGetValue(
                    canonical.SceneName,
                    out Dictionary<GrassKey, GrassState> sceneStates))
            {
                return sceneStates.ContainsKey(canonical);
            }
            else
            {
                return false;
            }
        }


        // Serialization handling from GrassyKnight
        public void Clear()
        {
            _grassStates = new Dictionary<string, Dictionary<GrassKey, GrassState>>();
            _globalStats = new GrassStats();
            _sceneStats = new Dictionary<string, GrassStats>();

            OnStatsChanged?.Invoke(this, EventArgs.Empty);
        }

        private const string _serializationVersion = "1";

        // Serializes the DB into a single string.
        //
        // HollowKnight doesn't ship with
        // System.Runtime.Serialization.Formatters.dll so I don't think
        // it's safe to use a stdlib serializer... Thus we make our own.
        //
        // Format is simple. It's a series of strings seperated by semicolons.
        // First string is the version of the serialization formatter (in case
        // we need to change the format in a back-incompat way). That's
        // followed by GrassKey.NumSerializationTokens number of strings that
        // make up a single grass key, followed by a single string that holds
        // the state of that grass, then it repeats for however many grass
        // states are stored.
        public string Serialize()
        {
            var parts = new List<string>();

            parts.Add(_serializationVersion);

            foreach (Dictionary<GrassKey, GrassState> states in _grassStates.Values)
            {
                foreach (KeyValuePair<GrassKey, GrassState> kv in states)
                {
                    parts.AddRange(kv.Key.Serialize());
                    parts.Add(((int)kv.Value).ToString());
                }
            }

            return String.Join(";", parts.ToArray());
        }

        // Adds all the data in serialized. Will not call Clear() first so you
        // may want to... NOTE: will invoke OnStatsChanged a bunch 🤷‍♀️
        public void AddSerializedData(string serialized)
        {
            if (serialized == null || serialized == "")
            {
                return;
            }

            string[] parts = serialized.Split(';');

            if (parts[0] != _serializationVersion)
            {
                throw new ArgumentException(
                    $"Unknown serialization version {parts[0]}. You may " +
                    $"a new version of the mod to load this save file.");
            }
            else if ((parts.Length - 1) % (GrassKey.NumSerializationTokens + 1) != 0)
            {
                throw new ArgumentException("GrassDB in save data is corrupt");
            }

            string[] grassKeyParts = new string[GrassKey.NumSerializationTokens];
            for (int i = 1; i < parts.Length; i += GrassKey.NumSerializationTokens + 1)
            {
                // Copy just the parts for a single grass key into
                // grassKeyParts.
                Array.Copy(
                    parts, i,
                    grassKeyParts, 0,
                    GrassKey.NumSerializationTokens);
                GrassKey k = GrassKey.Deserialize(grassKeyParts);

                // Convert the one GrassState part into a GrassState
                GrassState state = (GrassState)int.Parse(
                    parts[i + GrassKey.NumSerializationTokens]);

                TrySet(k, state);
            }
        }
    }

    public enum GrassState
    {
        Uncut,
        // A special state that grass might enter if it is struck with the
        // nail but not actually cut in game.
        ShouldBeCut,
        Cut,
    }

    public class GrassStats
    {
        // Maps from GrassState (ex: Cut) to number of grass in that state. I'm
        // curious if there's a way to create a mutable-tuple-of-sorts with the
        // correct size (the number of enum values in GrassState)... but I
        // don't think there is.
        private int[] GrassInState;

        public GrassStats()
        {
            GrassInState = new int[Enum.GetNames(typeof(GrassState)).Length];
        }

        public int Total()
        {
            int sum = 0;
            foreach (int numGrass in GrassInState)
            {
                sum += numGrass;
            }
            return sum;
        }

        public int GetNumGrassInState(GrassState state)
        {
            return GrassInState[(int)state];
        }

        public int this[GrassState state]
        {
            get => GetNumGrassInState(state);
        }

        public void HandleUpdate(GrassState? oldState, GrassState newState)
        {
            if (oldState is GrassState oldStateValue)
            {
                GrassInState[(int)oldStateValue] -= 1;
            }

            GrassInState[(int)newState] += 1;
        }

        public override string ToString()
        {
            string result = "GrassStats(";
            foreach (GrassState state in Enum.GetValues(typeof(GrassState)))
            {
                result += $"{Enum.GetName(typeof(GrassState), state)}=" +
                          $"{GrassInState[(int)state]}, ";
            }
            return result + ")";
        }
    }
}
