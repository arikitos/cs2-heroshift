using System.Collections.Concurrent;
using src.SkillsCore.Abstractions;

namespace src.Players;

public sealed class PlayerStateStore
{
    private readonly ConcurrentDictionary<uint, PlayerRuntimeState> _players = [];

    public IEnumerable<PlayerRuntimeState> All => _players.Values;

    public void Register(PlayerRuntimeState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _players[state.PlayerIndex] = state;
    }

    public bool Remove(uint playerIndex) => _players.TryRemove(playerIndex, out _);

    public PlayerRuntimeState? Get(uint? playerIndex)
    {
        if (playerIndex == null)
            return null;
        return _players.TryGetValue(playerIndex.Value, out var state) ? state : null;
    }

    public int CountBySkill(SkillId skill) => _players.Values.Count(player => player.Skill == skill);

    public void Clear() => _players.Clear();
}
