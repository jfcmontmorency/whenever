namespace JFCartier.Whenever
{
    /// <summary>
    /// Whether a zone runs once for everything inside it, or once for each object that enters.
    /// </summary>
    public enum TriggerFor
    {
        /// <summary>The zone as a whole: one run, however many objects are inside.</summary>
        Zone,

        /// <summary>Each object that enters gets its own run (own Hold Time, repeats and exit).</summary>
        EachObject,
    }
}
