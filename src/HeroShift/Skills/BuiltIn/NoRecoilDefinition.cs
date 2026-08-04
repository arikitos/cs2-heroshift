using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record NoRecoilOptions : ISkillOptions
{
}

public static class NoRecoilDefinition
{
    public static SkillDefinition<NoRecoilOptions> Create() => new()
    {
        Id = BuiltInSkillIds.NoRecoil,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#8a42f5",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new NoRecoilOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = NoRecoil.LoadSkill,
            EnableSkill = NoRecoil.EnableSkill,
            DisableSkill = NoRecoil.DisableSkill,
            OnTick = NoRecoil.OnTick,
            NewRound = NoRecoil.NewRound,
        },
    };
}
