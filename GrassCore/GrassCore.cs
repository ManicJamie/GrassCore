using Modding;
using MonoMod.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace GrassCore
{
    public class GrassCore : Mod
    {
        private static GrassCore instance;
        public static GrassCore Instance { get { instance ??= new GrassCore(); return instance; } }

        // Module singletons
        public readonly GrassCutListener grassCutListener = GrassCutListener.Instance;
        public readonly WeedKiller weedKiller = WeedKiller.Instance;
        public readonly GrassRegister_Global grassRegister = GrassRegister_Global.Instance;

        // Enable handler singletons
        private readonly WeedKillerEnableHandler weedKillerEnableHandler = WeedKillerEnableHandler.Instance;
        private readonly DisconnectWeedKillerHandler disconnectWeedKillerHandler = DisconnectWeedKillerHandler.Instance;
        private readonly UniqueCutEnableHandler uniqueCutEnableHandler = UniqueCutEnableHandler.Instance;
        private readonly CutsEnableHandler cutsEnableHandler = CutsEnableHandler.Instance;
        private readonly RawCutsEnableHandler rawCutsEnableHandler = RawCutsEnableHandler.Instance;

        // Properties for downstream users to set as needed
        /// <summary>
        /// Please note that setting this to false does not guarantee the weedkiller will be disabled, as other mods may still request it.
        /// </summary>
        public bool WeedkillerEnabled { get => weedKillerEnableHandler.Get(); set => RegisterMod(weedKillerEnableHandler, value); }
        /// <summary>
        /// Disable default weedkiller behaviour, allowing other mods to manually set WeedKiller.Blacklist to their own records.
        /// </summary>
        public bool DisconnectWeedKiller { get => disconnectWeedKillerHandler.Get(); set => RegisterMod(disconnectWeedKillerHandler, value); }
        public bool UniqueCutsEnabled { get => uniqueCutEnableHandler.Get(); set => RegisterMod(uniqueCutEnableHandler, value); }
        public bool CutsEnabled { get => cutsEnableHandler.Get(); set => RegisterMod(cutsEnableHandler, value); }
        public bool RawCutsEnabled { get => rawCutsEnableHandler.Get(); set => RegisterMod(rawCutsEnableHandler, value); }

        /// <summary>
        /// Passes set properties to their relevant handlers
        /// </summary>
        private void RegisterMod(EnableHandler handler, bool enabled)
        {
            // A bit smelly, but works to get the caller of the property, and as long as the same mod doesn't call from 2 different places it's fine :)
            // Could use namespace but that risks certain messes arising if 2 mods share a namespace for whatever reason.
            Type m = new StackTrace().GetFrame(2).GetMethod().DeclaringType;
            Log($"{m.Name} passed {enabled} to {handler}");
            handler.Set(m, enabled);
        }

        /* MAPI interface */

        public MySaveData OnSaveLocal() => new MySaveData
        {
            serializedGrassRegister = grassRegister.Serialize()
        };

        public void OnLoadLocal(MySaveData value)
        {
            grassRegister.Clear();
            grassRegister.AddSerializedData(value.serializedGrassRegister);
        }

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        /* Constructors */

        public GrassCore() : base("GrassCore")
        {
            // All the singleton constructors are above.
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            weedKiller.Blacklist = grassRegister._grassStates; // Connect WeedKiller - can be disconnected by setting DisconnectWeedKiller.

            GrassEventDispatcher.UniqueGrassWasCut += Log_Unique;
            GrassEventDispatcher.GrassWasCut += Log_Grass;
            GrassEventDispatcher.Raw_GrassWasCut += Log_Raw_Grass;
            
            #if DEBUG
            // Self-enable on debug builds
            WeedkillerEnabled = true;
            CutsEnabled = true;
            #endif
        }

        /* Utils */

        public static string IndentString(string str, string indent = "... ") => indent + str.Replace("\n", "\n" + indent);
        public void LogException(string heading, Exception error) => LogError($"{heading}\n{IndentString(error.ToString())}");

        public void Log_Unique(GrassKey key) =>     LogDebug($"UNIQUE | {key}");

        public void Log_Grass(GrassKey key) =>      LogDebug($"GRASS  | {key}");

        public void Log_Raw_Grass(GrassKey key) =>  LogDebug($"CUT    | {key}");
    }
}