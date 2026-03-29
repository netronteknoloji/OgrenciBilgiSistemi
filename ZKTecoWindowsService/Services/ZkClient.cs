using zkemkeeper;

namespace ZKTecoWindowsService.Services;

public interface IZkClient
{
    bool Connect(string ip, int port, int commKey = 0);
    void Disconnect();
    bool EnableDevice(bool enable);
    // ihtiyaca göre: log çekme, kart ekleme vb.
}

public sealed class ZkClient : IZkClient
{
    private CZKEM? _zk;

    public bool Connect(string ip, int port, int commKey = 0)
    {
        _zk = new CZKEM();
        if (!_zk.Connect_Net(ip, port)) return false;
        if (commKey > 0) _zk.SetCommPassword(commKey);
        return true;
    }

    public void Disconnect()
    {
        try { _zk?.Disconnect(); } catch { /* ignore */ }
        _zk = null;
    }

    public bool EnableDevice(bool enable)
    {
        if (_zk == null) return false;
        return _zk.EnableDevice(1, enable);
    }
}
