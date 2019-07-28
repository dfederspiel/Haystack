using UnityEngine;

namespace HaystackReContinued
{
    /// <summary>
    /// Class to house vessel types along with icons and sort order for the plugin
    /// </summary>
    public class HSVesselType
    {
        public string name; // Type name defined by KSP devs
        public byte sort; // Sort order, lowest first

        // Icon texture, loaded from icons directory. File must be named 'button_vessel_TYPE.png'
        public Texture2D icon;

        public bool visible; // Is this type shown in list

        public HSVesselType(string name, byte sort, Texture2D icon, bool visible)
        {
            this.name = name;
            this.sort = sort;
            this.icon = icon;
            this.visible = visible;
        }
    };
}