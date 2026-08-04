using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using HeroShift.src.utils;

namespace src.Infrastructure.Tracing;

/// <summary>
/// Boundary around the externally installed RayTrace capability.
/// </summary>
public interface ITraceService
{
    bool IsAvailable { get; }

    CustomTraceResult? TraceShape(
        CCSPlayerController player,
        Vector startPos,
        Vector endPos,
        ulong? mask = null,
        ulong? contents = null);

    CustomTraceResult? EyeTrace(CCSPlayerController player);

    CustomTraceResult? TraceHullShape(
        Vector startPos,
        Vector endPos,
        CCSPlayerController player,
        Vector? mins = null,
        Vector? maxs = null,
        ulong? mask = null,
        ulong? contents = null,
        QAngle? angle = null);
}
