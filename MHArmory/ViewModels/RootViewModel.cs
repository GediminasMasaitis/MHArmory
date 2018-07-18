﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MHArmory.Core;
using MHArmory.Core.DataStructures;
using MHArmory.Search;
using MHArmory.Searching;

namespace MHArmory.ViewModels
{
    public class RootViewModel : ViewModelBase
    {
        public ICommand OpenSkillSelectorCommand { get; }
        public ICommand SearchArmorSetsCommand { get; }

        private SolverData solverData;
        private Solver solver;

        private bool isDataLoading = true;
        public bool IsDataLoading
        {
            get { return isDataLoading; }
            set { SetValue(ref isDataLoading, value); }
        }

        private bool isDataLoaded;
        public bool IsDataLoaded
        {
            get { return isDataLoaded; }
            set { SetValue(ref isDataLoaded, value); }
        }

        private IEnumerable<AbilityViewModel> selectedAbilities;
        public IEnumerable<AbilityViewModel> SelectedAbilities
        {
            get { return selectedAbilities; }
            set { SetValue(ref selectedAbilities, value); }
        }

        private IEnumerable<ArmorSetViewModel> foundArmorSets;
        public IEnumerable<ArmorSetViewModel> FoundArmorSets
        {
            get { return foundArmorSets; }
            set { SetValue(ref foundArmorSets, value); }
        }

        private bool isSearching;
        public bool IsSearching
        {
            get { return isSearching; }
            private set { SetValue(ref isSearching, value); }
        }

        private bool isAutoSearch;
        public bool IsAutoSearch
        {
            get { return isAutoSearch; }
            set { SetValue(ref isAutoSearch, value); }
        }

        public RootViewModel()
        {
            OpenSkillSelectorCommand = new AnonymousCommand(OpenSkillSelector);
            SearchArmorSetsCommand = new AnonymousCommand(SearchArmorSets);
        }

        public void Initialize()
        {
            //IDictionary<int, IList<ICharm>> skillsToCharmsMap = await GlobalData.Instance.GetSkillsToCharmsMap();
            //IDictionary<int, IList<IJewel>> skillsToJewelsMap = await GlobalData.Instance.GetSkillsToJewelsMap();



            IList <IArmorPiece> allHeads = GlobalData.Instance.Heads;
        }

        private void OpenSkillSelector(object parameter)
        {
            RoutedCommands.OpenSkillsSelector.Execute(null);
        }

        public async void SearchArmorSets()
        {
            var desiredAbilities = SelectedAbilities
                .Where(x => x.IsChecked)
                .Select(x => x.Ability)
                .ToList();

            solverData = new SolverData(
                null,
                GlobalData.Instance.Heads,
                GlobalData.Instance.Chests,
                GlobalData.Instance.Gloves,
                GlobalData.Instance.Waists,
                GlobalData.Instance.Legs,
                GlobalData.Instance.Charms,
                GlobalData.Instance.Jewels,
                desiredAbilities
            );

            solverData.Done();

            solver = new Solver(solverData);

            solver.DebugData += Solver_DebugData;
            solver.SearchingChanged += Solver_SearchingChanged;

            IList<ArmorSetSearchResult> result = await solver.SearchArmorSets();

            if (result == null)
                FoundArmorSets = null;
            else
            {
                FoundArmorSets = result.Where(x => x.IsMatch).Select(x => new ArmorSetViewModel
                {
                    ArmorPieces = x.ArmorPieces,
                    Charm = x.Charm,
                    Jewels = x.Jewels.Select(j => new ArmorSetJewelViewModel { Jewel = j.Jewel, Count = j.Count }).ToList()
                });
            }

            //if (IsSearching)
            //    return;

            //IsSearching = true;

            //try
            //{
            //await SearchArmorSetsInternal();
            //}
            //finally
            //{
            //    IsSearching = false;
            //}

            //solverData.

            solver.SearchingChanged -= Solver_SearchingChanged;
            solver.DebugData -= Solver_DebugData;
        }

        private void Solver_SearchingChanged(bool isSearching)
        {
            IsSearching = isSearching;
        }

        private void Solver_DebugData(string debugData)
        {
            SearchResult = debugData;
        }

        private MaximizedSearchCriteria[] sortCriterias = new MaximizedSearchCriteria[]
        {
            MaximizedSearchCriteria.BaseDefense,
            MaximizedSearchCriteria.DragonResistance,
            MaximizedSearchCriteria.SlotSizeCube
        };

        private string searchResult;
        public string SearchResult
        {
            get { return searchResult; }
            private set { SetValue(ref searchResult, value); }
        }
    }
}
