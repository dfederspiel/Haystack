using UnityEngine;

namespace HaystackReContinued
{
    public class BottomButtons
    {
        private GroupedScrollerView groupedScrollerView;
        private DefaultScrollerView defaultScrollerView;

        internal bool GroupByOrbitingBody { get; private set; }
        internal bool IsHiddenVesselsToggled { get; private set; }
        internal bool IsNearbyOnly { get; private set; }

        private bool isAscendingSortOrder = true;

        internal bool IsAscendingSortOrder
        {
            get { return this.isAscendingSortOrder; }
            private set { this.isAscendingSortOrder = value; }
        }

        private static readonly GUIContent groupByOrbitContent = new GUIContent(Resources.btnOrbitIcon, "Group by orbiting body");
        private static readonly GUIContent hiddenVesselsButtonContent = new GUIContent(Resources.btnHiddenIcon, "Manage hidden vessels");
        private static readonly GUIContent nearbyButtonContent = new GUIContent("NB", "Nearby vessels only");
        private static readonly GUIContent ascendingButtonContent = new GUIContent(Resources.btnAscendingIcon, "Ascending sort order");
        private static readonly GUIContent descendingButtonContent = new GUIContent(Resources.btnDescendingIcon, "Descending sort order");

        internal void LoadSettings()
        {
            this.IsAscendingSortOrder = HaystackResourceLoader.Instance.Settings.BottomButtons["ascending"];
            this.IsNearbyOnly = HaystackResourceLoader.Instance.Settings.BottomButtons["nearby"];
            this.GroupByOrbitingBody = HaystackResourceLoader.Instance.Settings.BottomButtons["groupby"];
        }

        internal void SaveSettings()
        {
            HaystackResourceLoader.Instance.Settings.BottomButtons["ascending"] = this.IsAscendingSortOrder;
            HaystackResourceLoader.Instance.Settings.BottomButtons["nearby"] = this.IsNearbyOnly;
            HaystackResourceLoader.Instance.Settings.BottomButtons["groupby"] = this.GroupByOrbitingBody;
        }

        private void groupByButton()
        {
            //group by toggle
            var previous = this.GroupByOrbitingBody;
            this.GroupByOrbitingBody = GUILayout.Toggle(this.GroupByOrbitingBody,
                groupByOrbitContent,
                GUI.skin.button, GUILayout.Width(32f), GUILayout.Height(32f));

            if (previous != this.GroupByOrbitingBody)
            {
                this.fireOnGroupByChanged(this);
            }
        }

        private void nearbyButton()
        {
            var previous = this.IsNearbyOnly;
            this.IsNearbyOnly = GUILayout.Toggle(this.IsNearbyOnly,
                nearbyButtonContent, GUI.skin.button, GUILayout.Width(32f),
                GUILayout.Height(32f));

            if (previous != this.IsNearbyOnly)
            {
                this.fireOnNearbyChanged(this);
            }
        }

        private void hiddenVesselsButton()
        {
            var previous = this.IsHiddenVesselsToggled;
            this.IsHiddenVesselsToggled = GUILayout.Toggle(this.IsHiddenVesselsToggled,
                hiddenVesselsButtonContent, GUI.skin.button,
                GUILayout.Width(32f), GUILayout.Height(32f));

            if (previous != this.IsHiddenVesselsToggled)
            {
                this.fireOnHiddenVesselsChanged(this);
            }
        }

        private void sortOrderButtons()
        {
            var previous = this.IsAscendingSortOrder;

            var ascPrev = this.IsAscendingSortOrder;
            var descPrev = !this.IsAscendingSortOrder;

            var ascendingButton = GUILayout.Toggle(ascPrev,
                ascendingButtonContent, GUI.skin.button, GUILayout.Width(32f),
                GUILayout.Height(32f));

            var descendingButton = GUILayout.Toggle(descPrev, descendingButtonContent, GUI.skin.button,
                GUILayout.Width(32f), GUILayout.Height(32f));


            var next = previous;
            if (previous && ascPrev == ascendingButton && descendingButton != descPrev)
            {
                next = !previous;
            }
            if (!previous && descPrev == descendingButton && ascendingButton != ascPrev)
            {
                next = !previous;
            }

            this.IsAscendingSortOrder = next;

            if (previous != next)
            {
                this.fireOnSortOrderChanged(this);
            }
        }

