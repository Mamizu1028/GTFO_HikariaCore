namespace Hikaria.Core.SNetworkExt;

internal sealed class SNetExt_ReplicatorRegistry
{
    public enum RegisterStatus { Success, Conflict, InvalidKeyHash }

    private readonly Dictionary<SNetExt_KeyHash16, ISNetExt_Replicator> _byKeyHash = new(64);
    private readonly Dictionary<string, SNetExt_KeyHash16> _keyToKeyHash = new(64);
    private readonly Dictionary<IntPtr, SNetExt_Replicator_VanillaWrapper> _byVanillaPtr = new(64);
    private readonly List<ISNetExt_Replicator> _assigned = new();

    public IReadOnlyList<ISNetExt_Replicator> Assigned => _assigned;

    public RegisterStatus Register(ISNetExt_MutableReplicator r)
    {
        if (!r.HasValidKeyHash)
            return RegisterStatus.InvalidKeyHash;
        var k = SNetExt_KeyHash16.FromHex(r.KeyHash);
        if (k.IsEmpty)
            return RegisterStatus.InvalidKeyHash;
        if (_byKeyHash.TryGetValue(k, out var existing) && existing != null)
            return RegisterStatus.Conflict;

        _byKeyHash[k] = r;
        if (!r.IsAnonymous)
            _keyToKeyHash[r.Key] = k;
        if (!_assigned.Contains(r))
            _assigned.Add(r);
        return RegisterStatus.Success;
    }

    public bool Unregister(ISNetExt_Replicator r)
    {
        if (r == null) return false;
        var k = SNetExt_KeyHash16.FromHex(r.KeyHash);
        if (k.IsEmpty || !_byKeyHash.TryGetValue(k, out var existing) || existing != r)
        {
            _assigned.Remove(r);
            return false;
        }
        _byKeyHash.Remove(k);
        if (!r.IsAnonymous)
            _keyToKeyHash.Remove(r.Key);
        _assigned.Remove(r);
        return true;
    }

    public RegisterStatus Reassign(ISNetExt_MutableReplicator r, in SNetExt_KeyHash16 newHash)
    {
        if (newHash.IsEmpty)
            return RegisterStatus.InvalidKeyHash;
        if (_byKeyHash.TryGetValue(newHash, out var occupant) && occupant != null && occupant != r)
            return RegisterStatus.Conflict;

        Unregister(r);
        r.AssignKey(string.Empty);
        r.AssignKeyHash(newHash);
        return Register(r);
    }

    public bool TryGet(in SNetExt_KeyHash16 keyHash, out ISNetExt_Replicator r)
    {
        return _byKeyHash.TryGetValue(keyHash, out r) && r != null;
    }

    public bool TryGetByKey(string key, out ISNetExt_Replicator r)
    {
        r = null;
        return !string.IsNullOrWhiteSpace(key)
            && _keyToKeyHash.TryGetValue(key, out var k)
            && _byKeyHash.TryGetValue(k, out r)
            && r != null;
    }

    public bool TryGetByVanilla(IntPtr ptr, out SNetExt_Replicator_VanillaWrapper w)
    {
        return _byVanillaPtr.TryGetValue(ptr, out w) && w != null;
    }

    public RegisterStatus RegisterVanilla(SNetExt_Replicator_VanillaWrapper w)
    {
        var status = Register(w);
        if (status == RegisterStatus.Success)
            _byVanillaPtr[w.Vanilla.Pointer] = w;
        return status;
    }

    public bool UnregisterVanilla(SNetExt_Replicator_VanillaWrapper w)
    {
        if (!Unregister(w)) return false;
        _byVanillaPtr.Remove(w.Vanilla.Pointer);
        return true;
    }

    public void Clear()
    {
        _byKeyHash.Clear();
        _keyToKeyHash.Clear();
        _byVanillaPtr.Clear();
        _assigned.Clear();
    }
}
