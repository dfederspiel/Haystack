using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;

using ClickThroughFix;

namespace HaystackReContinued
{
    public class ExpandedVesselInfo : IDisposable
    {
        private HaystackContinued haystack;
        private DefaultScrollerView defaultScrollerView;
        private GroupedScrollerView groupedScrollerView;
        private BottomButtons bottomButtons;

        private bool _isExpanded;
        private VesselData vesselData = new VesselData();
        private BodyData bodyData = new BodyData();

        private readonly int windowId = Resources.rnd.Next(1000, 2000000);
        private Vector2 scrollPosition = new Vector2(0, 0);
        private Rect windowRect;

        private GUIContent renameContent = new GUIContent("R", "Rename vessel");
        private GUIContent terminateContent = new GUIContent(Resources.btnTerminateNormalBackground, "Terminate vessel");
        //btnTerminateFilePath

        internal ExpandedVesselInfo(HaystackContinued haystack, BottomButtons bottomButtons, DefaultScrollerView defaultScrollerView,
            GroupedScrollerView groupedScrollerView)
        {
            this.haystack = haystack;
            this.defaultScrollerView = defaultScrollerView;
            this.groupedScrollerView = groupedScrollerView;

            this.defaultScrollerView.OnSelectionChanged += view => this.updateData();
            this.defaultScrollerView.OnSelectedItemClicked += view => this.IsExpanded = !this.IsExpanded;
            this.groupedScrollerView.OnSelectionChanged += view => this.updateData();
            this.groupedScrollerView.OnSelectedItemClicked += view => this.IsExpanded = !this.IsExpanded;

            this.bottomButtons = bottomButtons;
            this.windowRect = new Rect(this.haystack.WinRect.xMax, this.haystack.WinRect.y, 0,
                   this.haystack.winRect.height);
        }

