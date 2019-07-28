using System;

namespace HaystackReContinued
{
    public class OrbitData
    {
        public string SOI = string.Empty;
        public string AP = string.Empty;
        public string PE = string.Empty;
        public string timeToAP = string.Empty;
        public string timeToPE = string.Empty;
        public string INC = string.Empty;
        public string Period = string.Empty;
        public bool IsSOIChange;
        public string SOIChangeTime = string.Empty;
        public string SOIChangeDate = string.Empty;

        public static OrbitData FromOrbit(Orbit orbit)
        {
            return new OrbitData
            {
                SOI = orbit.referenceBody.bodyName,
                AP = Converters.Distance(Math.Max(0, orbit.ApA)),
                PE = Converters.Distance(Math.Max(0, orbit.PeA)),
                timeToAP = Converters.Duration(Math.Max(0, orbit.timeToAp)),
                timeToPE = Converters.Duration(Math.Max(0, orbit.timeToPe)),
                INC = orbit.inclination.ToString("F3") + "°",
                Period = Converters.Duration(Math.Max(0, orbit.period), 4),
                IsSOIChange = orbit.patchEndTransition == Orbit.PatchTransitionType.ESCAPE || orbit.patchEndTransition == Orbit.PatchTransitionType.ENCOUNTER,
                SOIChangeTime = Converters.Duration(orbit.UTsoi - Planetarium.GetUniversalTime()),
                SOIChangeDate = KSPUtil.PrintDateCompact(orbit.UTsoi, true, true)

            };
        }
    }
}