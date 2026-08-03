using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record SoundMakerOptions : ISkillOptions
{
    public int Cooldown { get; init; } = 2;
}

public static class SoundMakerDefinition
{
    public static SkillDefinition<SoundMakerOptions> Create() => new()
    {
        Id = BuiltInSkillIds.SoundMaker,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#e3ed8c",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new SoundMakerOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = SoundMaker.LoadSkill,
            EnableSkill = SoundMaker.EnableSkill,
            DisableSkill = SoundMaker.DisableSkill,
            OnTick = SoundMaker.OnTick,
            NewRound = SoundMaker.NewRound,
            PlayerMakeSound = SoundMaker.PlayerMakeSound,
            PlayerDeath = SoundMaker.PlayerDeath,
        },
    };
}
