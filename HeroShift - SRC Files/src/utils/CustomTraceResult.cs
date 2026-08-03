using RayTraceAPI;
using System.Numerics;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace HeroShift.src.utils
{
    /*
     * CustomTraceResult - what a RayTrace call gives back to a hero.
     *
     * It is a copy of the RayTrace module's native TraceResult, plus the inputs that
     * produced it (the start position and the masks). The copy matters: the native
     * TraceResult does not carry the start position, so without keeping it here the
     * distance travelled could not be computed afterwards. It is a struct, so it is
     * a snapshot - reading it later never touches native memory again.
     *
     * Positions and the normal are stored as loose X/Y/Z floats because that is the
     * native struct's layout; the Vector3 properties below just recombine them for
     * convenience.
     *
     * How to read a result:
     *   DidHit     - true when the ray was stopped by something. Derived from
     *                Fraction < 1, i.e. the ray did not travel its whole length.
     *   Fraction   - 0..1, how far along the ray the hit occurred. Useful for
     *                "how close was it?" without a distance calculation.
     *   EndPos     - where the ray stopped: the impact point on a hit, or the
     *                original end point on a miss.
     *   Normal     - the surface normal at the impact point, e.g. for placing a
     *                decal/entity flat against a wall or reflecting a direction.
     *   HitEntity  - raw handle of whatever was hit. Do not use it directly; pass
     *                the result to the Hit* helpers in RayTrace (HitPlayer,
     *                HitWorld, ...) which type-check the DesignerName for you.
     *   Distance   - units from StartPos to EndPos.
     *   IsAllSolid - the trace started inside solid geometry. In that case the hit
     *                data is not meaningful, so check this before trusting EndPos
     *                or Normal (a common cause is tracing from inside a wall or a
     *                player who is clipped into geometry).
     *
     * Remember that RayTrace returns a NULLABLE CustomTraceResult: null means the
     * trace could not run at all, which is different from a result with
     * DidHit == false.
     */
    public struct CustomTraceResult(TraceResult result, Vector startPos, ulong mask, ulong contents, bool drawBeam)
    {
        public float StartPosX = startPos.X;
        public float StartPosY = startPos.Y;
        public float StartPosZ = startPos.Z;

        public float EndPosX = result.EndPosX;
        public float EndPosY = result.EndPosY;
        public float EndPosZ = result.EndPosZ;

        public nint HitEntity = result.HitEntity;
        public float Fraction = result.Fraction;
        public int AllSolid = result.AllSolid;

        public float NormalX = result.NormalX;
        public float NormalY = result.NormalY;
        public float NormalZ = result.NormalZ;

        // The mask/exclude actually used for this trace, kept for debugging and so a
        // follow-up trace can reuse the same layers.
        public ulong InteractsWith = mask;
        public ulong InteractsExclude = contents;
        public bool DrawBeam = drawBeam;

        public readonly Vector3 StartPos => new(StartPosX, StartPosY, StartPosZ);
        public readonly Vector3 EndPos => new(EndPosX, EndPosY, EndPosZ);
        public readonly Vector3 Normal => new(NormalX, NormalY, NormalZ);
        public readonly float Distance => Vector3.Distance(StartPos, EndPos);
        public readonly bool DidHit => Fraction < 1f;
        public readonly bool IsAllSolid => AllSolid != 0;
    }
}
