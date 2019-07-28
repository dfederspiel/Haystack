using System;
using System.Collections.Generic;
using System.Linq;

namespace HaystackReContinued
{
    public class VesselListController
    {
        private HaystackContinued haystack;

        // number of vessles per type
        private Dictionary<string, int> vesselTypeCounts = new Dictionary<string, int>();

        private List<Vessel> vesselList = new List<Vessel>();
        private List<Vessel> filteredVesselList = new List<Vessel>();
        private List<CelestialBody> filteredBodyList = new List<CelestialBody>();

        private Comparers.CombinedComparer<Vessel> vesselComparer;

        // Search text
        private string searchTerm = "";

        private readonly Dictionary<CelestialBody, List<Vessel>> groupedBodyVessel =
            new Dictionary<CelestialBody, List<Vessel>>();

        private bool listIsAscending;
        private Vessel activeVessel;

        internal VesselListController(HaystackContinued haystack, BottomButtons bottomButtons)
        {
            this.haystack = haystack;
            bottomButtons.OnNearbyChanged += onNearbyChanged;
            bottomButtons.OnHiddenVesselsChanged += onHiddenVesselChanged;
            bottomButtons.OnSortOrderChanged += onSortOrderChanged;

            this.listIsAscending = bottomButtons.IsAscendingSortOrder;

            this.vesselComparer = Comparers.CombinedComparer<Vessel>.FromOne(new Comparers.VesselNameComparer());
        }

        private void onSortOrderChanged(BottomButtons view)
        {
            this.listIsAscending = view.IsAscendingSortOrder;

            this.RefreshFilteredList();
        }

        private void onHiddenVesselChanged(BottomButtons view)
        {
            this.RefreshFilteredList();
        }

        private void onNearbyChanged(BottomButtons view)
        {
            if (!HSUtils.IsInFlight)
            {
                return;
            }

            this.updateActiveVessel();

            if (view.IsNearbyOnly)
            {
                this.vesselComparer = this.vesselComparer.Add(new Comparers.VesselNearbyComparer(this.activeVessel));
            }
            else
            {
                this.vesselComparer = this.vesselComparer.Remove<Comparers.VesselNearbyComparer>();
            }

            this.RefreshFilteredList();
        }

        public Dictionary<string, int> VesselTypeCounts
        {
            get { return this.vesselTypeCounts; }
        }

        public List<Vessel> DisplayVessels
        {
            get { return this.filteredVesselList; }
        }

        public Dictionary<CelestialBody, List<Vessel>> GroupedByBodyVessels
        {
            get { return this.groupedBodyVessel; }
        }

        public List<CelestialBody> DisplayBodyies
        {
            get { return this.filteredBodyList; }
        }

        public string SearchTerm
        {
            get { return this.searchTerm; }
            set
            {
                var previous = this.searchTerm;
                this.searchTerm = value;

                if (string.IsNullOrEmpty(this.searchTerm))
                {
                    this.vesselComparer = this.vesselComparer.Remove<Comparers.FilteredVesselComparer>();
                }
                else
                {
                    this.vesselComparer =
                        this.vesselComparer.Add(new Comparers.FilteredVesselComparer(this.searchTerm));
                }

                if (previous != this.searchTerm)
                {
                    this.RefreshFilteredList();
                }
            }
        }

        /// <summary>
        /// Refresh list of vessels
        /// </summary>
        public void FetchVesselList()
        {
            this.vesselList = FlightGlobals.Vessels;

            if (this.vesselList == null)
            {
                HSUtils.DebugLog("vessel list is null");
                this.vesselList = new List<Vessel>();
            }

            this.updateActiveVessel();

            // count vessel types
            this.VesselTypeCounts.Clear();
            foreach (var vessel in vesselList)
            {
                var typeString = vessel.vesselType.ToString();

                if (this.VesselTypeCounts.ContainsKey(typeString))
                    this.VesselTypeCounts[typeString]++;
                else
                    this.VesselTypeCounts.Add(typeString, 1);
            }

            this.performFilters();
        }

