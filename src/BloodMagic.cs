using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SideLoader;
using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using NodeCanvas.Tasks.Actions;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BloodMagic
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class BloodMagic : BaseUnityPlugin
    {
        public const string GUID = "com.LlamaMage.BloodMagic";
        public const string NAME = "Blood Magic";
        public const string VERSION = "1.0.0";


        public static BloodMagic Instance;


        // For accessing your BepInEx Logger from outside of this class (eg Plugin.Log.LogMessage("");)
        internal static ManualLogSource Log;

        // If you need settings, define them like so:
        //public static ConfigEntry<bool> ExampleConfig;

        // Awake is called when your plugin is created. Use this to set up your mod.
        internal void Awake()
        {
            BloodMagic.Instance = this;

            Log = this.Logger;
            Log.LogMessage($"Hello world from {NAME} {VERSION}!");

            // Any config settings you define should be set up like this:
            //ExampleConfig = Config.Bind("ExampleCategory", "ExampleSetting", false, "This is an example setting.");

            
            //SL.OnPacksLoaded += this.SL_OnPacksLoaded;

            var harmony = new Harmony(GUID);
            harmony.PatchAll();
        }

        // Update is called once per frame. Use this only if needed.
        // You also have all other MonoBehaviour methods available (OnGUI, etc)
        internal void Update()
        {

        }
    }
}
