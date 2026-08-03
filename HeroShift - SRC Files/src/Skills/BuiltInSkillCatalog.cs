using src.SkillsCore.Abstractions;
using src.SkillsCore.BuiltIn;

namespace src.SkillsCore;

/*
 * BuiltInSkillCatalog - registers every migrated skill's typed
 * SkillDefinition into a SkillRegistry (REFACTOR.md section 6/10).
 *
 * Grows by one Create() call per skill as batches migrate (REFACTOR.md
 * section 23); skills not yet migrated simply have no entry here and keep
 * running exclusively through the legacy reflection dispatch until their
 * batch lands. Not yet wired into plugin load - the legacy dispatch is the
 * only one actually driving gameplay until the runtime-state migration
 * commit (13) and the legacy-removal commit (14).
 */
public static class BuiltInSkillCatalog
{
    public static SkillRegistry BuildRegistry()
    {
        var registry = new SkillRegistry();

        registry.Register(AntyFlashDefinition.Create());
        registry.Register(AstronautDefinition.Create());
        registry.Register(BehindDefinition.Create());
        registry.Register(DashDefinition.Create());
        registry.Register(DraculaDefinition.Create());
        registry.Register(DwarfDefinition.Create());
        registry.Register(FastReloadDefinition.Create());
        registry.Register(IlliterateDefinition.Create());
        registry.Register(PushDefinition.Create());
        registry.Register(RobinHoodDefinition.Create());

        return registry;
    }
}
