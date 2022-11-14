using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace BloodMagic


{
    class BloodMage
    {
        //Summon Blood Weapon
        public const int SummonBloodWeaponID = -28050;
        public const int BloodSwordID = -28051;
        public const int SummonBloodSpearID = -28052;
        public const int BloodSpearID = -28053;

        //Blood Sacrifice 
        public const int BloodSacrificeID = -28054;

        //Chakram Skill

        //Corrupted Devotion
        public const int CorruptedDevotionID = 000000000;

        //Summon Bloody Beast

        //True Power
        public const int TruePowerID = 0000000;
        //Blood Sigil

        //Chakram Skill


        //Basically just going to imitate the Runic Blade class
        public class SummonBloodWeapon : Effect
        {
            public Weapon BloodSwordPrefab;
            public Weapon BloodSpearPrefab;
            public float SummonLifespan = 180f;

            public override void ActivateLocally(Character _affectedCharacter, object[] _infos)
            {
                if (_affectedCharacter)
                {
                    if (this.BloodSwordPrefab && !_affectedCharacter.Inventory.HasEquipped(this.BloodSwordPrefab.ItemID))
                    {
                        Weapon weapon = ItemManager.Instance.GenerateItem(this.BloodSwordPrefab.ItemID) as Weapon;
                        weapon.SetHolderUID(_affectedCharacter.UID + "_" + this.BloodSwordPrefab.name);
                        weapon.ClientGenerated = PhotonNetwork.isNonMasterClientInRoom;
                        weapon.SetKeepAlive();

                        Item equippedItem = _affectedCharacter.Inventory.Equipment.GetEquippedItem(EquipmentSlot.EquipmentSlotIDs.RightHand);
                        SummonedEquipment component = weapon.GetComponent<SummonedEquipment>();
                        float lifespan = EnvironmentConditions.ConvertToGameTime(this.SummonLifespan);
                        component.Activate(lifespan, equippedItem ? equippedItem.UID : null, null);

                        if (equippedItem)
                        {
                            _affectedCharacter.Inventory.UnequipItem((Equipment)equippedItem);
                            equippedItem.ForceUpdateParentChange();
                        }
                        weapon.transform.SetParent(_affectedCharacter.Inventory.GetMatchingEquipmentSlot(EquipmentSlot.EquipmentSlotIDs.RightHand).transform);
                        weapon.ForceStartInit();
                        return;
                    }
                    if (this.BloodSpearPrefab && this.BloodSwordPrefab &&
                        _affectedCharacter.Inventory.SkillKnowledge.IsItemLearned(SummonBloodSpearID) &&
                        _affectedCharacter.Inventory.HasEquipped(this.BloodSwordPrefab.ItemID))
                    {
                        Item equippedItem2 = _affectedCharacter.Inventory.Equipment.GetEquippedItem(EquipmentSlot.EquipmentSlotIDs.RightHand);
                        SummonedEquipment component2 = equippedItem2.GetComponent<SummonedEquipment>();
                        Weapon weapon2 = ItemManager.Instance.GenerateItemNetwork(this.BloodSpearPrefab.ItemID) as Weapon;
                        weapon2.SetHolderUID(_affectedCharacter.UID + "_" + this.BloodSpearPrefab.name);
                        weapon2.ClientGenerated = PhotonNetwork.isNonMasterClientInRoom;
                        weapon2.SetKeepAlive();
                        Item equippedItem3 = _affectedCharacter.Inventory.Equipment.GetEquippedItem(EquipmentSlot.EquipmentSlotIDs.LeftHand);
                        SummonedEquipment component3 = weapon2.GetComponent<SummonedEquipment>();
                        float lifespan2 = EnvironmentConditions.ConvertToGameTime(this.SummonLifespan);
                        component3.Activate(lifespan2, component2.PreviousRightHand, (equippedItem3 && equippedItem3.UID != component2.PreviousRightHand) ? equippedItem3.UID : null);

                        if (equippedItem2)
                        {
                            _affectedCharacter.Inventory.UnequipItem((Equipment)equippedItem2);
                            equippedItem2.ForceUpdateParentChange();
                        }
                        if (equippedItem3)
                        {
                            _affectedCharacter.Inventory.UnequipItem((Equipment)equippedItem3);
                            equippedItem3.ForceUpdateParentChange();
                        }
                        weapon2.transform.SetParent(_affectedCharacter.Inventory.GetMatchingEquipmentSlot(EquipmentSlot.EquipmentSlotIDs.RightHand).transform);
                        weapon2.ForceStartInit();
                    }
                }
            }
        }

    }

    //Patches

    //Corrupted Devotion
    [HarmonyPatch(typeof(AffectCorruption), nameof(AffectCorruption.ActivateLocally))]
    public class CorruptionLimits
    {
        static void Postfix(AffectCorruption __instance, Character _affectedCharacter, object[] _infos)
        {
            if(_affectedCharacter != null && _affectedCharacter.Alive && _affectedCharacter.PlayerStats)
            {
                if (_affectedCharacter.Inventory.SkillKnowledge.IsItemLearned(BloodMage.CorruptedDevotionID))
                {
                    if(_affectedCharacter.PlayerStats.Corruption < 26f)
                    {
                        _affectedCharacter.PlayerStats.AffectCorruptionLevel(26f, !__instance.IsRaw);
                    }
                    if(_affectedCharacter.PlayerStats.Corruption >= 99f)
                    {
                        _affectedCharacter.PlayerStats.AffectCorruptionLevel(99f, !__instance.IsRaw);
                    }
                }
            }
        }
    }


    //True Power
    [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.GetAmplifiedDamage))]
    public class TruePowerDamageAdjustment
    {
        static void Postfix(CharacterStats __instance, IList<Tag> _tags, ref DamageList _damages)
        {
            if(__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.TruePowerID))
            {
                for(int i = 0; i < _damages.Count; i++)
                {
                    _damages[i].Damage += (_damages[i].Damage * ((__instance.m_character.PlayerStats.Corruption / 100f) / 2f));
                }
            }
        }
    }




    /*[HarmonyPatch(typeof(CharacterKnowledge), nameof(CharacterKnowledge.AddItem))]
    public class LeylinePassivesLearnedPatch
    {
    }*/
}
