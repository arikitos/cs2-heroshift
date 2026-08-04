using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * ReturnToSenderOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record ReturnToSenderOptions : ISkillOptions
{
}

/*
 * ReturnToSenderDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing ReturnToSender gameplay implementation.
 */
public static class ReturnToSenderDefinition
{
    public static SkillDefinition<ReturnToSenderOptions> Create() => new()
    {
        Id = BuiltInSkillIds.ReturnToSender,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#a68132",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ReturnToSenderOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = ReturnToSender.LoadSkill,
            DisableSkill = ReturnToSender.DisableSkill,
            NewRound = ReturnToSender.NewRound,
            PlayerHurt = ReturnToSender.PlayerHurt,
        },
    };
}
