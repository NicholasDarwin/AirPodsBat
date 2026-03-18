namespace AirPodBat.Models
{
    /// <summary>
    /// Represents the battery levels for AirPods components.
    /// Values are percentages (0-100) or -1 if unavailable.
    /// </summary>
    public class AirPodsBattery
    {
        /// <summary>
        /// Battery level of the left earbud (0-100 or -1 if unavailable)
        /// </summary>
        public int Left { get; set; }

        /// <summary>
        /// Battery level of the right earbud (0-100 or -1 if unavailable)
        /// </summary>
        public int Right { get; set; }

        /// <summary>
        /// Battery level of the charging case (0-100 or -1 if unavailable)
        /// </summary>
        public int Case { get; set; }

        public AirPodsBattery()
        {
            Left = -1;
            Right = -1;
            Case = -1;
        }

        /// <summary>
        /// Determines if all battery values are valid (not -1)
        /// </summary>
        public bool HasValidData => Left >= 0 && Right >= 0 && Case >= 0;

        public override string ToString()
        {
            return $"L:{Left}% R:{Right}% C:{Case}%";
        }
    }
}
