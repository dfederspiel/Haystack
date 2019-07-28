using UnityEngine;

namespace HaystackReContinued
{
        public class DefaultScrollerView
        {
            private Vector2 scrollPos = Vector2.zero;
            private Vessel selectedVessel;
            private CelestialBody selectedBody;

            private readonly VesselInfoView vesselInfoView;
            private readonly VesselListController vesselListController;

            public bool ShowCelestialBodies { get; set; }

            internal DefaultScrollerView(HaystackContinued haystackContinued, VesselListController vesselListController)
            {
                this.vesselInfoView = new VesselInfoView(haystackContinued);
                this.vesselListController = vesselListController;
            }

            internal Vessel SelectedVessel
            {
                get { return selectedVessel; }
                set
                {
                    this.selectedVessel = value;
                    this.selectedBody = null;
                }
            }

            internal CelestialBody SelectedBody
            {
                get { return this.selectedBody; }
                set
                {
                    this.selectedBody = value;
                    this.selectedVessel = null;
                }
            }

            private void reset()
            {
                this.scrollPos = Vector2.zero;
                this.selectedVessel = null;
                this.selectedBody = null;
                this.vesselInfoView.Reset();
            }

            internal void Draw()
            {
                var displayVessels = this.vesselListController.DisplayVessels;
                if ((displayVessels == null || displayVessels.IsEmpty()) && this.ShowCelestialBodies != true)
                {
                    GUILayout.Label("No match found");
                    GUILayout.FlexibleSpace();
                    return;
                }

                var clicked = false;
                Vessel preSelectedVessel = null;
                CelestialBody preSelecedBody = null;

                this.scrollPos = GUILayout.BeginScrollView(this.scrollPos);

                GUILayout.BeginVertical();

                var activeVessel = HSUtils.IsInFlight ? FlightGlobals.ActiveVessel : null;

                foreach (var vessel in displayVessels)
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

                // celestial bodies
                if (this.ShowCelestialBodies)
                {
                    var displayBodies = this.vesselListController.DisplayBodyies;
                    foreach (var body in displayBodies)
                    {
                        GUILayout.BeginVertical(body == this.SelectedBody
                            ? Resources.vesselInfoSelected
                            : Resources.vesselInfoDefault);

                        GUILayout.Label(body.name, Resources.textListHeaderStyle);
                        GUILayout.EndVertical();

                        Rect check = GUILayoutUtility.GetLastRect();

                        if (Event.current != null && Event.current.type == EventType.MouseDown &&
                            Input.GetMouseButtonDown(0) && check.Contains(Event.current.mousePosition))
                        {
                            if (this.SelectedBody == body)
                            {
                                this.fireOnSelectedItemClicked(this);
                                continue;
                            }

                            preSelecedBody = body;
                            clicked = true;
                        }
                    }
                }

                GUILayout.EndVertical();
                GUILayout.EndScrollView();

                var checkInScrollClick = GUILayoutUtility.GetLastRect();

                //clicks to items in scroll view can happen outside of the scroll view
                if (!clicked || Event.current == null || !checkInScrollClick.Contains(Event.current.mousePosition))
                {
                    return;
                }

                if (preSelectedVessel != null && preSelectedVessel == this.SelectedVessel)
                {
                    this.fireOnSelectedItemClicked(this);
                    return;
                }

                if (preSelecedBody != null && preSelecedBody != this.SelectedBody)
                {
                    this.SelectedBody = preSelecedBody;
                    this.fireOnSelectionChanged(this);
                }
                else if (preSelectedVessel != null && preSelectedVessel != this.SelectedVessel)
                {
                    this.SelectedVessel = preSelectedVessel;
                    this.fireOnSelectionChanged(this);
                }


                this.vesselInfoView.Reset();
                this.changeCameraTarget();
            }

            private void changeCameraTarget()
            {
                // don't do anything if we are in the space center since there is no map view to change.
                if (HSUtils.IsSpaceCenterActive)
                {
                    return;
                }

                if (this.SelectedVessel != null)
                {

                    if (HSUtils.IsTrackingCenterActive)
                    {
                        HSUtils.RequestCameraFocus(this.SelectedVessel);
                    }
                    else
                    {
                        HSUtils.FocusMapObject(this.selectedVessel);
                    }
                }
                if (this.SelectedBody != null)
                {
                    HSUtils.FocusMapObject(this.SelectedBody);
                }
            }

            internal delegate void OnSelectionChangedHandler(DefaultScrollerView scrollerView);
            internal event OnSelectionChangedHandler OnSelectionChanged;
            protected virtual void fireOnSelectionChanged(DefaultScrollerView scrollerview)
            {
                OnSelectionChangedHandler handler = this.OnSelectionChanged;
                if (handler != null) handler(scrollerview);
            }

            internal delegate void OnSelectedItemClickedHandler(DefaultScrollerView scrollerView);
            internal event OnSelectedItemClickedHandler OnSelectedItemClicked;
            protected virtual void fireOnSelectedItemClicked(DefaultScrollerView scrollerView)
            {
                var handler = this.OnSelectedItemClicked;
                if (handler != null) handler(scrollerView);
            }

            internal void GUISetup(BottomButtons bottomButtons)
            {
                bottomButtons.OnGroupByChanged += view => this.reset();
            }
        }
    }