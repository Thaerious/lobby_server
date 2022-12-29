using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using frar.lobbyserver;
using System.Diagnostics;
using System.IO;

var x = System.AppDomain.CurrentDomain.BaseDirectory;
var y = Environment.CurrentDirectory;
Console.WriteLine(x);
Console.WriteLine(y);

LobbyModel lobbyModel = new LobbyModel();
lobbyModel.AddPlayer("Adam");
lobbyModel.CreateGame("Adam's Game", "Adam", 4, "password");
