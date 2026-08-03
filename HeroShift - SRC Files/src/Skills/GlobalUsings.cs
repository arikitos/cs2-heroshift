// Skill identity is used by every migrated implementation. Keeping the core
// abstraction globally imported prevents generated gameplay files from
// depending on namespace insertion order while migration batches are applied.
global using src.SkillsCore.Abstractions;
