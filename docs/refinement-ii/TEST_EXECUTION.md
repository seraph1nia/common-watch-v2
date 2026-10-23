# Simulation test execution

Core, Shared and session test assemblies run their xUnit collections serially.
Each process still uses the normal pinned simulation and runtime; no tests are
filtered, costs changed or retry semantics replaced.

The pinned Arch 2.1.0 package identifies upstream commit
`04d52e7268eb6f376ca4841a0c204334120c5e9d`. Its
[ComponentRegistry.Add implementation](https://github.com/genaray/Arch/blob/04d52e7268eb6f376ca4841a0c204334120c5e9d/src/Arch/Core/ComponentRegistry.cs#L121)
updates a process-wide dictionary, array and size without a lock. Generic component
initializers can register different types concurrently when separate test
collections start cold simulations. Arch's package XML also explicitly describes
registry mutation as not thread-safe. The application ordinarily owns its
simulation on one application thread; the test harness must respect cold registry
initialization as well.

During #16 verification, one unchanged-source Shared run aborted its test host
after 75 completed tests. A subsequent host consumed CPU without completing and
did not answer a bounded managed-stack request; native debugger attachment was
unavailable under the existing ptrace policy. That host was deliberately
terminated and confirmed gone. A replacement with an explicit 180-second
execution bound also timed out and was confirmed gone. After serializing test
collections, all 145 Shared tests passed in 45 seconds and all 46 Core tests passed.
Raw transcripts are retained under `artifacts/refinement-ii/r1/logs/`.

This removes a demonstrated unsafe test setup condition. It does not establish a
native stack-level cause for every historical fault, nor claim to fix the separate
Godot allocator abort or the previously documented intermittent native .NET/JIT
risk. Keep those qualification limits and report subsequent failures honestly.