        private bool IsVessel
        {
            get
            {
                if (bottomButtons.GroupByOrbitingBody)
                {
                    if (groupedScrollerView.SelectedVessel != null)
                    {
                        return true;
                    }
                }
                else
                {
                    if (defaultScrollerView.SelectedVessel != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private Vessel currentVessel
        {
            get
            {
                return this.bottomButtons.GroupByOrbitingBody ?
                    this.groupedScrollerView.SelectedVessel : this.defaultScrollerView.SelectedVessel;
            }
        }

        private bool IsBody
        {
            get { return (!bottomButtons.GroupByOrbitingBody && defaultScrollerView.SelectedBody != null); }
        }

        private CelestialBody currentBody
        {
            get
            {
                if (!bottomButtons.GroupByOrbitingBody)
                {
                    return defaultScrollerView.SelectedBody;
                }
                return null;
            }
        }

        private bool IsExpanded
        {
            get { return this._isExpanded; }
            set
            {
                if (value)
                {
                    this.updateData();
                    this.haystack.StartCoroutine(this.updateDataCoroutine());
                }
                else
                {
                    this.scrollPosition = new Vector2(0, 0);
                }
                this._isExpanded = value;
            }
        }

        private void updateData()
        {
            this.windowRect.width = 0;
            if (IsVessel)
            {
                updateVesselData();
            }
            if (IsBody)
            {
                this.updateBodyData();
            }
        }
        private void updateBodyData()
        {
            var body = this.currentBody;
            var same = body.bodyName.GetHashCode() == this.bodyData.Id;

            var newBodyData = new BodyData
            {
                Id = body.bodyName.GetHashCode(),
                OrbitData = body.orbit != null ? OrbitData.FromOrbit(body.orbit) : new OrbitData(),
                PhysicalData = same ? this.bodyData.PhysicalData : getPhysicalData(body),
                AtmData = same ? this.bodyData.AtmData : getAtmData(body),
                SciData = same ? this.bodyData.SciData : getSciData(body),
                Satellites = same ? this.bodyData.Satellites : getSatellites(body),
            };

            this.bodyData = newBodyData;
        }

        private List<DisplayItem> getSatellites(CelestialBody body)
        {
            return (from satellite in body.orbitingBodies
                    select DisplayItem.Create(satellite.bodyName, "")).ToList();
        }

        private List<DisplayItem> getSciData(CelestialBody body)
        {
            var items = new List<DisplayItem>();
            var sci = body.scienceValues;

            var spaceHigh = DisplayItem.Create("Space High Alt: ", sci.spaceAltitudeThreshold.ToString("N0") + "m");
            items.Add(spaceHigh);
            if (body.atmosphere)
            {
                var flyingHigh = DisplayItem.Create("Flying High Alt: ",
                    sci.flyingAltitudeThreshold.ToString("N0") + "m");
                items.Add(flyingHigh);
            }

            return items;
        }

        private List<DisplayItem> getAtmData(CelestialBody body)
        {
            var items = new List<DisplayItem>();

            if (!body.atmosphere)
            {
                return items;
            }

            var maxHeight = DisplayItem.Create("Atmopshere Ends: ", body.atmosphereDepth.ToString("N0") + "m");
            items.Add(maxHeight);
            var oxygen = DisplayItem.Create("Oxygen?: ", body.atmosphereContainsOxygen ? "Yes" : "No");
            items.Add(oxygen);

            var kPAASL = body.GetPressure(0d);
            var atmASL = kPAASL * PhysicsGlobals.KpaToAtmospheres;
            var aslDisplay = string.Format("{0}kPa ({1}atm)", kPAASL.ToString("F2"), atmASL.ToString("F2"));
            var aslPressure = DisplayItem.Create("Atm. ASL: ", aslDisplay);
            items.Add(aslPressure);

            var surfaceTemp = DisplayItem.Create("Surface Temp: ", body.GetTemperature(0.0).ToString("0.##") + "K");
            items.Add(surfaceTemp);

            return items;
        }

        private List<DisplayItem> getPhysicalData(CelestialBody body)
        {
            var radius = DisplayItem.Create("Radius: ", (body.Radius / 1000d).ToString("N0") + "km");
            var mass = DisplayItem.Create("Mass: ", body.Mass.ToString("0.###E+0") + "kg");
            var gm = DisplayItem.Create("GM: ", body.gravParameter.ToString("0.###E+0"));
            var gravity = DisplayItem.Create("Surface Gravity: ", body.GeeASL.ToString("0.####") + "g");

            var escape = 2d * body.gravParameter / body.Radius;
            escape = Math.Sqrt(escape);

            var escapeVelocity = DisplayItem.Create("Escape Velocity: ", escape.ToString("0.0") + "m/s");

            double alt = body.atmosphere ? body.atmosphereDepth + 20000 : 15000;
            var orbitVelocity = Math.Sqrt(body.gravParameter / (body.Radius + alt));

            var standardOrbitVelocity = DisplayItem.Create("Std Orbit Velocity: ",
                orbitVelocity.ToString("0.0") + "m/s @ " + Converters.Distance(alt));

            var rotationalPeriod = DisplayItem.Create("Rotational Period: ", Converters.Duration(body.rotationPeriod));
            var tidalLocked = DisplayItem.Create("Tidally Locked: ", body.tidallyLocked ? "Yes" : "No");
            var soiSize = DisplayItem.Create("SOI Size: ", (body.sphereOfInfluence / 1000d).ToString("N0") + "km");

            return new List<DisplayItem>
                {
                    radius,
                    mass,
                    gm,
                    gravity,
                    standardOrbitVelocity,
                    escapeVelocity,
                    rotationalPeriod,
                    tidalLocked,
                    soiSize
                };
        }

        private void updateVesselData()
        {
            var vessel = this.currentVessel;

            bool hasNextNode = false;

            var nextNodeTime = Vessel.GetNextManeuverTime(vessel, out hasNextNode);

            // some stuff doesn't need to be updated if it's not changing
            var shouldUpdate = vessel.isActiveVessel || vessel.loaded || !this.vesselData.Id.Equals(vessel.id);

            var vesselData = new VesselData
            {
                Id = vessel.id,
                OrbitData = OrbitData.FromOrbit(vessel.orbit),
                Resources = shouldUpdate ? updateVesselResourceData(vessel) : this.vesselData.Resources,
                CrewData = shouldUpdate ? this.updateVesselCrewData(vessel) : this.vesselData.CrewData,
                MET = Converters.Duration(Math.Abs(vessel.missionTime)),
                HasNextNode = hasNextNode,
                NextNodeIn = "T " + KSPUtil.PrintTime(Planetarium.GetUniversalTime() - nextNodeTime, 3, true),
                NextNodeTime = KSPUtil.PrintDateCompact(nextNodeTime, true, true),
                Situation = Vessel.GetSituationString(vessel),
            };

            this.vesselData = vesselData;
        }

        private CrewData updateVesselCrewData(Vessel vessel)
        {
            var crewData = new CrewData();

            if (vessel.isEVA)
            {
                return crewData;
            }

            if (vessel.loaded)
            {
                crewData.TotalCrew = vessel.GetCrewCount();
                crewData.MaxCrew = vessel.GetCrewCapacity();

                crewData.Crew = (from part in vessel.parts
                                 from crew in part.protoModuleCrew
                                 orderby crew.name
                                 select formatCrewDisplay(part.partInfo.title, crew)).ToList();
            }
            else
            {
                crewData.TotalCrew = vessel.protoVessel.GetVesselCrew().Count;

                crewData.MaxCrew = vessel.protoVessel.protoPartSnapshots.Aggregate(0,
                    (acc, part) => acc + part.partInfo.partPrefab.CrewCapacity);

                crewData.Crew = (from part in vessel.protoVessel.protoPartSnapshots
                                 from crew in part.protoModuleCrew
                                 orderby crew.name
                                 select this.formatCrewDisplay(part.partInfo.title, crew)).ToList();
            }

            return crewData;
        }

        private string formatCrewDisplay(string partName, ProtoCrewMember crew)
        {
            var profession = crew.experienceTrait.Title.Substring(0, 1);
            var name = crew.name;
            var experiance = crew.experienceLevel;

            return string.Format("{0} ({1}:{2}) - {3}", name, profession, experiance, partName);
        }

        private List<string> updateVesselResourceData(Vessel vessel)
        {
            if (vessel.loaded)
            {
                var counts = from part in vessel.parts
                             from resource in part.Resources
                             group resource by resource.resourceName
                    into resources
                             select new
                             {
                                 ResourceName = resources.Key,
                                 Total = resources.Aggregate(0d, (acc, r) => acc + r.amount),
                                 Max = resources.Aggregate(0d, (acc, r) => acc + r.maxAmount),
                             };

                return (from resource in counts
                        orderby resource.ResourceName
                        select
                            string.Format("{0}: {1}/{2} ({3})", resource.ResourceName, resource.Total.ToString("N1"), resource.Max.ToString("N1"),
                                (resource.Total / resource.Max).ToString("P1"))).ToList();
            }
            else
            {
                var counts = from proto in vessel.protoVessel.protoPartSnapshots
                             from resource in proto.resources
                             group resource by resource.resourceName
                    into resources
                             select new
                             {
                                 ResourceName = resources.Key,
                                 Total =
                                     resources.Aggregate(0d, (acc, r) => acc + r.amount),
                                 Max =
                                     resources.Aggregate(0d,
                                         (acc, r) => acc + r.maxAmount)
                             };



                return (from resource in counts
                        orderby resource.ResourceName
                        select
                            string.Format("{0}: {1}/{2} ({3})", resource.ResourceName, resource.Total.ToString("N1"), resource.Max.ToString("N1"),
                                (resource.Total / resource.Max).ToString("P1"))).ToList();
            }
        }

        private IEnumerator updateDataCoroutine()
        {
            yield return new WaitForEndOfFrame();
            while (this.IsExpanded && (this.IsVessel || this.IsBody))
            {
                this.updateData();
                yield return new WaitForSeconds(1f);
            }
        }

        public void DrawExpandButton()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button(this.IsExpanded ? Resources.btnExtendedIconClose : Resources.btnExtendedIconOpen, Resources.buttonExtendedStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false), GUILayout.Width(12f)))
            {
                this.IsExpanded = !this.IsExpanded;
            }
            GUILayout.Space(20f);
            GUILayout.EndVertical();
        }

        public void DrawExpandedWindow()
        {
            if (!IsExpanded)
            {
                return;
            }

            this.windowRect.x = this.haystack.WinRect.xMax;
            this.windowRect.y = this.haystack.WinRect.y;
            this.windowRect.height = this.haystack.WinRect.height;
            var rect = ClickThruBlocker.GUILayoutWindow(this.windowId, this.windowRect, this.drawExpanded, "Vessel Infomation", Resources.winStyle, new[] { GUILayout.MaxWidth(600), GUILayout.MinHeight(this.haystack.winRect.height), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false) });
            this.windowRect.width = rect.width;
        }

