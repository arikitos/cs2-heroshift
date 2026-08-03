// https://discord.com/channels/1160907911501991946/1508172390863994910/1508180670659166348

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace HeroShift.src.utils
{

    /*
     * CServerSideClient and friends - raw engine access used for one thing only:
     * forcing a full network update to a client.
     *
     * Why the plugin needs it: heroes that change what a player is allowed to SEE
     * (invisibility, wallhack, hiding a carried weapon, C4 camouflage) work by
     * filtering entities in CheckTransmit. CS2 sends clients delta snapshots, so a
     * client that already received an entity will not re-receive it just because the
     * filtering changed - the visual stays stale. Setting a client's DeltaTick to -1
     * invalidates its baseline and makes the server send a full snapshot, which
     * applies the new visibility immediately. That is exactly what ForceFullUpdate()
     * does, and SkillUtils.ForceFullUpdate/ForceFullUpdateToAll are the wrappers
     * heroes actually call - none of this file should be used directly from a hero.
     *
     * None of these types are exposed by CounterStrikeSharp, so they are recreated
     * here by hand:
     *   - CUtlMemory<T>/CUtlVector<T> mirror Valve's container layout so the client
     *     slot array can be indexed directly in unmanaged memory.
     *   - INetworkServerService is obtained from the "NetworkServerService_001"
     *     Valve interface, and its GetIGameServer is called through a vtable index.
     *   - All three magic numbers come from gamedata/HeroShift.gamedata.json:
     *     "INetworkServerService_GetIGameServer" is a VTABLE INDEX (and differs
     *     between Windows and Linux, as vtable layouts do), while
     *     "INetworkGameServer_Slots" and "CServerSideClient_m_nDeltaTick" are BYTE
     *     OFFSETS added to a base pointer.
     *
     * Because these are hardcoded offsets into engine memory, a CS2 update can
     * invalidate them. Symptoms are the wrong field being read or a crash rather
     * than a clean error, which is why the accessors below null-check handles and
     * bounds-check the slot index. The source of these offsets is credited in the
     * Discord link at the top of the file.
     */

    #region Native Structs

    // Valve's CUtlMemory: a raw buffer plus its allocation bookkeeping. Layout must stay
    // sequential and field order must match the engine exactly.
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CUtlMemory<T> where T : unmanaged
    {
        public T* m_pMemory;
        public int m_nAllocationCount;
        public int m_nGrowSize;
    }

    // Valve's CUtlVector: element count followed by the memory block. Note the size field
    // comes FIRST here, which is why it cannot be modelled with a normal List.
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CUtlVector<T> where T : unmanaged
    {
        public int m_Size;
        public CUtlMemory<T> m_Memory;

        public int Count => m_Size;

        // Returns the element by reference so it can be written in place.
        // The null check on m_pMemory is the practical signal that a gamedata offset has
        // gone stale after a CS2 update - see the exception message.
        public ref T Element(int index)
        {
            if (index < 0 || index >= m_Size)
                throw new IndexOutOfRangeException();

            if (m_Memory.m_pMemory == null)
                throw new NullReferenceException("m_pMemory is null. The vector is empty or the GameData offset is outdated.");

            return ref m_Memory.m_pMemory[index];
        }
    }

    #endregion

    #region Network Services

    // Wrapper over the engine's NetworkServerService. The only reason it exists is to
    // reach the game server object, which owns the per-client slot array.
    public class INetworkServerService : NativeObject
    {
        private readonly VirtualFunctionWithReturn<nint, nint> GetIGameServerFunc;

        // Resolves the Valve interface by name and binds GetIGameServer by vtable index
        // (the "offset" from gamedata here is a vtable slot, not a byte offset).
        public INetworkServerService() : base(NativeAPI.GetValveInterface(0, "NetworkServerService_001"))
        {
            GetIGameServerFunc = new VirtualFunctionWithReturn<nint, nint>(Handle, GameData.GetOffset("INetworkServerService_GetIGameServer"));
        }

        public INetworkGameServer GetIGameServer()
        {
            return new INetworkGameServer(GetIGameServerFunc.Invoke(Handle));
        }
    }

    // The running game server; owns the array of connected client objects.
    public unsafe class INetworkGameServer(nint ptr) : NativeObject(ptr)
    {
        // Byte offset from the server object to its client-slot CUtlVector.
        private static readonly int SlotsOffset = GameData.GetOffset("INetworkGameServer_Slots");

        // Reinterprets that memory as a CUtlVector of client pointers, by reference so no
        // copy is made. This is a live view of engine memory, not a snapshot.
        private ref CUtlVector<nint> Slots => ref Unsafe.AsRef<CUtlVector<nint>>((void*)(Handle + SlotsOffset));

        // Looks up a client by its player slot (CCSPlayerController.Slot). Returns null for
        // an out-of-range slot or an empty entry, so the caller must handle null - a slot
        // can be unoccupied even while other players are connected.
        public CServerSideClient? GetClientBySlot(int slot)
        {
            if (Handle == nint.Zero)
                return null;

            if (slot < 0 || slot >= Slots.Count || Slots.m_Memory.m_pMemory == null)
                return null;

            var ptr = Slots.Element(slot);

            if (ptr == nint.Zero)
                return null;

            return new CServerSideClient(ptr);
        }
    }

    #endregion

    #region CServerSideClient

    // One connected client, as the engine sees it.
    public unsafe class CServerSideClient(nint ptr) : NativeObject(ptr)
    {
        private static readonly int m_nDeltaTick = GameData.GetOffset("CServerSideClient_m_nDeltaTick");

        // Generic read/write of an unmanaged field at a byte offset from this object.
        private ref T Field<T>(int offset) where T : unmanaged
        {
            return ref Unsafe.AsRef<T>((void*)(Handle + offset));
        }

        // The tick of the snapshot this client is being delta-compressed against.
        // Writable, which is the whole point of this file.
        public int DeltaTick
        {
            get => Field<int>(m_nDeltaTick);
            set => Field<int>(m_nDeltaTick) = value;
        }

        // Setting DeltaTick to -1 tells the engine there is no valid baseline, so the next
        // snapshot sent to this client is a full one. Used after changing entity visibility
        // (CheckTransmit filtering) so the client stops rendering what it should no longer
        // see.
        //
        // Do not call this directly from a hero - use SkillUtils.ForceFullUpdate /
        // ForceFullUpdateToAll. Those wrappers add the parts that make it safe: they honour
        // Config.EnableFullForceUpdate, skip bots, collapse multiple requests in the same
        // tick, and re-apply each player's view angles a few ticks later, because a full
        // update also resets the client's view angles.
        public void ForceFullUpdate()
        {
            if (Handle == nint.Zero) return;
            DeltaTick = -1;
        }
    }

    #endregion
}