// Test fixtures may run in parallel (workers set in booker.runsettings).
// Safe because every test creates its own data and gets its own browser context or API context.
[assembly: Parallelizable(ParallelScope.Fixtures)]
