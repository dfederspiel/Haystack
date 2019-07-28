using System.Linq;
using UnityEngine;

namespace HaystackReContinued
{
        public class VesselInfoView
        {
            internal bool Clicked { get; set; }
            private Vessel expandedVessel = null;
            private bool selected;
            private DockingPortListView dockingPortListView;

            private HaystackContinued haystackContinued;
            public BottomButtons bottomButtons;

            internal VesselInfoView(HaystackContinued haystackContinued)
            {
                this.haystackContinued = haystackContinued;
                this.bottomButtons = this.haystackContinued.bottomButtons;
                this.dockingPortListView = new DockingPortListView(haystackContinued);
            }

            internal void Reset()
            {
                this.Clicked = false;
                this.expandedVessel = null;
                this.selected = false;
                this.dockingPortListView.CurrentVessel = null;
            }

            internal void Draw(Vessel vessel, bool selected, Vessel activeVessel)
            {
                this.Clicked = false;
                this.selected = selected;

                if (this.bottomButtons.IsHiddenVesselsToggled &&
                    !global::HaystackReContinued.HiddenVessels.ExcludedTypes.Contains(vessel.vesselType))
                {

                    GUILayout.BeginHorizontal();

                    var hidden = this.haystackContinued.isVesselHidden(vessel);

                    var tooltip = hidden ? "Show vessel" : "Hide vessel";

                    var change = GUILayout.Toggle(hidden, new GUIContent(Resources.btnHiddenIcon, tooltip),
                        GUI.skin.button, GUILayout.Height(24f), GUILayout.Height(24f));
                    if (hidden != change)
                    {
                        this.haystackContinued.markVesselHidden(vessel, change);
                    }
                }

                GUILayout.BeginVertical(selected ? Resources.vesselInfoSelected : Resources.vesselInfoDefault);

                GUILayout.BeginHorizontal();
                GUILayout.Label(vessel.vesselName, Resources.textListHeaderStyle);
                GUILayout.FlexibleSpace();
                this.drawDistance(vessel, activeVessel);
                GUILayout.EndHorizontal();

                this.drawVesselInfoText(vessel, activeVessel);

                GUILayout.EndVertical();

                var check = GUILayoutUtility.GetLastRect();

                if (this.bottomButtons.IsHiddenVesselsToggled &&
                    !global::HaystackReContinued.HiddenVessels.ExcludedTypes.Contains(vessel.vesselType))
                {
                    GUILayout.EndHorizontal();
                }


                if (Event.current != null && Event.current.type == EventType.MouseDown &&
                    Input.GetMouseButtonDown(0) &&
                    check.Contains(Event.current.mousePosition))
                {
                    this.Clicked = true;
                }
            }

            private void drawDistance(Vessel vessel, Vessel activeVessel)
            {
                string distance = "";

                if (HSUtils.IsInFlight && vessel != activeVessel && vessel != null && activeVessel != null)
                {
                    var calcDistance = Vector3.Distance(activeVessel.transform.position, vessel.transform.position);
                    distance = HSUtils.ToSI(calcDistance) + "m";
                }

                GUILayout.Label(distance, Resources.textSituationStyle);
            }


            private void drawVesselInfoText(Vessel vessel, Vessel activeVessel)
            {
                string status = "";
                int cnt = 0;
                if (activeVessel == vessel)
                {
                    status = ". Currently active";
                    cnt = vessel.Parts.Count;
                }
                else if (vessel.loaded)
                {
                    status = ". Loaded";
                    cnt = vessel.Parts.Count;
                    //cnt = vessel.protoVessel.protoPartSnapshots.Count;
                }
                else cnt = vessel.protoVessel.protoPartSnapshots.Count;

                string situation = "";
                if (cnt == 1)
                    situation = string.Format("{0}. {1}{2}",
                       vessel.vesselType,
                       Vessel.GetSituationString(vessel),
                       status
                       );
                else
                    situation = string.Format("{0}. {1}{2}. Parts: {3}",
                       vessel.vesselType,
                       Vessel.GetSituationString(vessel),
                       status, cnt
                       );

                GUILayout.BeginHorizontal();

                GUILayout.Label(situation, Resources.textSituationStyle);
                if (this.selected)
                {
                    GUILayout.FlexibleSpace();

                    drawDockingExpandButton(vessel);
                }
                GUILayout.EndHorizontal();


                this.dockingPortListView.Draw(vessel);
            }

            private void drawDockingExpandButton(Vessel vessel)
            {
                //can't show docking ports in the tracking center or space center
                if (HSUtils.IsTrackingCenterActive || HSUtils.IsSpaceCenterActive)
                {
                    return;
                }

                var enabled = vessel == this.expandedVessel;
                var icon = enabled ? Resources.btnDownArrow : Resources.btnUpArrow;

                var result = GUILayout.Toggle(enabled, new GUIContent(icon, "Show Docking Ports"),
                    Resources.buttonExpandStyle);

                if (result != enabled)
                {
                    this.expandedVessel = result ? vessel : null;
                    this.dockingPortListView.CurrentVessel = this.expandedVessel;
                }
            }
        }
    }