        private void targetButton()
        {
            // Disable buttons for current vessel or nothing selected
            if (this.isTargetButtonDisabled())
            {
                GUI.enabled = false;
            }

            // target button
            if (GUILayout.Button(Resources.btnTarg, Resources.buttonTargStyle))
            {
                ITargetable selected;

                if (this.GroupByOrbitingBody)
                {
                    selected = this.groupedScrollerView.SelectedVessel;
                }
                else
                {
                    selected = (ITargetable)this.defaultScrollerView.SelectedVessel ??
                               this.defaultScrollerView.SelectedBody;
                }

                if (selected != null)
                {
                    FlightGlobals.fetch.SetVesselTarget(selected);
                }
            }

            GUI.enabled = true;
        }

        private void flyButton()
        {
            // Disable fly button if we selected a body, have no selection, or selected the current vessel
            if (this.isFlyButtonDisabled())
            {
                GUI.enabled = false;
            }

            // fly button
            if (GUILayout.Button(Resources.btnGoHover, Resources.buttonGoStyle))
            {
                this.fireOnSwitchVessel(this.getSelectedVessel());
            }

            GUI.enabled = true;
        }

        internal void Draw()
        {
            GUILayout.BeginHorizontal();

            this.groupByButton();
            this.nearbyButton();
            this.hiddenVesselsButton();

            GUILayout.FlexibleSpace();

            this.sortOrderButtons();

            GUILayout.FlexibleSpace();

            this.targetButton();
            this.flyButton();

            GUILayout.EndHorizontal();
        }

        private bool isTargetButtonDisabled()
        {
            if (!HSUtils.IsInFlight)
            {
                return true;
            }

            if (this.GroupByOrbitingBody)
            {
                return this.groupedScrollerView.SelectedVessel == null ||
                       this.groupedScrollerView.SelectedVessel == FlightGlobals.ActiveVessel;
            }


            // cannot target current orbiting body
            if (this.defaultScrollerView.SelectedBody != null && FlightGlobals.ActiveVessel.orbit.referenceBody != this.defaultScrollerView.SelectedBody)
            {
                return false;
            }

            return this.defaultScrollerView.SelectedVessel == null ||
                   FlightGlobals.ActiveVessel == this.defaultScrollerView.SelectedVessel;
        }

        private bool isFlyButtonDisabled()
        {
            var vessel = this.GroupByOrbitingBody
                ? this.groupedScrollerView.SelectedVessel
                : this.defaultScrollerView.SelectedVessel;

            return vessel == null || FlightGlobals.ActiveVessel == vessel;
        }

        private Vessel getSelectedVessel()
        {
            return this.GroupByOrbitingBody
                ? this.groupedScrollerView.SelectedVessel
                : this.defaultScrollerView.SelectedVessel;
        }

        internal delegate void OnGroupByChangedHandler(BottomButtons view);

        internal event OnGroupByChangedHandler OnGroupByChanged;

        protected virtual void fireOnGroupByChanged(BottomButtons view)
        {
            var handler = this.OnGroupByChanged;
            if (handler != null) handler(view);
        }

        internal delegate void OnHiddenVesselsChangedHandler(BottomButtons view);

        internal event OnHiddenVesselsChangedHandler OnHiddenVesselsChanged;

        private void fireOnHiddenVesselsChanged(BottomButtons bottomButtons)
        {
            var handler = this.OnHiddenVesselsChanged;
            if (handler != null) handler(bottomButtons);
        }


        internal delegate void OnSwitchVesselHandler(Vessel vessel);

        internal event OnSwitchVesselHandler OnSwitchVessel;

        protected virtual void fireOnSwitchVessel(Vessel vessel)
        {
            var handler = this.OnSwitchVessel;
            if (handler != null) handler(vessel);
        }

        internal delegate void OnNearbyChangedHandler(BottomButtons view);

        internal event OnNearbyChangedHandler OnNearbyChanged;

        protected virtual void fireOnNearbyChanged(BottomButtons view)
        {
            var handler = this.OnNearbyChanged;
            if (handler != null) handler(view);
        }

        internal delegate void OnSortOrderChangedHandler(BottomButtons view);

        internal event OnSortOrderChangedHandler OnSortOrderChanged;

        protected virtual void fireOnSortOrderChanged(BottomButtons view)
        {
            var handler = this.OnSortOrderChanged;
            if (handler != null) handler(view);
        }


        internal void GUISetup(GroupedScrollerView groupedScrollerView, DefaultScrollerView defaultScrollerView)
        {
            this.groupedScrollerView = groupedScrollerView;
            this.defaultScrollerView = defaultScrollerView;
        }
    }
}