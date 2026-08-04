using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record PhoenixOptions : ISkillOptions
{
    public float ChanceFrom { get; init; } = .2f;
    public float ChanceTo { get; init; } = .4f;
}

public static class PhoenixDefinition
{
    public static SkillDefinition<PhoenixOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Phoenix,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ff5C0A",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new PhoenixOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Phoenix.LoadSkill,
            EnableSkill = Phoenix.EnableSkill,
            DisableSkill = Phoenix.DisableSkill,
            OnTakeDamage = Phoenix.OnTakeDamage,
            NewRound = Phoenix.NewRound,
            PlayerDisconnect = Phoenix.PlayerDisconnect,
        },
    };
}
