using src.SkillsCore.Abstractions;
using src.SkillsCore.BuiltIn;

namespace src.SkillsCore;

/*
 * BuiltInSkillCatalog - registers every migrated skill's typed definition in
 * stable legacy skill order. The catalog remains additive until the live
 * runtime is switched from reflection to SkillDispatcher.
 */
public static class BuiltInSkillCatalog
{
    public static SkillRegistry BuildRegistry()
    {
        var registry = new SkillRegistry();

        registry.Register(NoneDefinition.Create());
        registry.Register(AntyFlashDefinition.Create());
        registry.Register(AstronautDefinition.Create());
        registry.Register(BehindDefinition.Create());
        registry.Register(CatapultDefinition.Create());
        registry.Register(DisarmamentDefinition.Create());
        registry.Register(DashDefinition.Create());
        registry.Register(DraculaDefinition.Create());
        registry.Register(DwarfDefinition.Create());
        registry.Register(FastReloadDefinition.Create());
        registry.Register(FragileBombDefinition.Create());
        registry.Register(GrenadierDefinition.Create());
        registry.Register(IlliterateDefinition.Create());
        registry.Register(ImpostorDefinition.Create());
        registry.Register(InfiniteAmmoDefinition.Create());
        registry.Register(JumpingJackDefinition.Create());
        registry.Register(KnockbackDefinition.Create());
        registry.Register(PushDefinition.Create());
        registry.Register(PyroDefinition.Create());
        registry.Register(RamboDefinition.Create());
        registry.Register(ReturnToSenderDefinition.Create());
        registry.Register(RichBoyDefinition.Create());
        registry.Register(RobinHoodDefinition.Create());
        registry.Register(SaperDefinition.Create());
        registry.Register(ShortBombDefinition.Create());
        registry.Register(SilentDefinition.Create());
        registry.Register(TeleporterDefinition.Create());
        registry.Register(ZeusDefinition.Create());

        return registry;
    }
}
