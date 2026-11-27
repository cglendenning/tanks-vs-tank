using System.Runtime.CompilerServices;

// The test assemblies call the game's internal members directly instead of reaching private ones by reflection
// (refactor, owner approval 2026-11-27).
[assembly: InternalsVisibleTo("TreadShred.Screens.Tests")]
[assembly: InternalsVisibleTo("TreadShred.BuildPolicy.Tests")]
[assembly: InternalsVisibleTo("TreadShred.Progression.Tests")]
