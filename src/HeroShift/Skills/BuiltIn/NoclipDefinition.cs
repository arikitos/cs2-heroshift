using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record NoclipOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 30f;
    public float Duration { get; init; } = 2f;
    public float CooldownWhenStuck { get; init; } = 5f;
}

public static class NoclipDefinition
{
    public static SkillDefinition<NoclipOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Noclip,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#44ebd4",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new NoclipOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Noclip.LoadSkill,
            EnableSkill = Noclip.EnableSkill,
            DisableSkill = Noclip.DisableSkill,
            UseSkill = Noclip.UseSkill,
            OnTick = Noclip.OnTick,
            NewRound = Noclip.NewRound,
        },
    };
}
