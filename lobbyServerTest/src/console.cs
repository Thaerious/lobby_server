using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using frar.lobbyserver;
using System.Diagnostics;
using System.IO;
using frar.lobbyserver.test;
using frar.clientserver;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Reflection;

var client = new Client();
client.Register("user", "pw", "em");

public class Client {
    private Connection connection;

    public Client() {
        this.connection = Connection.ConnectTo("127.0.0.1", 5500);
    }

    public void Register(string username, string password, string email) {
        this.connection.Write("RegisterPlayer", username, password, email);
        Console.WriteLine(this.connection.Read());
        this.connection.Write("Login", username, password, email);
        Console.WriteLine(this.connection.Read());
    }
}