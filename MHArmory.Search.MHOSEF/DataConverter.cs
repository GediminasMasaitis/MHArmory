using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHArmory.Core.DataStructures;
using MHArmory.Search.Contracts;
using MoreLinq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MHArmory.Search.MHOSEF
{
    class ConvertedArmorBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public IList<ConvertedSlot> Slots { get; set; }
        public IList<ConvertedSkill> Skills { get; set; }

        [JsonIgnore]
        public IArmorPiece ArmorPiece { get; set; }
    }

    class ConvertedCharm
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IList<ConvertedCharmRank> Ranks { get; set; }
    }

    class ConvertedCharmRank
    {
        public int Level { get; set; }
        public string Name { get; set; }
        public IList<ConvertedSkill> Skills { get; set; }

        [JsonIgnore]
        public ICharmLevel Charm { get; set; }
    }

    class ConvertedArmor : ConvertedArmorBase
    {
        public ConvertedSetEntry ArmorSet { get; set; }
    }

    class ConvertedArmorEntry : ConvertedArmorBase
    {
        public int ArmorSet { get; set; }
    }

    class ConvertedJewel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Slot { get; set; }
        public IList<ConvertedSkill> Skills { get; set; }
    }

    class ConvertedSlot
    {
        public int Rank { get; set; }
    }

    class ConvertedSkill
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int Skill { get; set; }
        public string SkillName { get; set; }
    }

    class ConvertedData
    {
        public IList<ConvertedArmor> Armors { get; set; }
        public IList<ConvertedCharm> Charms { get; set; }
        public IList<ConvertedJewel> Jewels { get; set; }
        public IList<ConvertedSet> Sets { get; set; }
        public IList<ConvertedSkillGroup> Skills { get; set; }
    }

    class ConvertedSetEntry
    {
        public int Id { get; set; }
        public IList<int> Pieces { get; set; }
    }

    class ConvertedSet
    {
        public int Id { get; set; }
        public IList<ConvertedArmorEntry> Pieces { get; set; }
        public ConvertedSetBonus Bonus { get; set; }
    }

    class ConvertedSetBonus
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IList<ConvertedSetBonusRank> Ranks { get; set; }
    }

    class ConvertedSetBonusRank
    {
        public int Pieces { get; set;}
        public ConvertedSkill Skill { get; set; }
    }

    class ConvertedSkillGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IList<ConvertedSkill> Ranks { get; set; }
    }

    internal class DataConverter
    {
        public ConvertedData ConvertData(ISolverData solverData)
        {
            var convertedData = new ConvertedData();
            convertedData.Armors = ConvertArmors(solverData.AllHeads, solverData.AllChests, solverData.AllGloves, solverData.AllWaists, solverData.AllLegs);
            convertedData.Charms = ConvertCharm(solverData.AllCharms);
            convertedData.Jewels = ConvertJewels(solverData.AllJewels);
            convertedData.Sets = ConvertSets(convertedData.Armors);
            convertedData.Skills = ConvertAllSkills(convertedData.Sets, convertedData.Charms);
            return convertedData;
        }

        private IList<ConvertedSkillGroup> ConvertAllSkills(IList<ConvertedSet> convertedSets, IList<ConvertedCharm> convertedCharms)
        {
            var allAbilities = new List<IAbility>();
            foreach (ConvertedSet set in convertedSets)
            {
                foreach (ConvertedArmorEntry piece in set.Pieces)
                {
                    allAbilities.AddRange(piece.ArmorPiece.Abilities);
                    if (piece.ArmorPiece.ArmorSetSkills != null)
                    {
                        allAbilities.AddRange(piece.ArmorPiece.ArmorSetSkills.SelectMany(x => x.Parts).SelectMany(x => x.GrantedSkills));
                    }
                }
            }

            allAbilities.AddRange(convertedCharms.SelectMany(x => x.Ranks).SelectMany(x => x.Charm.Abilities));
            var distinctSkills = allAbilities.Select(x => x.Skill).Distinct().ToList();

            var convertedGroups = new List<ConvertedSkillGroup>();
            foreach (ISkill skill in distinctSkills)
            {
                var group = new ConvertedSkillGroup();
                group.Id = skill.Id;
                group.Name = skill.Name["EN"];
                group.Ranks = ConvertSkills(skill.Abilities);
                convertedGroups.Add(group);
            }
            return convertedGroups;
        }

        public IList<ConvertedCharm> ConvertCharm(IEnumerable<ISolverDataEquipmentModel> equipments)
        {
            var convertedCharms = new List<ConvertedCharm>();

            IEnumerable<ICharmLevel> charms = equipments.Select(x =>x.Equipment).Cast<ICharmLevel>();
            ILookup<int, ICharmLevel> groups = charms.ToLookup(x => x.Charm.Id);

            foreach (IGrouping<int, ICharmLevel> group in groups)
            {
                var convertedCharm = new ConvertedCharm();
                convertedCharm.Id = group.Key;
                convertedCharm.Ranks = new List<ConvertedCharmRank>();
                foreach (ICharmLevel level in group)
                {
                    var convertedRank = new ConvertedCharmRank();
                    convertedRank.Level = level.Level;
                    convertedRank.Skills = ConvertSkills(level.Abilities);
                    convertedRank.Charm = level;
                    convertedRank.Name = level.Name["EN"];
                    convertedCharm.Name = level.Charm.Name["EN"];
                    convertedCharm.Ranks.Add(convertedRank);
                }
                convertedCharms.Add(convertedCharm);
            }
            return convertedCharms;
        }

        private IList<ConvertedSet> ConvertSets(IList<ConvertedArmor> convertedDataArmors)
        {
            var sets = new List<ConvertedSet>();
            ILookup<int, ConvertedArmor> setLookup = convertedDataArmors.ToLookup(x => x.ArmorSet.Id);
            foreach (IGrouping<int, ConvertedArmor> setGrouping in setLookup)
            {
                var set = new ConvertedSet();
                set.Id = setGrouping.Key;
                set.Pieces = new List<ConvertedArmorEntry>();
                var setEntry = new ConvertedSetEntry();
                setEntry.Id = set.Id;
                setEntry.Pieces = new List<int>();
                ConvertedArmor firstArmor = setGrouping.First();
                foreach (ConvertedArmor armor in setGrouping)
                {
                    var entry = new ConvertedArmorEntry();
                    armor.ArmorSet = setEntry;
                    setEntry.Pieces.Add(armor.Id);
                    entry.ArmorSet = set.Id;
                    entry.Id = armor.Id;
                    entry.Name = armor.Name;
                    entry.Skills = armor.Skills;
                    entry.Slots = armor.Slots;
                    entry.Type = armor.Type;
                    entry.ArmorPiece = armor.ArmorPiece;
                    set.Pieces.Add(entry);
                }

                IArmorSetSkill setSkills = firstArmor.ArmorPiece.ArmorSetSkills?.FirstOrDefault();
                if (setSkills != null)
                {
                    var bonus = new ConvertedSetBonus();
                    bonus.Id = setSkills.Id;
                    bonus.Ranks = new List<ConvertedSetBonusRank>();
                    foreach (IArmorSetSkillPart part in setSkills.Parts)
                    {
                        var bonusRank = new ConvertedSetBonusRank();
                        bonusRank.Skill = ConvertSkills(part.GrantedSkills).Single();
                        bonusRank.Pieces = part.RequiredArmorPieces;
                        bonus.Ranks.Add(bonusRank);
                    }
                    set.Bonus = bonus;
                }
                sets.Add(set);
            }
            return sets;
        }

        private IList<ConvertedJewel> ConvertJewels(IEnumerable<SolverDataJewelModel> jewels)
        {
            return jewels.Select(x => x.Jewel).Select(ConvertJewel).ToList();
        }



        public IList<ConvertedArmor> ConvertArmors(params IEnumerable<ISolverDataEquipmentModel>[] models)
        {
            var selectedPieces = models
                .SelectMany(x => x)
                .Where(x => x.IsSelected)
                .Select(x => x.Equipment)
                .ToList();
            var convertedArmorPieces = selectedPieces
                .Select(ConvertArmor)
                .ToList();
            return convertedArmorPieces;
        }

        private ConvertedJewel ConvertJewel(IJewel jewel)
        {
            var convertedArmor = new ConvertedJewel();
            convertedArmor.Id = jewel.Id;
            convertedArmor.Name = jewel.Name["EN"];
            convertedArmor.Skills = ConvertSkills(jewel.Abilities);
            convertedArmor.Slot = jewel.SlotSize;
            return convertedArmor;
        }

        private ConvertedArmor ConvertArmor(IEquipment armorPiece)
        {
            var convertedArmor = new ConvertedArmor();
            convertedArmor.Id = armorPiece.GetHashedId();
            convertedArmor.Name = armorPiece.Name["EN"];
            convertedArmor.Skills = ConvertSkills(armorPiece.Abilities);
            convertedArmor.Slots = ConvertSlots(armorPiece.Slots);
            convertedArmor.Type = armorPiece.Type.ToString().ToLowerInvariant();
            convertedArmor.ArmorSet = new ConvertedSetEntry();
            convertedArmor.ArmorSet.Id = armorPiece.Id;
            convertedArmor.ArmorPiece = (IArmorPiece)armorPiece;
            //var sameSet = selectedPieces.Where(x => x.Id == armorPiece.Id).ToList();
            return convertedArmor;
        }

        public IList<ConvertedSlot> ConvertSlots(int[] slots)
        {
            var convertedSlots = new List<ConvertedSlot>();
            foreach (int slot in slots)
            {
                var convertedSlot = new ConvertedSlot();
                convertedSlot.Rank = slot;
                convertedSlots.Add(convertedSlot);
            }
            return convertedSlots;
        }

        public IList<ConvertedSkill> ConvertSkills(IEnumerable<IAbility> abilities)
        {
            var convertedSkills = new List<ConvertedSkill>();
            foreach (IAbility ability in abilities)
            {
                var convertedSkill = new ConvertedSkill();
                convertedSkill.Id = ability.Id;
                convertedSkill.Level = ability.Level;
                convertedSkill.Skill = ability.Skill.Id;
                convertedSkill.SkillName = ability.Skill.Name["EN"];
                convertedSkills.Add(convertedSkill);
            }
            return convertedSkills;
        }
    }
}
