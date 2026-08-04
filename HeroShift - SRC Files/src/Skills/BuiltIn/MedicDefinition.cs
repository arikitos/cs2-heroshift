using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record MedicOptions : ISkillOptions
{
    public int HealthToAdd { get; init; } = 50;
    public int HealthShotLimit { get; init; } = 3;
    public float Cooldown { get; init; } = 1f;
}

public static class MedicDefinition
{
    public static SkillDefinition<MedicOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Medic,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#10c212",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new MedicOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Medic.LoadSkill,
            EnableSkill = Medic.EnableSkill,
            DisableSkill = Medic.DisableSkill,
            UseSkill = Medic.UseSkill,
            OnTick = Medic.OnTick,
            NewRound = Medic.NewRound,
        },
    };
}
