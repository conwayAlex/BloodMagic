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
using System.Collections;

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
            Log.LogMessage($"{NAME} {VERSION} loading...");

            // Any config settings you define should be set up like this:
            //ExampleConfig = Config.Bind("ExampleCategory", "ExampleSetting", false, "This is an example setting.");

            
            //SL.OnPacksLoaded += this.SL_OnPacksLoaded;

            var harmony = new Harmony(GUID);
            harmony.PatchAll();
            Log.LogMessage($"{NAME} {VERSION} loaded.");
        }

        //private void SL_OnPAcksLoaded

        // Update is called once per frame. Use this only if needed.
        // You also have all other MonoBehaviour methods available (OnGUI, etc)
        internal void Update()
        {

        }

        //The below code does not belong to me, taken from Emo on the OW modding discord
        public static Tag GetTagDefinition(string TagName)
        {
            foreach (var item in TagSourceManager.Instance.m_tags)
            {

                if (item.TagName == TagName)
                {
                    return item;
                }
            }

            return default(Tag);
        }


        [HarmonyPatch(typeof(Character), nameof(Character.Start))]
        public class EnchantableBackpack
        {
            public static void Postfix(Character __instance)
            {
                DelayDo(() =>
                {
                    if (__instance.CharacterUI)
                    {
                        __instance.CharacterUI.EnchantmentMenu.m_inventoryDisplay.m_filter.m_equipmentTypes.Add(EquipmentSlot.EquipmentSlotIDs.Back);
                        Log.LogMessage($"Enchantment Menu updated.");
                    }
                }, 10f);
            }

            public static void DelayDo(Action OnAfterDelay, float DelayTime)
            {
                BloodMagic.Instance.StartCoroutine(DoAfterDelay(OnAfterDelay, DelayTime));
            }

            public static IEnumerator DoAfterDelay(Action OnAfterDelay, float DelayTime)
            {
                yield return new WaitForSeconds(DelayTime);
                OnAfterDelay.Invoke();
                yield break;
            }
        }
        



        //Thank you Faeryn for the patch on this!
        [HarmonyPatch(typeof(EffectCondition))]
        public static class EffectConditionPatches
        {
            [HarmonyPatch(nameof(EffectCondition.IsValid)), HarmonyPrefix]
            private static bool EffectCondition_IsValid_Prefix(Character _affectedCharacter, ref bool __result)
            {
                if (_affectedCharacter == null)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
    }
}