        private void drawExpanded(int id)
        {
            GUILayout.BeginHorizontal();
            this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition, GUILayout.Width(300f));
            GUILayout.BeginVertical();

            if (this.IsVessel)
            {
                drawVessel();
            }
            else if (this.IsBody)
            {
                drawBody();
            }
            else
            {
                GUILayout.Label("Nothing is selected", Resources.textListHeaderStyle);
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();

            if (GUI.tooltip != "")
            {
                var mousePosition = Event.current.mousePosition;
                var content = new GUIContent(GUI.tooltip);
                var size = Resources.tooltipBoxStyle.CalcSize(content);
                GUI.Box(new Rect(mousePosition.x - 30, mousePosition.y - 30, size.x + 12, 25).ClampToPosIn(this.windowRect), content, Resources.tooltipBoxStyle);
            }
        }

        private void drawBody()
        {
            GUILayout.Space(4f);
            GUILayout.Label(this.currentBody.bodyName, Resources.textListHeaderStyle);

            if (this.currentBody.orbit != null)
            {
                GUILayout.Space(10f);
                this.drawOrbit(this.bodyData.OrbitData);
            }

            GUILayout.Space(10f);
            this.drawItemList("Science Information:", this.bodyData.SciData);

            GUILayout.Space(10f);
            this.drawItemList("Physical Information:", this.bodyData.PhysicalData);

            if (this.currentBody.atmosphere)
            {
                GUILayout.Space(10f);
                this.drawItemList("Atmospheric Information:", this.bodyData.AtmData);
            }

            if (!this.bodyData.Satellites.IsEmpty())
            {
                GUILayout.Space(10f);
                this.drawItemList("Satellites:", this.bodyData.Satellites);
            }
        }

