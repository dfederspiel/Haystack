using System.Collections.Generic;

namespace HaystackReContinued
{
    public class BodyData
    {
        public int Id;
        public OrbitData OrbitData = new OrbitData();
        public List<DisplayItem> PhysicalData = new List<DisplayItem>();
        public List<DisplayItem> AtmData = new List<DisplayItem>();
        public List<DisplayItem> SciData = new List<DisplayItem>();
        public List<DisplayItem> Satellites = new List<DisplayItem>();
    }
}