// SPDX-License-Identifier: GPL-3.0-only
using Xunit;

// Arch 2.1.0 lazily registers component types in a process-wide, non-thread-safe
// registry. Concurrent cold simulations can corrupt that registry. Keep test
// worlds on one test-collection lane; separate test processes remain independent.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
