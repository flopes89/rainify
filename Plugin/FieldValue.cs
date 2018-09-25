namespace rainify
{
    /// <summary>
    /// Simple value holder class for the field values returned from the SpotifyApi
    /// </summary>
    internal class FieldValue
    {
        /// <summary>
        /// String value
        /// </summary>
        public string StringValue { get; set; } = string.Empty;

        /// <summary>
        /// Double value
        /// </summary>
        public double DoubleValue { get; set; } = 0;
    }
}
