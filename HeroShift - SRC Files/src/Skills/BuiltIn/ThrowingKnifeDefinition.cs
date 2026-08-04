using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ThrowingKnifeOptions : ISkillOptions
{
    public bool FriendlyFire { get; init; } = false;
    public int Damage { get; init; } = 9999;
}

public static class ThrowingKnifeDefinition
{
    public static SkillDefinition<ThrowingKnifeOptions> Create() => new()
    {
        Id = BuiltInSkillIds.ThrowingKnife,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#8f108f",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ThrowingKnifeOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = ThrowingKnife.LoadSkill,
            EnableSkill = ThrowingKnife.EnableSkill,
            DisableSkill = ThrowingKnife.DisableSkill,
            UseSkill = ThrowingKnife.UseSkill,
            CheckTransmit = ThrowingKnife.CheckTransmit,
            NewRound = ThrowingKnife.NewRound,
            RoundEnd = ThrowingKnife.RoundEnd,
            PlayerMakeSound = ThrowingKnife.PlayerMakeSound,
            OnTriggerEnter = ThrowingKnife.OnTriggerEnter,
            OnWeaponCanAcquire = ThrowingKnife.OnWeaponCanAcquire,
            PlayerDisconnect = ThrowingKnife.PlayerDisconnect,
        },
    };
}
