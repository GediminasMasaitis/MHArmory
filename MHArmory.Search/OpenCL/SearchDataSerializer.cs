using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MHArmory.Core.DataStructures;

namespace MHArmory.Search.OpenCL
{
    public class SearchDataSerializer
    {
        public SerializedSearchParameters Serialize(ISolverData data)
        {
            var parameters = new SerializedSearchParameters();
            
            var heads = data.AllHeads.Where(x => x.IsSelected).Select(x => x.Equipment).ToList();
            var chests = data.AllChests.Where(x => x.IsSelected).Select(x => x.Equipment).ToList();
            var gloves = data.AllGloves.Where(x => x.IsSelected).Select(x => x.Equipment).ToList();
            var waists = data.AllWaists.Where(x => x.IsSelected).Select(x => x.Equipment).ToList();
            var legs = data.AllLegs.Where(x => x.IsSelected).Select(x => x.Equipment).ToList();
            var charms = data.AllCharms.Where(x => x.IsSelected).Select(x => x.Equipment).ToList();

            var allArmorPieces = heads.Concat(chests).Concat(gloves).Concat(waists).Concat(legs).Cast<IArmorPiece>().ToList();
            var allEquipment = allArmorPieces.Concat(charms).ToList();

            if (allEquipment.Count > 255)
            {
                throw new Exception();
            }

            var armorSetParts = allArmorPieces.Where(x => x.ArmorSetSkills != null).SelectMany(x => x.ArmorSetSkills.SelectMany(y => y.Parts)).ToList();
            var desiredAbilitiesSkillSet = new HashSet<int>(data.DesiredAbilities.Select(x => x.Skill.Id));
            IEnumerable<IArmorSetSkillPart> desiredArmorSetParts = armorSetParts.Where(x => x.GrantedSkills.Any(y => desiredAbilitiesSkillSet.Contains(y.Skill.Id)));
            IEnumerable<int> desiredSkillIDs = data.DesiredAbilities.Select(x => x.Skill.Id);

            var maps = new SearchIDMaps();
            maps.EquipmentIdMap = CreateIDMap(allEquipment.Select(x => new Tuple<int, EquipmentType>(x.Id, x.Type)));
            maps.JewelIdMap = CreateIDMap(data.AllJewels.Select(x => x.Jewel.Id));
            maps.SetIdMap = CreateIDMap(desiredArmorSetParts.Select(x => x.Id));
            maps.SkillIdMap = CreateIDMap(desiredSkillIDs);

            byte[] header = new byte[13];
            SerializeWeaponSlots(data.WeaponSlots, header);
            header[4] = (byte)heads.Count;
            header[5] = (byte)chests.Count;
            header[6] = (byte)gloves.Count;
            header[7] = (byte)waists.Count;
            header[8] = (byte)legs.Count;
            header[9] = (byte)charms.Count;
            header[10] = (byte)data.AllJewels.Length;
            header[11] = (byte)data.DesiredAbilities.Length;
            header[12] = (byte)maps.SetIdMap.Count;
            parameters.Header = header;

            parameters.Equipment = SerializeEquipment(maps, allEquipment);
            parameters.DesiredSkills = SerializeDesiredSkills(maps, data.DesiredAbilities);
            parameters.Decorations = SerializeDecorations(maps, data.AllJewels);
            parameters.Combinations = (uint) (heads.Count * chests.Count * gloves.Count * waists.Count * legs.Count * charms.Count);
            parameters.SearchIDMaps = maps;
            return parameters;
        }

        private byte[] SerializeDecorations(SearchIDMaps maps, SolverDataJewelModel[] jewels)
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            IOrderedEnumerable<SolverDataJewelModel> orderedJewels = jewels.OrderByDescending(x => x.Jewel.SlotSize);
            foreach (SolverDataJewelModel jewel in orderedJewels)
            {
                sbyte available = (sbyte)Math.Min(jewel.Available, sbyte.MaxValue);
                byte mappedId = maps.JewelIdMap[jewel.Jewel.Id];
                IAbility ability = jewel.Jewel.Abilities[0]; // Fuck it lol
                writer.Write((byte)mappedId);
                writer.Write((ushort)jewel.Jewel.Id);
                writer.Write((byte)jewel.Jewel.SlotSize);
                writer.Write(available);
                SerializeAbility(maps, ability, writer);
            }
            byte[] result = ms.ToArray();
            return result;
        }

