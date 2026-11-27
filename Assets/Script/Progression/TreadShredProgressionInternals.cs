using System.Runtime.CompilerServices;

// The screen tests set internal state directly instead of reaching private fields by reflection (refactor, owner
// approval 2026-11-27).
[assembly: InternalsVisibleTo("TreadShred.Screens.Tests")]
[assembly: InternalsVisibleTo("TreadShred.Progression.Tests")]
