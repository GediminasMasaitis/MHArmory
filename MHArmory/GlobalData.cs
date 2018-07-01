﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHArmory.Core.DataStructures;
using MHArmory.ViewModels;

namespace MHArmory
{
    public class GlobalData
    {
        public static readonly GlobalData Instance = new GlobalData();

        // ================================================================================================================

        #region Abilities

        private readonly TaskCompletionSource<AbilityViewModel[]> abilitiesTaskCompletionSource = new TaskCompletionSource<AbilityViewModel[]>();

        public void SetAbilities(AbilityViewModel[] abilities)
        {
            abilitiesTaskCompletionSource.TrySetResult(abilities);
        }

        public Task<AbilityViewModel[]> GetAbilities()
        {
            return abilitiesTaskCompletionSource.Task;
        }

        #endregion // Abilities

        // ================================================================================================================

        #region Skills

        private readonly TaskCompletionSource<SkillViewModel[]> skillsTaskCompletionSource = new TaskCompletionSource<SkillViewModel[]>();

        public void SetSkills(SkillViewModel[] skills)
        {
            skillsTaskCompletionSource.TrySetResult(skills);
        }

        public Task<SkillViewModel[]> GetSkills()
        {
            return skillsTaskCompletionSource.Task;
        }

        #endregion // Skills

        // ================================================================================================================

        #region Armors

        private readonly TaskCompletionSource<SkillViewModel[]> armorsTaskCompletionSource = new TaskCompletionSource<SkillViewModel[]>();

        public void SetArmors(SkillViewModel[] skills)
        {
            skillsTaskCompletionSource.TrySetResult(skills);
        }

        public Task<SkillViewModel[]> GetArmors()
        {
            return skillsTaskCompletionSource.Task;
        }

        #endregion // Armors

        // ================================================================================================================

        #region SkillsToArmorsMap

        private readonly TaskCompletionSource<IDictionary<int, IArmorPiece[]>> skillsToArmorsMapTaskCompletionSource = new TaskCompletionSource<IDictionary<int, IArmorPiece[]>>();

        public void SetSkillsToArmorsMap(IDictionary<int, IArmorPiece[]> skillsToArmorsMap)
        {
            skillsToArmorsMapTaskCompletionSource.TrySetResult(skillsToArmorsMap);
        }

        public Task<IDictionary<int, IArmorPiece[]>> GetSkillsToArmorsMap()
        {
            return skillsToArmorsMapTaskCompletionSource.Task;
        }

        #endregion // SkillsToArmorsMap

        // ================================================================================================================

        #region SkillsToCharmsMap

        private readonly TaskCompletionSource<IDictionary<int, ICharm[]>> skillsToCharmsMapTaskCompletionSource = new TaskCompletionSource<IDictionary<int, ICharm[]>>();

        public void SetSkillsToCharmsMap(IDictionary<int, ICharm[]> skillsToCharmsMap)
        {
            skillsToCharmsMapTaskCompletionSource.TrySetResult(skillsToCharmsMap);
        }

        public Task<IDictionary<int, ICharm[]>> GetSkillsToCharmsMap()
        {
            return skillsToCharmsMapTaskCompletionSource.Task;
        }

        #endregion // SkillsToCharmsMap

        // ================================================================================================================

        #region SkillsToJewelsMap

        private readonly TaskCompletionSource<IDictionary<int, IJewel[]>> skillsToJewelsMapTaskCompletionSource = new TaskCompletionSource<IDictionary<int, IJewel[]>>();

        public void SetSkillsToJewelsMap(IDictionary<int, IJewel[]> skillsToJewelsMap)
        {
            skillsToJewelsMapTaskCompletionSource.TrySetResult(skillsToJewelsMap);
        }

        public Task<IDictionary<int, IJewel[]>> GetSkillsToJewelsMap()
        {
            return skillsToJewelsMapTaskCompletionSource.Task;
        }

        #endregion // SkillsToJewelsMap
    }
}
