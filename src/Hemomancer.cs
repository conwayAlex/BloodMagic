using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace BloodMagic
{
    class Hemomancer
    {
        //Skill IDs
        public const int HarnessBloodID = -28100;
        public const int EquilibriumID = -28101;
        public const int PiercingCrimsonID = -28102;
        public const int VampiricTouchID = -28103;
        public const int VilePactID = -28104;
        public const int TransfusionID = -28105;
        public const int LeylineAbandonmentID = -28106;
        public const int LeylineEntanglementID = -28107;
        public const int HypovolemiaID = -28108;
        public const int PhlebotomistID = -28109;

        //Status IDs
        public const int HemorrhageID = -28110;
        public const int VileRegenerationID = -28111;
        public const int HumorsMaintenanceID = -28112;


    }

    //Patches

    /* --- Leyline Abandonment Patch(es) ---*/

    // Learning Leyline Abandonment
    //Purpose: Fix mana stats so they're applied correctly to the player.
    /*
     * How it works:
     * If the player is learning the passive, reset the mana points to zero. 
     *      This removes the "manaAugmentation" statstack from applying and the mana points triggering any GUI items.
     * If the player has obtained mana from other sources, calculate the amount to add to the
     * max hp and stamina pools. The extra mana from the stacks in the maxMana resource wont matter since
     * the mana points are gone. 
     * 
     */
    [HarmonyPatch(typeof(CharacterKnowledge), nameof(CharacterKnowledge.AddItem))]
    public class LeylinePassivesLearnedPatch
    {
        static bool Prefix(CharacterKnowledge __instance, Item _item)
        {
            if (_item != null)
            {
                if (_item.ItemID == Hemomancer.LeylineAbandonmentID)
                {
                    //BloodMage.Log.LogMessage("Learning Leyline Abandonment");
                    StatStack statstack;

                    //Learning with mana active
                    if (__instance.m_character.Stats.m_manaPoint > 0)
                    {
                        //BloodMage.Log.LogMessage("Reset Mana Points");
                        //Reset mana points
                        __instance.m_character.Stats.m_manaPoint = 0;
                    }

                    //BloodMage.Log.LogMessage("Evaluating stat stacks");
                    //Grab all stat stacks for mana and convert
                    foreach (var stack in __instance.m_character.Stats.m_maxManaStat.RawStack)
                    {
                        //Skip the dedicated mana points stat
                        if (stack.SourceID.Contains("ManaAugmentation"))
                        {
                            continue;
                        }
                        //Add hp & stam for a 1/4 of mana added from stacks
                        statstack = new StatStack(stack.SourceID, stack.RawValue * .25f, null);
                        __instance.m_character.Stats.m_maxHealthStat.AddRawStack(statstack);
                        __instance.m_character.Stats.m_maxStamina.AddRawStack(statstack);
                    }
                }
            }
            return true;
        }
    }

    //Convening with the Leyline
    //Purpose: Prevent this.
    /*
     * How it works:
     * If you have the passive, you cannot convene with the Leyline to get mana points.
     * All the normal stuff will happen, like learning Spark.
     */
    [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.GiveManaPoint))]
    public class LeylineAbandonmentGiveManaPointPatch
    {
        static void Postfix(CharacterStats __instance)
        {
            if (__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(Hemomancer.LeylineAbandonmentID))
            {
                __instance.m_manaPoint = 0;
            }
        }
    }

    //Learning a mana passive.
    //Purpose: Provides health and stamina from gained mana.
    /*
     * How it works:
     * If you have the passive, determines how much hp and stamina to get and adds the stacks.
     */
    [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.AddStatStack))]
    public class LeylineAbandonmentAddStackPatch
    {
        static bool Prefix(CharacterStats __instance, Tag _stat, StatStack _stack, bool _multiplier)
        {
            if (_stat.TagName == "MaxMana")
            {
                if (__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(Hemomancer.LeylineAbandonmentID))
                {
                    float derived = _stack.RawValue * .25f; //20 mana -> 5 hp and 5 stamina, 1/4 of 20

                    StatStack statstack = new StatStack(_stack.SourceID, derived, null);
                    __instance.m_character.Stats.m_maxHealthStat.AddRawStack(statstack);
                    __instance.m_character.Stats.m_maxStamina.AddRawStack(statstack);

                }
            }

            return true;
        }
    }

    /* --- Leyline Entanglement Patch(es) ---*/

    //Using Consume Soul with Leyline Entanglement
    //Purpose: Halves the mana gained from "Consume Soul" and heals the caster
    /*
     * How it works:
     * Hooks into the ActivateLocally call and runs before it does.
     * Acquire the transform, and figure out where it's coming from, after null checking to avoid angry messages.
     * Figure out if the player has learned the needed passive, then perform the math to split the resource return.
     * Skip the original method logic with a return false.
     */
    [HarmonyPatch(typeof(AffectMana), nameof(AffectMana.ActivateLocally))]
    public class LeylineEntanglementSoulAbsorb
    {
        static bool Prefix(AffectMana __instance, Character _affectedCharacter, object[] _infos)
        {
            Transform t = __instance.transform;

            if (t != null && t.parent != null)
            {
                if (t.parent.name == "ConsumeSoul")
                {
                    if (_affectedCharacter != null && _affectedCharacter.Inventory.SkillKnowledge.IsItemLearned(Hemomancer.LeylineEntanglementID))
                    {
                        //BloodMage.Log.LogMessage("Spark Soul interaction");
                        float amountH = (__instance.Value * (__instance.IsModifier ? (0.01f * _affectedCharacter.Stats.MaxHealth) : 1f)) / 2f;
                        float amountM = (__instance.Value * (__instance.IsModifier ? (0.01f * _affectedCharacter.Stats.MaxMana) : 1f)) / 2f;


                        _affectedCharacter.Stats.AffectHealth(amountH);
                        _affectedCharacter.Stats.RestaureMana(null, amountM);

                        return false;
                    }
                }
            }

            return true;
        }
    }

    //Casting spells
    //Purpose: Check to make sure when Entanglement is learned the user doesn't kill themselves on a cast.
    /*
     * How it works:
     * If you have the passive, figure out how much mana to turn into health.
     * If the player can manage that cost, alert them and skip the original method call.
     * 
     */
    [HarmonyPatch(typeof(Skill), nameof(Skill.HasEnoughHealth))]
    public class LeylinePassivesHealthOverride
    {
        static bool Prefix(Skill __instance, bool _tryingToActivate, ref bool __result)
        {
            if (__instance.m_ownerCharacter.Inventory.SkillKnowledge.IsItemLearned(Hemomancer.LeylineEntanglementID))
            {
                float derived = __instance.ManaCost / 2f;
                __result = (__instance.m_ownerCharacter.Health > derived);

                if (!__result && __instance.m_ownerCharacter.CharacterUI && _tryingToActivate)
                {
                    __instance.m_ownerCharacter.CharacterUI.ShowInfoNotificationLoc("Notification_Skill_NotEnoughHealth");
                }
                return false;
            }
            return true;
        }
    }

    /* --- Leyline Abandonment & Entanglement Patch(es) ---*/

    //Casting spells and using skills
    //Purpose: Overrides resource consumption.
    /*
     * How it works:
     * For each passive, if you have Abandonment, turns entire mana cost to health cost. No mana cost reduction.
     * If you have Entanglement, splits the costs depending on which costs they are.
     */
    [HarmonyPatch(typeof(Skill), nameof(Skill.ConsumeResources))]
    public class LeylinePassivesConsumptionPatch
    {
        static bool Prefix(Skill __instance)
        {
            if (__instance.m_ownerCharacter.Inventory.SkillKnowledge.IsItemLearned(Hemomancer.LeylineAbandonmentID))
            {
                if (__instance.GetStaminaCost() > 0f)
                {
                    return true;
                }

                if (__instance.ManaCost != 0f)
                {
                    __instance.m_ownerCharacter.Stats.ReceiveDamage(__instance.ManaCost);
                }

                if (__instance.HealthCost != 0f)
                {
                    __instance.m_ownerCharacter.Stats.ReceiveDamage(__instance.HealthCost);
                }

                return false;
            }
            else if (__instance.m_ownerCharacter.Inventory.SkillKnowledge.IsItemLearned(Hemomancer.LeylineEntanglementID))
            {
                if (__instance.ManaCost != 0f)
                {
                    float derivedHealth = __instance.ManaCost / 2f;

                    __instance.m_ownerCharacter.Stats.ReceiveDamage(derivedHealth);
                    __instance.m_ownerCharacter.Stats.UseMana(null, derivedHealth);
                }

                if (__instance.HealthCost != 0f)
                {
                    float derivedMana = __instance.HealthCost / 2f;

                    __instance.m_ownerCharacter.Stats.ReceiveDamage(derivedMana);
                    __instance.m_ownerCharacter.Stats.UseMana(null, derivedMana);
                }

                return false;
            }
            return true;
        }
    }

    //Resource checking
    //Purpose: Fixes resource checking in the mana avenue.
    /*
     * How it works:
     * If the player has Abandonment, just check if enough health instead.
     * Otherwise if the player has Entanglement, the cost needs adjusted before deciding
     * if this can be cast or not. Alert if they can't.
     */
    [HarmonyPatch(typeof(Skill), nameof(Skill.HasEnoughMana))]
    public class LeylinePassivesManaOverride
    {
        static bool Prefix(Skill __instance, bool _tryingToActivate, ref bool __result)
        {
            if (__instance.m_ownerCharacter.Inventory.SkillKnowledge.IsItemLearned(Hemomancer.LeylineAbandonmentID))
            {
                __result = __instance.HasEnoughHealth(_tryingToActivate);
                return false;
            }
            else if (__instance.m_ownerCharacter.Inventory.SkillKnowledge.IsItemLearned(Hemomancer.LeylineEntanglementID))
            {
                float derived = (__instance.m_ownerCharacter.Stats) ? __instance.m_ownerCharacter.Stats.GetFinalManaConsumption(null, __instance.ManaCost) : __instance.ManaCost;
                __result = (__instance.m_ownerCharacter.Mana) >= (derived / 2f);

                if (!__result && __instance.m_ownerCharacter.CharacterUI && _tryingToActivate)
                {
                    __instance.m_ownerCharacter.CharacterUI.ShowInfoNotificationLoc("Notification_Skill_NotEnoughMana");
                }
                return false;
            }
            return true;
        }
    }

    /* --- Hypovolemia Patch(es) ---*/

    //Player is bleeding.
    //Purpose: Applies reduction to bleeding damage
    /*
     * How it works:
     * If the player knows the passive, it just adjusts the original return value.
     * This adjusts both the bleed and extreme bleed effects, as they both use this method.
     */
    [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.GetAmplifiedBleedingDamage))]
    public class HypovolemiaBleedDamagePatch
    {
        static void Postfix(CharacterStats __instance, ref float __result)
        {
            if (__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(Hemomancer.HypovolemiaID))
            {
                __result *= .5f;
            }
        }
    }


}
