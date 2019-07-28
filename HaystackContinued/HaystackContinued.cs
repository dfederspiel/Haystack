using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using ClickThroughFix;

namespace HaystackReContinued
{
    public abstract class HaystackContinued : MonoBehaviour
    {
        // window vars
        private int windowId;
        protected bool WinVisible = false;
        public Rect winRect;
        private bool isGUISetup;


        // controllers and managers
        private VesselListController vesselListController;

        //controls
        private ResizeHandle resizeHandle;
        private DefaultScrollerView defaultScrollerView;
        private GroupedScrollerView groupedScrollerView;
        public BottomButtons bottomButtons;
        private ExpandedVesselInfo expandedVesselInfo;


        public void Awake()
        {
            HSUtils.DebugLog("HaystackContinued#Awake");

            this.bottomButtons = new BottomButtons();
            this.bottomButtons.LoadSettings();

            this.vesselListController = new VesselListController(this, this.bottomButtons);
            this.defaultScrollerView = new DefaultScrollerView(this, this.vesselListController);
            this.groupedScrollerView = new GroupedScrollerView(this, this.vesselListController);
            this.expandedVesselInfo = new ExpandedVesselInfo(this, this.bottomButtons, this.defaultScrollerView,
                this.groupedScrollerView);
            this.resizeHandle = new ResizeHandle();


            windowId = Resources.rnd.Next(1000, 2000000);
        }


        private void onDataLoadedHandler()
        {
            this.vesselListController.RefreshFilteredList();
        }

        public void OnEnable()
        {
            HSUtils.DebugLog("HaystackContinued#OnEnable");

            GameEvents.onPlanetariumTargetChanged.Add(this.onMapTargetChange);

            this.WinRect = HaystackResourceLoader.Instance.Settings.WindowPositions[this.SettingsName];
            this.WinVisible = HaystackResourceLoader.Instance.Settings.WindowVisibilities[this.SettingsName];
            this.bottomButtons.LoadSettings();

            HaystackResourceLoader.Instance.DisplayButtonOnClick += this.displayButtonClicked;

            InvokeRepeating("IRFetchVesselList", 5.0F, 5.0F);
            InvokeRepeating("RefreshDataSaveSettings", 0, 30.0F);

            //HaystackResourceLoader.Instance.FixApplicationLauncherButtonDisplay(this.WinVisible);
        }

        public void OnDisable()
        {
            HSUtils.DebugLog("HaystackContinued#OnDisable");
            CancelInvoke();

            GameEvents.onPlanetariumTargetChanged.Remove(this.onMapTargetChange);

            HaystackResourceLoader.Instance.DisplayButtonOnClick -= this.displayButtonClicked;

            HaystackResourceLoader.Instance.Settings.WindowPositions[this.SettingsName] = this.WinRect;
            HaystackResourceLoader.Instance.Settings.WindowVisibilities[this.SettingsName] = this.WinVisible;
            this.bottomButtons.SaveSettings();

            HaystackResourceLoader.Instance.Settings.Save();
        }

        private void displayButtonClicked(EventArgs e)
        {
            this.WinVisible = !this.WinVisible;
        }

        public void Start()
        {
            // not an anonymous functions because we need to remove them in #OnDestroy
            GameEvents.onHideUI.Add(onHideUI);
            GameEvents.onShowUI.Add(onShowUI);
            GameEvents.onVesselChange.Add(onVesselChange);
            GameEvents.onVesselWasModified.Add(onVesselWasModified);
            GameEvents.onVesselRename.Add(onVesselRenamed);

            this.vesselListController.FetchVesselList();
            DataManager.Instance.OnDataLoaded += this.onDataLoadedHandler;
        }

        private void onVesselRenamed(GameEvents.HostedFromToAction<Vessel, string> data)
        {
            this.vesselListController.FetchVesselList();
        }

        private void onVesselWasModified(Vessel data)
        {
            this.vesselListController.FetchVesselList();
        }

