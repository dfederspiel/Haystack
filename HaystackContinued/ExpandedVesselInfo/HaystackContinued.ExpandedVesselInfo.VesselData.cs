using System;
using System.Collections.Generic;

namespace HaystackReContinued
{
    public class VesselData
    {
        public Guid Id;
        public OrbitData OrbitData = new OrbitData();
        public List<string> Resources = new List<string>();
        public CrewData CrewData = new CrewData();
        public string MET = string.Empty;
        public bool HasNextNode;
        public string NextNodeIn = string.Empty;
        public string NextNodeTime = string.Empty;
        public string Situation = string.Empty;

        public bool HasCrew { get { return CrewData.TotalCrew > 0; } }
        public bool HasResources { get { return this.Resources.Count > 0; } }
    }
}
