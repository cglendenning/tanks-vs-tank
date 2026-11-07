using System;

namespace TreadShred.Save
{
    /// <summary>
    /// D-044: the words of RESET GAME under Settings, and the typed word that unlocks it, so a stray tap can never
    /// erase a game. The erase itself is TreadShredGameReset; the cloud side is CloudSync.Reset (D-025-AC-05).
    /// </summary>
    public static class GameReset
    {
        public const string ConfirmWord = "RESET";
        public const string Title = "RESET GAME";
        public const string Erases =
            "THIS ERASES ALL MISSIONS, MEDALS, COINS, GEMS (BOUGHT GEMS TOO), UPGRADES, WEAPONS AND SETTINGS.";
        public const string Prompt = "TYPE " + ConfirmWord + " TO CONFIRM";
        public const string Erase = "ERASE EVERYTHING";
        public const string Keep = "KEEP MY GAME";

        /// <summary>D-044-AC-01: the platform cloud save goes too, on every device signed in to the account.</summary>
        public static string Reach(bool android) =>
            "IT ALSO ERASES THE SAVE IN YOUR " + (android ? "GOOGLE PLAY GAMES" : "ICLOUD") + " ACCOUNT. IT CANNOT BE UNDONE.";

        /// <summary>D-044-AC-02: the typed word, in any letter case and ignoring surrounding spaces.</summary>
        public static bool Confirms(string typed) =>
            typed != null && string.Equals(typed.Trim(), ConfirmWord, StringComparison.OrdinalIgnoreCase);
    }
}
