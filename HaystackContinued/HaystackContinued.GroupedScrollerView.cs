using UnityEngine;

namespace HaystackReContinued
{
    public class GroupedScrollerView
    {
        private Vector2 scrollPos = Vector2.zero;
        private Vessel selectedVessel;
        private CelestialBody selectedBody;
        private readonly VesselInfoView vesselInfoView;
        private VesselListController vesselListController;


        internal GroupedScrollerView(HaystackContinued haystackContinued, VesselListController vesselListController)
        {
            this.vesselInfoView = new VesselInfoView(haystackContinued);
            this.vesselListController = vesselListController;
        }

        public Vessel SelectedVessel
        {
            get { return this.selectedVessel; }
            set { this.selectedVessel = value; }
        }

        internal void Draw()
        {
            var displayVessels = this.vesselListController.DisplayVessels;
            if (displayVessels == null || displayVessels.IsEmpty())
            {
                GUILayout.Label("No matched vessels found");

                GUILayout.FlexibleSpace();
                return;
            }

            this.scrollPos = GUILayout.BeginScrollView(scrollPos);

            Vessel preSelectedVessel = null;

            var clicked = false;

            GUILayout.BeginVertical();

            foreach (var kv in this.vesselListController.GroupedByBodyVessels)
            {
                var body = kv.Key;
                var vessels = kv.Value;

                var selected = body == selectedBody;

                selected = GUILayout.Toggle(selected, new GUIContent(body.name), Resources.buttonTextOnly);

                if (selected)
                {
                    this.selectedBody = body;
                }
                else
                {
                    if (this.selectedBody == body)
                    {
                        this.selectedBody = null;
                    }
                    continue;
                }

                var activeVessel = HSUtils.IsInFlight ? FlightGlobals.ActiveVessel : null;
                foreach (var vessel in vessels)
                {
                    //this typically happens when debris is going out of physics range and is deleted by the game
                    if (vessel == null)
                    {
                        continue;
                    }

                    this.vesselInfoView.Draw(vessel, vessel == this.selectedVessel, activeVessel);

                    if (!this.vesselInfoView.Clicked)
                    {
                        continue;
                    }

                    preSelectedVessel = vessel;
                    clicked = true;
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            var checkInScroll = GUILayoutUtility.GetLastRect();
            if (!clicked || !checkInScroll.Contains(Event.current.mousePosition))
            {
                return;
            }

            if (preSelectedVessel != null && preSelectedVessel == this.selectedVessel)
            {
                this.fireOnSelectedItemClicked(this);
                return;
            }

            if (preSelectedVessel != null && preSelectedVessel != this.selectedVessel)
            {
                this.selectedVessel = preSelectedVessel;
                this.fireOnSelectionChanged(this);
            }

            this.vesselInfoView.Reset();
            this.changeCameraTarget();
        }

        private void changeCameraTarget()
        {
            if (this.selectedVessel == null)
            {
                return;
            }

            if (HSUtils.IsTrackingCenterActive)
            {
                HSUtils.RequestCameraFocus(this.selectedVessel);
            }
            else
            {
                HSUtils.FocusMapObject(this.selectedVessel);
            }
        }

        internal void GUISetup(BottomButtons bottomButtons)
        {
            bottomButtons.OnGroupByChanged += view => this.reset();
        }

        private void reset()
        {
            this.scrollPos = Vector2.zero;
            this.selectedVessel = null;
            this.selectedBody = null;
            this.vesselInfoView.Reset();
        }

        internal delegate void OnSelectionChangedHandler(GroupedScrollerView view);
        internal event OnSelectionChangedHandler OnSelectionChanged;
        protected virtual void fireOnSelectionChanged(GroupedScrollerView view)
        {
            OnSelectionChangedHandler handler = this.OnSelectionChanged;
            if (handler != null) handler(view);
        }

        internal delegate void OnSelectedItemClickedHandler(GroupedScrollerView view);

        internal event OnSelectedItemClickedHandler OnSelectedItemClicked;

        protected virtual void fireOnSelectedItemClicked(GroupedScrollerView view)
        {
            var handler = this.OnSelectedItemClicked;
            if (handler != null) handler(view);
        }
    }
}