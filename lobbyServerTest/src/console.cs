using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using frar.lobbyserver;
using System.Diagnostics;
using System.IO;
using frar.lobbyserver.test;
using frar.clientserver;

var test = new LobbyTest();

var adam = test.NewUser("adam", false);
var packet = new Packet("adam", "super secret", "who@ami");
System.Console.WriteLine(packet);
adam.router.Process(new Packet("adam", "super secret", "who@ami"));
System.Console.WriteLine(adam.conn.Pop());