        private void updateActiveVessel()
        {
            if (!HSUtils.IsInFlight)
            {
                this.activeVessel = null;
                return;
            }

            var current = FlightGlobals.ActiveVessel;

            if (current != this.activeVessel)
            {
                this.activeVessel = current;
                this.updateNearbyComparer();
            }
        }

        private void updateNearbyComparer()
        {
            var comparer = this.vesselComparer.Comparers.FirstOrDefault(c => c is Comparers.VesselNearbyComparer);

            if (comparer == null)
            {
                return;
            }

            this.vesselComparer = this.vesselComparer.Remove<Comparers.VesselNearbyComparer>();
            this.vesselComparer.Add(new Comparers.VesselNearbyComparer(this.activeVessel));
        }

        public void RefreshFilteredList()
        {
            this.updateActiveVessel();
            this.performFilters();
            this.performSort(this.filteredVesselList);
        }

        private void performFilters()
        {
            this.filteredVesselList = new List<Vessel>(this.vesselList);
            this.filteredBodyList = new List<CelestialBody>(Resources.CelestialBodies);

            if (this.vesselList != null)
            {
                this.removeFilteredVesslesFromList(this.filteredVesselList);

                //now hidden vessels
                if (!this.haystack.bottomButtons.IsHiddenVesselsToggled)
                {
                    this.removeHiddenVesselsFromList(this.filteredVesselList);
                }

                if (this.haystack.bottomButtons.IsNearbyOnly)
                {
                    this.filterForNearbyOnly(this.filteredVesselList);
                }

                // And then filter by the search string
                this.performSearchOnVesselList(this.filteredVesselList);

                if (!string.IsNullOrEmpty(this.SearchTerm))
                {
                    this.filteredBodyList.RemoveAll(
                        cb => -1 == cb.bodyName.IndexOf(this.SearchTerm, StringComparison.OrdinalIgnoreCase)
                        );
                }
            }

            if (this.haystack.bottomButtons.GroupByOrbitingBody)
            {
                this.GroupedByBodyVessels.Clear();

                foreach (var vessel in this.filteredVesselList)
                {
                    var body = vessel.orbit.referenceBody;

                    List<Vessel> list;
                    if (!this.GroupedByBodyVessels.TryGetValue(body, out list))
                    {
                        list = new List<Vessel>();
                        this.GroupedByBodyVessels.Add(body, list);
                    }

                    list.Add(vessel);
                }

                // sort groups
                foreach (var kv in this.groupedBodyVessel)
                {
                    this.performSort(kv.Value);
                }
            }
        }

        private void removeFilteredVesslesFromList(List<Vessel> list)
        {
            if (Resources.vesselTypesList != null)
            {
                var invisibleTypes = Resources.vesselTypesList.FindAll(type => type.visible == false).Select(type => type.name);

                list.RemoveAll(vessel => invisibleTypes.Contains(vessel.vesselType.ToString()));
            }
        }

        private void removeHiddenVesselsFromList(List<Vessel> list)
        {
            list.RemoveAll(v => this.haystack.HiddenVessels.Contains(v.id));
        }

        private void filterForNearbyOnly(List<Vessel> list)
        {
            if (!HSUtils.IsInFlight)
            {
                return;
            }
            var localBody = FlightGlobals.ActiveVessel.orbit.referenceBody;

            list.RemoveAll(v => v.orbit.referenceBody != localBody);
        }

        private void performSearchOnVesselList(List<Vessel> list)
        {
            if (string.IsNullOrEmpty(this.SearchTerm))
            {
                return;
            }

            list.RemoveAll(
                    v => v == null || v.vesselName == null || -1 == v.vesselName.IndexOf(this.SearchTerm, StringComparison.OrdinalIgnoreCase)
                    );
        }

        private void performSort(List<Vessel> list)
        {
            list.Sort(this.vesselComparer);
            if (!this.listIsAscending)
            {
                list.Reverse();
            }
        }
    }
}