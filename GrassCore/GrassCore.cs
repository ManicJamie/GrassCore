using Modding;
using MonoMod.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace GrassCore
{
    public class GrassCore : Mod
    {
        public static GrassCore Instance;

        public readonly GrassCutListener grassCutListener;
        public readonly WeedKiller weedKiller;
        public readonly GrassEventDispatcher grassEventDispatcher;
        public readonly GrassRegister_Global grassRegister;

        private readonly WeedKillerEnableHandler weedKillerEnableHandler = new();
        private readonly DisconnectWeedKillerHandler disconnectWeedKillerHandler = new();
        private readonly UniqueCutEnableHandler uniqueCutEnableHandler = new();
        private readonly CutsEnableHandler cutsEnableHandler = new();
        private readonly RawCutsEnableHandler rawCutsEnableHandler = new();

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

        // Passes set properties to their relevant handlers
        private void RegisterMod(EnableHandler handler, bool enabled)
        {
            // A bit smelly, but works to get the caller of the property, and as long as the same mod doesn't call from 2 different places it's fine :)
            // Could use namespace but that risks certain messes arising if 2 mods share a namespace for whatever reason.
            Type m = new StackTrace().GetFrame(2).GetMethod().DeclaringType;
            Log($"Class {m} passed {enabled} to {handler}");
            handler.Set(m, enabled);
        }

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

        public GrassCore() : base("GrassCore")
        {
            Instance = this;
            grassEventDispatcher = new GrassEventDispatcher();
            grassCutListener = new GrassCutListener();
            weedKiller = new WeedKiller();
            grassRegister = new GrassRegister_Global();
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");
            weedKiller.Blacklist = grassRegister._grassStates; // Connect WeedKiller - can be disconnected by setting DisconnectWeedKiller.
            Log("Initialized");
#if DEBUG
            //WeedkillerEnabled = true;
            //UniqueCutsEnabled = true;
#endif
        }

        public static string IndentString(string str, string indent = "... ") => indent + str.Replace("\n", "\n" + indent);
        public void LogException(string heading, Exception error) => LogError($"{heading}\n{IndentString(error.ToString())}");

        public void Log_Unique(GrassKey key)
        {
            Log($"UNIQUE | {key}");
        }

        public void Log_Grass(GrassKey key)
        {
            Log($"GRASS  | {key}");
        }

        public void Log_Raw_Grass(GrassKey key)
        {
            Log($"CUT    | {key}");
        }
    }
}