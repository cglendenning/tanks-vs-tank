using System.Globalization;

namespace TreadShred.Economy
{
    /// <summary>D-028-AC-04: every count the player reads uses comma thousands separators (1,200 GEMS).</summary>
    public static class CountText
    {
        public static string Format(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
    }
}
