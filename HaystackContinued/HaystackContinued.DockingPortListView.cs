using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HaystackReContinued
{
    public class DockingPortListView
    {
        private Vessel currentVessel;
        private readonly List<PortInfo> portList = new List<PortInfo>();
        private readonly HaystackContinued haystackContinued;
        private bool runUpdate;

        private static readonly Type moduleDockingNodeNamedType;
        private static readonly FieldInfo modulePortName;
        private static bool namedDockingPortSupport;

        static DockingPortListView()
        {
            try
            {
                Type result = null;
                AssemblyLoader.loadedAssemblies.TypeOperation(t =>
                {
                    if (t.FullName == "NavyFish.ModuleDockingNodeNamed")
                    {
                        result = t;
                    }
                });

                moduleDockingNodeNamedType = result;

                modulePortName = moduleDockingNodeNamedType.GetField("portName",
                    BindingFlags.Instance | BindingFlags.Public);
            }
            catch (Exception e)
            {
                moduleDockingNodeNamedType = null;
                modulePortName = null;
                HSUtils.DebugLog("exception getting docking port alignment indicator type");
                HSUtils.DebugLog("{0}", e.Message);
            }

            if (moduleDockingNodeNamedType != null && modulePortName != null)
            {
                namedDockingPortSupport = true;

                HSUtils.Log("Docking Port Alignment Indicator mod detected: using named docking node support.");
                HSUtils.DebugLog("{0} {1}", moduleDockingNodeNamedType.FullName,
                    moduleDockingNodeNamedType.AssemblyQualifiedName);
            }
            else
            {
                HSUtils.DebugLog("Docking Port Alignment Indicator mod was not detected");
            }
        }

        internal DockingPortListView(HaystackContinued haystackContinued)
        {
            this.haystackContinued = haystackContinued;
        }

        internal Vessel CurrentVessel
        {
            get { return this.currentVessel; }
            set
            {
                this.currentVessel = value;
                this.handleVesselChange();
            }
        }

        private void handleVesselChange()
        {
            if (this.currentVessel == null)
            {
                this.portList.Clear();
                this.runUpdate = false;
                return;
            }

            this.runUpdate = true;
            this.haystackContinued.StartCoroutine(this.updatePortListCoroutine());
        }

        public IEnumerator updatePortListCoroutine()
        {
            while (this.runUpdate)
            {
                yield return new WaitForEndOfFrame();

                this.populatePortList();

                yield return new WaitForSeconds(30f);
            }
        }

        public void populatePortList()
        {
            this.portList.Clear();

            var targetables = this.currentVessel.FindPartModulesImplementing<ITargetable>();
            foreach (var targetable in targetables)
            {
                var port = targetable as ModuleDockingNode;
                if (port == null)
                {
                    continue;
                }

                if (port.state.StartsWith("Docked", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // don't display docking ports that have all their attach nodes used.
                var usedNodeCount = port.part.attachNodes.Count(node => node.attachedPart != null);
                if (usedNodeCount == port.part.attachNodes.Count)
                {
                    continue;
                }

                var info = new PortInfo
                {
                    Name = getPortName(port),
                    PortNode = port,
                };

                this.portList.Add(info);
            }

            portList.Sort((a, b) => a.Name.CompareTo(b.Name));
        }

        private string getPortName(ModuleDockingNode port)
        {
            HSUtils.DebugLog("DockingPortListView#getPortName: start");

            if (!namedDockingPortSupport)
            {
                return port.part.partInfo.title.Trim();
            }

            PartModule found = null;
            for (int i = 0; i < port.part.Modules.Count; i++)
            {
                var module = port.part.Modules[i];
                if (module.GetType() == moduleDockingNodeNamedType)
                {
                    found = module;
                    break;
                }
            }

            if (found == null)
            {
                HSUtils.DebugLog(
                    "DockingPortListView#getPortName: named docking port support enabled but could not find the part module");
                return port.part.partInfo.title;
            }

            var portName = (string)modulePortName.GetValue(found);

            HSUtils.DebugLog("DockingPortListView#getPortName: found name: {0}", portName);

            return portName;
        }

        internal void Draw(Vessel vessel)
        {
            if (this.CurrentVessel == null || vessel != this.CurrentVessel)
            {
                return;
            }

            if (!this.CurrentVessel.loaded)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("The vessel is out of range: cannot list docking ports",
                    Resources.textSituationStyle);
                GUILayout.EndVertical();
                return;
            }

            if (this.portList.IsEmpty())
            {
                GUILayout.BeginVertical();
                GUILayout.Label("This vessel does not have any docking ports", Resources.textSituationStyle);
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginVertical();

            GUILayout.Label("Docking Ports", Resources.textDockingPortHeaderStyle);

            foreach (var i in portList)
            {
                GUILayout.Box((Texture)null, Resources.hrSepLineStyle, GUILayout.ExpandWidth(true),
                    GUILayout.ExpandHeight(false));

                GUILayout.BeginHorizontal();

                GUILayout.Label(i.Name, Resources.textDockingPortStyle, GUILayout.ExpandHeight(false));
                GUILayout.FlexibleSpace();

                if (FlightGlobals.ActiveVessel != this.currentVessel)
                {
                    var distance = this.getDistanceText(i.PortNode);
                    GUILayout.Label(distance, Resources.textDockingPortDistanceStyle, GUILayout.ExpandHeight(true));
                    GUILayout.Space(10f);
                    if (GUILayout.Button(Resources.btnTargetAlpha, Resources.buttonDockingPortTarget,
                        GUILayout.Width(18f),
                        GUILayout.Height(18f)))
                    {
                        setDockingPortTarget(i.PortNode);
                    }

                    GUILayout.Space(10f);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        private void setDockingPortTarget(ModuleDockingNode portNode)
        {
            var vessel = portNode.GetVessel();

            //can't set target if the vessel is not loaded or is the active vessel
            if (!vessel.loaded || vessel.isActiveVessel)
            {
                return;
            }

            FlightGlobals.fetch.SetVesselTarget(portNode);
        }

        private string getDistanceText(ModuleDockingNode port)
        {
            var activeVessel = FlightGlobals.ActiveVessel;
            var distance = Vector3.Distance(activeVessel.transform.position, port.GetTransform().position);

            return string.Format("{0}m", HSUtils.ToSI(distance));
        }

        private struct PortInfo
        {
            internal string Name;
            internal ModuleDockingNode PortNode;
        }
    }
}