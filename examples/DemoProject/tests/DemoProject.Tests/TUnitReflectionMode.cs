// TUnit's source generator cannot see tests emitted by OTHER source generators
// (Storm Petrel's *TestStormPetrel copies) — Reflection mode enables runtime discovery.
// Kept in a separate file: Storm Petrel copies the whole test file, and a duplicated
// assembly attribute would break compilation (CS0579).
[assembly: TUnit.Core.ReflectionMode]
