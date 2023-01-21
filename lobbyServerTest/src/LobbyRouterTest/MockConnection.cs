using System.Collections.Generic;
using frar.clientserver;
using System;

namespace frar.lobbyserver.test;

public class MockConnection : IConnection {
    public event ShutDownEvent? OnShutDown;

    public List<Packet> Packets = new List<Packet>();

    public Packet Read() {
        throw new NotImplementedException();
    }

    public void Shutdown(DISCONNECT_REASON reason) {
        throw new NotImplementedException();
    }

    public void Write(Packet packet) {
        Packets.Add(Packet.FromString(packet.ToString()));
    }

    public Packet Get(string action) {
        foreach (Packet packet in this.Packets) {
            if (packet.Action == action) {
                this.Packets.Remove(packet);
                return packet;
            }
        }

        this.AvailablePackets().ForEach(s => System.Console.WriteLine(s));
        throw new Exception($"Unknown Packet: {action}");
    }

    public Packet Pop() {
        if (this.Packets.Count == 0) throw new Exception($"No available packets");
        var packet = this.Packets[0];
        this.Packets.RemoveAt(0);
        return packet;
    }

    public bool Has(string action) {
        foreach (Packet packet in this.Packets) {
            if (packet.Action == action) {
                return true;
            }
        }

        return false;
    }

    public Packet Assert(string action) {
        if (!this.Has(action)) {
            this.AvailablePackets().ForEach(s => System.Console.WriteLine(s));
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"unknown packet '{action}'");
        }
        return this.Peek(action);
    }

    public Packet Assert<T>(string action, string key, T value) {
        foreach (Packet packet in this.Packets) {
            if (packet.Action != action) continue;
            if (!packet.Has(key)) continue;
            if (packet.Get<T>(key)!.Equals(value)) return packet;
        }
        throw new Exception($"Unknown Packet: {action}.{key}");
    }    

    public Packet Peek(string action) {
        foreach (Packet packet in this.Packets) {
            if (packet.Action == action) {
                return packet;
            }
        }

        this.AvailablePackets().ForEach(s => System.Console.WriteLine(s));
        throw new Exception($"Unknown Packet: {action}");
    }

    public List<string> AvailablePackets() {
        var available = new List<string>();

        foreach (Packet packet in this.Packets) {
            available.Add(packet.Action);
        }
        return available;
    }
}
