using System;
using System.Collections.Generic;

namespace TreadShred.Progression
{
    /// <summary>
    /// D-014-AC-03: the system Back closes the panel or dialog opened last, exactly as its own BACK,
    /// CANCEL or NO THANKS button does; with nothing open it does nothing.
    /// </summary>
    public sealed class BackStack
    {
        private readonly List<KeyValuePair<object, Action>> open = new List<KeyValuePair<object, Action>>();

        public int Count => open.Count;

        public void Opened(object panel, Action close)
        {
            if (panel == null || close == null)
                throw new ArgumentNullException(panel == null ? nameof(panel) : nameof(close));
            Closed(panel);
            open.Add(new KeyValuePair<object, Action>(panel, close));
        }

        public void Closed(object panel)
        {
            open.RemoveAll(entry => ReferenceEquals(entry.Key, panel));
        }

        /// <summary>Closes the panel opened last; false when nothing is open.</summary>
        public bool Back()
        {
            if (open.Count == 0)
                return false;
            var top = open[open.Count - 1];
            open.RemoveAt(open.Count - 1);
            top.Value();
            return true;
        }
    }
}
