using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record WatchmakerOptions : ISkillOptions
{
    public int ChangeRoundTime { get; init; } = 7;
    public string SoundEvent { get; init; } = "UIPanorama.sidemenu_select";
}

public static class WatchmakerDefinition
{
    public static SkillDefinition<WatchmakerOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Watchmaker,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ff462e",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new WatchmakerOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Watchmaker.LoadSkill,
            OnEntitySpawned = Watchmaker.OnEntitySpawned,
            NewRound = Watchmaker.NewRound,
            BombPlanted = Watchmaker.BombPlanted,
        },
    };
}
