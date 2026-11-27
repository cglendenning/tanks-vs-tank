using System.Runtime.CompilerServices;

// The screen tests read the wallet's state directly instead of by reflection (refactor, owner approval 2026-11-27).
[assembly: InternalsVisibleTo("TreadShred.Screens.Tests")]