        private void onVesselChange(Vessel data)
        {
            this.vesselListController.FetchVesselList();
        }

        //called when the game tells us that the UI is going to be shown again
        private void onShowUI()
        {
            this.UIHide = false;
        }

        //called when the game tells us that the UI is to be hidden; used for screenshots generally
        private void onHideUI()
        {
            this.UIHide = true;
        }

        protected bool UIHide { get; set; }

        public void OnDestory()
        {
            HSUtils.DebugLog("HaystackContinued#OnDestroy");

            GameEvents.onHideUI.Remove(this.onHideUI);
            GameEvents.onShowUI.Remove(this.onShowUI);
            GameEvents.onVesselChange.Remove(this.onVesselChange);
            GameEvents.onVesselWasModified.Remove(this.onVesselWasModified);
            GameEvents.onVesselRename.Remove(this.onVesselRenamed);

            DataManager.Instance.OnDataLoaded -= this.onDataLoadedHandler;
            this.expandedVesselInfo.Dispose();
        }

        private void onMapTargetChange(MapObject mapObject)
        {
            if (!HSUtils.IsTrackingCenterActive)
            {
                return;
            }

            if (mapObject == null)
            {
                return;
            }

            switch (mapObject.type)
            {
                case MapObject.ObjectType.Vessel:
                    this.defaultScrollerView.SelectedVessel = mapObject.vessel;
                    this.groupedScrollerView.SelectedVessel = mapObject.vessel;
                    break;
                case MapObject.ObjectType.CelestialBody:
                    this.defaultScrollerView.SelectedBody = mapObject.celestialBody;
                    break;
                default:
                    this.defaultScrollerView.SelectedBody = null;
                    this.defaultScrollerView.SelectedVessel = null;
                    break;
            }
        }


        public void IRFetchVesselList()
        {
            if (!this.IsGuiDisplay) return;

            this.vesselListController.RefreshFilteredList();
        }


        /// <summary>
        /// Function called every 30 seconds
        /// </summary>
        public void RefreshDataSaveSettings()
        {
            if (!this.IsGuiDisplay) return;

            HaystackResourceLoader.Instance.Settings.WindowPositions[this.SettingsName] = this.WinRect;
            this.bottomButtons.SaveSettings();
        }

        /// <summary>
        /// Repaint GUI
        /// </summary>
        public void OnGUI()
        {
            if (!this.isGUISetup)
            {
                Resources.LoadStyles();

                //TODO: eliminate
                this.groupedScrollerView.GUISetup(this.bottomButtons);
                this.defaultScrollerView.GUISetup(this.bottomButtons);
                this.bottomButtons.GUISetup(this.groupedScrollerView, this.defaultScrollerView);

                this.bottomButtons.OnSwitchVessel += vessel => this.StartCoroutine(SwitchToVessel(vessel));

                this.vesselListController.FetchVesselList();

                this.isGUISetup = true;
            }

            if (this.IsGuiDisplay)
            {
                this.drawGUI();
            }
        }

        private static IEnumerator SwitchToVessel(Vessel vessel)
        {
            yield return new WaitForFixedUpdate();

            if (HSUtils.IsTrackingCenterActive)
            {
                HSUtils.TrackingSwitchToVessel(vessel);
            }
            else if (HSUtils.IsSpaceCenterActive)
            {
                HSUtils.SwitchAndFly(vessel);
            }
            else
            {
                FlightGlobals.SetActiveVessel(vessel);
            }
        }

        public Rect WinRect
        {
            get { return this.winRect; }
            set { this.winRect = value; }
        }

