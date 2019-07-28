namespace HaystackReContinued
{
            public class DisplayItem
            {
                public string Label { get; private set; }
                public string Value { get; private set; }

                public static DisplayItem Create(string label, string value)
                {
                    return new DisplayItem
                    {
                        Label = label,
                        Value = value,
                    };
                }
            }
        }
    