        private byte[] SerializeDesiredSkills(SearchIDMaps maps, IAbility[] desiredAbilities)
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            foreach (IAbility desiredAbility in desiredAbilities)
            {
                SerializeAbility(maps, desiredAbility, writer);
            }
            byte[] result = ms.ToArray();
            return result;
        }

        private void SerializeWeaponSlots(int[] weaponSlots, byte[] header)
        {
            foreach (int weaponSlot in weaponSlots)
            {
                header[weaponSlot]++;
            }
        }

        private IDictionary<T, byte> CreateIDMap<T>(IEnumerable<T> keys)
        {
            byte currentIndex = 0;
            var map = new Dictionary<T, byte>();
            foreach (T key in keys)
            {
                if (!map.ContainsKey(key))
                {
                    map.Add(key, currentIndex);
                    currentIndex++;
                }
            }
            return map;
        }

        private byte[] SerializeEquipment(SearchIDMaps maps, IEnumerable<IEquipment> equipments)
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            foreach (IEquipment equipment in equipments)
            {
                var tuple = new Tuple<int, EquipmentType>(equipment.Id, equipment.Type);
                byte mappedID = maps.EquipmentIdMap[tuple];
                writer.Write((ushort)mappedID);
                writer.Write((ushort)equipment.Id);

                SerializeEquipmentAbilities(maps, equipment, writer);
                if (equipment is IArmorPiece armorPiece)
                {
                    SerializeArmorSetSkills(maps, armorPiece, writer);
                }
                else
                {
                    for (int i = 0; i < SearchLimits.SetSkillCount; i++)
                    {
                        SerializeNullSetSkill(writer);
                    }
                }
                byte[] slots = new byte[4];
                foreach (int slot in equipment.Slots)
                {
                    ++slots[slot];
                }
                writer.Write(slots);
            }
            return ms.ToArray();
        }

        private void SerializeArmorSetSkills(SearchIDMaps maps, IArmorPiece armorPiece, BinaryWriter writer)
        {
            IArmorSetSkillPart[] parts = armorPiece.ArmorSetSkills?.SelectMany(x => x.Parts).ToArray() ?? new IArmorSetSkillPart[0];
            int skillCount = parts.SelectMany(x => x.GrantedSkills).Count(x => maps.SkillIdMap.ContainsKey(x.Skill.Id));
            if (skillCount > SearchLimits.SetSkillCount)
            {
                throw new Exception($"Armor {armorPiece.Name} has {skillCount} set skills");
            }
            foreach (IArmorSetSkillPart part in parts)
            {
                SerializeSetSkill(maps, part, writer);
            }
            for (int i = skillCount; i < SearchLimits.SetSkillCount; ++i)
            {
                SerializeNullSetSkill(writer);
            }
        }

        private void SerializeNullSetSkill(BinaryWriter writer)
        {
            writer.Write((byte)0xFF);
            writer.Write((byte)0);
            SerializeNullAbility(writer);
        }

        private void SerializeSetSkill(SearchIDMaps maps, IArmorSetSkillPart part, BinaryWriter writer)
        {
            bool mapped = maps.SetIdMap.TryGetValue(part.Id, out byte mappedId);
            if (!mapped)
            {
                return;
            }
            IEnumerable<IAbility> desiredSkills = part.GrantedSkills.Where(x => maps.SkillIdMap.ContainsKey(x.Skill.Id));
            foreach (IAbility ability in desiredSkills)
            {
                writer.Write((byte) mappedId);
                writer.Write((byte) part.RequiredArmorPieces);
                SerializeAbility(maps, ability, writer);
            }
        }

        private void SerializeEquipmentAbilities(SearchIDMaps maps, IEquipment equipment, BinaryWriter writer)
        {
            IAbility[] abilities = equipment.Abilities ?? new IAbility[0];
            if (abilities.Length > SearchLimits.ArmorSkillCount)
            {
                throw new Exception($"Armor {equipment.Name} has {abilities.Length} abilities");
            }
            foreach (IAbility ability in abilities)
            {
                SerializeAbility(maps, ability, writer);
            }
            for (int i = abilities.Length; i < SearchLimits.ArmorSkillCount; ++i)
            {
                SerializeNullAbility(writer);
            }
        }

        private void SerializeNullAbility(BinaryWriter writer)
        {
            writer.Write((byte)0xFF);
            writer.Write((byte)0);
        }

        private void SerializeAbility(SearchIDMaps maps, IAbility ability, BinaryWriter writer)
        {
            bool mapped = maps.SkillIdMap.TryGetValue(ability.Skill.Id, out byte mappedId);
            if (mapped)
            {
                writer.Write((byte) mappedId);
                writer.Write((byte) ability.Level);
            }
            else
            {
                SerializeNullAbility(writer);
            }
        }
    }
}