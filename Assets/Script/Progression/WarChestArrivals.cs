using System;

namespace TreadShred.Progression
{
    /// <summary>
    /// D-047-AC-07: when the free War Chest arrives: a variable ratio, like a slot machine's, so it is never on every
    /// screen and never on a rhythm the player can tune out. Each arrival comes after a random number of result
    /// screens from <see cref="WarChest.ArrivalMinScreens"/> to <see cref="WarChest.ArrivalMaxScreens"/>.
    /// </summary>
    public sealed class WarChestArrivals
    {
        private readonly Func<int, int, int> draw;
        private int seen;
        private int target;

        /// <param name="draw">A random whole number from the first argument to the second, both included.</param>
        public WarChestArrivals(Func<int, int, int> draw)
        {
            this.draw = draw ?? throw new ArgumentNullException(nameof(draw));
            target = Draw();
        }

        /// <summary>A result screen that could show it: true when the free War Chest arrives on this one.</summary>
        public bool ScreenShown()
        {
            if (++seen < target)
                return false;
            seen = 0;
            target = Draw();
            return true;
        }

        private int Draw() => Math.Max(1, draw(WarChest.ArrivalMinScreens, WarChest.ArrivalMaxScreens));
    }
}
