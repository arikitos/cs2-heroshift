using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record SpectatorOptions : ISkillOptions
{
    public float Distance { get; init; } = 100f;
    public float UseCooldown { get; init; } = .5f;
}

public static class SpectatorDefinition
{
    public static SkillDefinition<SpectatorOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Spectator,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#42f5da",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new SpectatorOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Spectator.LoadSkill,
            DisableSkill = Spectator.DisableSkill,
            UseSkill = Spectator.UseSkill,
            OnTick = Spectator.OnTick,
            NewRound = Spectator.NewRound,
            WeaponPickup = Spectator.WeaponPickup,
            PlayerDisconnect = Spectator.PlayerDisconnect,
        },
    };
}