        private void drawGUI()
        {
            GUI.skin = HighLogic.Skin;
            if (expandedVesselInfo.popupDialog)
                return;
            this.winRect = this.winRect.ClampToScreen();


            this.winRect = ClickThruBlocker.GUILayoutWindow(windowId, this.winRect, this.mainWindowConstructor,
                string.Format("Haystack ReContinued {0}", Settings.version), Resources.winStyle, GUILayout.MinWidth(120),
                GUILayout.MinHeight(300));

            this.expandedVesselInfo.DrawExpandedWindow();

            // do this here since if it's done within the window you only recieve events that are inside of the window
            this.resizeHandle.DoResize(ref this.winRect);
        }

        /// <summary>
        /// Checks if the GUI should be drawn in the current scene
        /// </summary>
        protected virtual bool IsGuiDisplay
        {
            get { return false; }
        }

        public bool isVesselHidden(Vessel vessel)
        {
            return this.HiddenVessels.Contains(vessel.id);
        }

        public void markVesselHidden(Vessel vessel, bool mark)
        {
            HSUtils.DebugLog("HaystackContinued#markVesselHidden: {0} {1}", vessel.name, mark);
            if (mark)
            {
                DataManager.Instance.HiddenVessels.AddVessel(vessel);
            }
            else
            {
                DataManager.Instance.HiddenVessels.RemoveVessle(vessel);
            }
        }

        protected abstract string SettingsName { get; }

        public HashSet<Guid> HiddenVessels
        {
            get { return DataManager.Instance.HiddenVessels.VesselList; }
        }

        private void mainWindowConstructor(int windowID)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            #region vessel types - horizontal

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Vessels
            var typeCounts = this.vesselListController.VesselTypeCounts;
            for (int i = 0; i < Resources.vesselTypesList.Count(); i++)
            {
                var typeString = Resources.vesselTypesList[i].name;

                if (typeCounts.ContainsKey(typeString))
                    typeString += string.Format(" ({0})", typeCounts[typeString]);

                var previous = Resources.vesselTypesList[i].visible;

                Resources.vesselTypesList[i].visible = GUILayout.Toggle(Resources.vesselTypesList[i].visible,
                    new GUIContent(Resources.vesselTypesList[i].icon, typeString), Resources.buttonVesselTypeStyle);

                if (previous != Resources.vesselTypesList[i].visible)
                {
                    this.vesselListController.RefreshFilteredList();
                }

                if (typeString.Equals(Resources.BODIES))
                {
                    defaultScrollerView.ShowCelestialBodies = Resources.vesselTypesList[i].visible;
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            #endregion vessel types

            //Search area

            GUILayout.BeginHorizontal();

            GUILayout.Label("Search:", Resources.textSearchStyle);
            this.vesselListController.SearchTerm = GUILayout.TextField(this.vesselListController.SearchTerm, GUILayout.MinWidth(50.0F), GUILayout.ExpandWidth(true));

            // clear search text
            if (GUILayout.Button("x", Resources.buttonSearchClearStyle))
            {
                this.vesselListController.SearchTerm = "";
            }

            GUILayout.EndHorizontal();

            if (this.bottomButtons.GroupByOrbitingBody)
            {
                this.groupedScrollerView.Draw();
            }
            else
            {
                this.defaultScrollerView.Draw();
            }

            this.bottomButtons.Draw();

            // handle tooltips here so it is on top
            if (GUI.tooltip != "")
            {
                var mousePosition = Event.current.mousePosition;
                var content = new GUIContent(GUI.tooltip);
                var size = Resources.tooltipBoxStyle.CalcSize(content);
                GUI.Box(new Rect(mousePosition.x - 30, mousePosition.y - 30, size.x + 12, 25).ClampToPosIn(this.WinRect), content, Resources.tooltipBoxStyle);
            }

            GUILayout.EndVertical();

            this.expandedVesselInfo.DrawExpandButton();

            GUILayout.EndHorizontal();

            this.resizeHandle.Draw(ref this.winRect);

            // If user input detected, force refresh
            if (GUI.changed)
            {
                // might want to make this a bit more granular but it seems ok atm
                this.vesselListController.RefreshFilteredList();
            }

            GUI.DragWindow();
        }
    } // HaystackContinued
}