        private void TerminateVessel()
        {
            //this.unlockUI();
            Vessel vessel = this.currentVessel;

            GameEvents.onVesselTerminated.Fire(vessel.protoVessel);
            KSP.UI.Screens.SpaceTracking.StopTrackingObject(vessel);
            List<ProtoCrewMember> vesselCrew = vessel.GetVesselCrew();
            int count = vesselCrew.Count;
            for (int i = 0; i < count; i++)
            {
                ProtoCrewMember protoCrewMember = vesselCrew[i];
                UnityEngine.Debug.Log("Crewmember " + protoCrewMember.name + " is lost.");
                protoCrewMember.StartRespawnPeriod(-1.0);
            }
            UnityEngine.Object.DestroyImmediate(vessel.gameObject);
            GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
            this.OnDialogDismiss();
        }

        private void OnDialogDismiss()
        {
            popupDialog = false;
            //this.unlockUI();
        }
        internal bool popupDialog = false;

        private void drawVessel()
        {
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Label(this.currentVessel.vesselName, Resources.textExpandedVesselNameStyle, GUILayout.ExpandWidth(false));

            GUILayout.Space(8f);
            if (GUILayout.Button(renameContent, Resources.buttonRenameStyle, GUILayout.Width(16f), GUILayout.Height(16f)))
            {
                this.currentVessel.RenameVessel();
            }
            GUILayout.FlexibleSpace();
            if (HighLogic.CurrentGame.Mode != Game.Modes.MISSION_BUILDER && this.currentVessel != FlightGlobals.ActiveVessel)
            {
                if (GUILayout.Button(terminateContent, Resources.buttonTerminateStyle, GUILayout.Width(16f), GUILayout.Height(16f)))
                {
                    //TerminateVessel(this.currentVessel);
                    popupDialog = true;

                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("TerminateMission", Localizer.Format("#autoLOC_481625"), Localizer.Format("#autoLOC_5050048"), HighLogic.UISkin, new DialogGUIBase[]
                           {
                                        new DialogGUIButton("Terminate", new Callback(this.TerminateVessel)),
                                        new DialogGUIButton("Cancel", new Callback(this.OnDialogDismiss))
                           }), false, HighLogic.UISkin, true, string.Empty);

                }
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            int cnt = 0;
            if (currentVessel.loaded)
                cnt = currentVessel.Parts.Count;
            else
                cnt = currentVessel.protoVessel.protoPartSnapshots.Count;
            GUILayout.Label("Parts: " + cnt, Resources.textVesselExpandedInfoItem);
            GUILayout.Space(10f);

            GUILayout.EndHorizontal();

            drawResources();
            drawCrew();
            drawOrbit(vesselData.OrbitData);

            GUILayout.Space(10f);
            GUILayout.Label("Status:", Resources.textListHeaderStyle);
            GUILayout.Label(this.vesselData.Situation, Resources.textVesselExpandedInfoItem);
            GUILayout.Label("MET: " + this.vesselData.MET, Resources.textVesselExpandedInfoItem);

            if (this.vesselData.HasNextNode)
            {
                GUILayout.Label("Maneuver Node in: " + this.vesselData.NextNodeIn, Resources.textVesselExpandedInfoItem);
                GUILayout.Label("Maneuver Node @: " + this.vesselData.NextNodeTime, Resources.textVesselExpandedInfoItem);
            }
        }

