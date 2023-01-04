using frar.clientserver;

namespace frar.lobbyserver.test;

public static class PacketExtension {
    public static Packet Assert<T>(this Packet packet, string key, T value) {
        if (!packet.Has(key)) {
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"Unknown key '{key}'");
        }
        else if (!packet.Get(typeof(T), key).Equals(value)) {
            var msg = $"Mismatched value for key '{key}'. Expected '{value}', actual '{packet[key]}'.";
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail(msg);
        }
        return packet;
    }

    public static Packet Assert(this Packet packet, string key) {
        if (!packet.Has(key)) {
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"Unknown key '{key}'");
        }
        return packet;
    }

}
