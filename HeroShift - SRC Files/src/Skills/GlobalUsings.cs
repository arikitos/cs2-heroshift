// Skill identity is used by every migrated implementation. Keeping the core
// abstraction globally imported prevents generated gameplay files from
// depending on namespace insertion order across the built-in skill files.
global using src.SkillsCore.Abstractions;
global using src.Configuration;
global using src.Configuration.Models;
global using src.Players;