        private void drawOrbit(OrbitData orbitData)
        {
            GUILayout.Label("Orbial information:", Resources.textListHeaderStyle);
            GUILayout.Label("SOI: " + orbitData.SOI, Resources.textVesselExpandedInfoItem);
            GUILayout.Label("Apoapsis: " + orbitData.AP, Resources.textVesselExpandedInfoItem);
            GUILayout.Label("Periapsis: " + orbitData.PE, Resources.textVesselExpandedInfoItem);
            GUILayout.Label("Time to AP: " + orbitData.timeToAP, Resources.textVesselExpandedInfoItem);
            GUILayout.Label("Time to PE: " + orbitData.timeToPE, Resources.textVesselExpandedInfoItem);
            GUILayout.Label("Orbital Period: " + orbitData.Period, Resources.textVesselExpandedInfoItem);
            GUILayout.Label("Inclination: " + orbitData.INC, Resources.textVesselExpandedInfoItem);
            if (orbitData.IsSOIChange)
            {
                GUILayout.Label("SOI Change Time: " + orbitData.SOIChangeTime, Resources.textVesselExpandedInfoItem);
                GUILayout.Label("SOI Change Date: " + orbitData.SOIChangeDate, Resources.textVesselExpandedInfoItem);
            }
        }

        private void drawCrew()
        {
            if (!this.vesselData.HasCrew)
            {
                return;
            }
            var crewData = this.vesselData.CrewData;

            var crewDisplay = string.Format("Crew ({0} / {1}):", crewData.TotalCrew, crewData.MaxCrew);
            GUILayout.Label(crewDisplay, Resources.textListHeaderStyle);

            foreach (var crew in crewData.Crew)
            {
                GUILayout.Label(crew, Resources.textVesselExpandedInfoItem);
            }

            GUILayout.Space(10f);
        }

        private void drawResources()
        {
            if (!this.vesselData.HasResources)
            {
                return;
            }

            GUILayout.Label("Resources:", Resources.textListHeaderStyle);

            foreach (var resourceDisplay in this.vesselData.Resources)
            {
                GUILayout.Label(resourceDisplay, Resources.textVesselExpandedInfoItem);
            }

            GUILayout.Space(10f);
        }

        private void drawItemList(string header, IEnumerable<DisplayItem> items)
        {
            GUILayout.Label(header, Resources.textListHeaderStyle);
            foreach (var item in items)
            {
                GUILayout.Label(string.Format("{0} {1}", item.Label, item.Value), Resources.textVesselExpandedInfoItem);
            }
        }

        public void Dispose()
        {
            this.IsExpanded = false;
        }
    }
}