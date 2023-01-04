using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using frar.lobbyserver;
using System.Diagnostics;
using System.IO;
using frar.lobbyserver.test;
using frar.clientserver;
using System.Collections.Generic;
using Newtonsoft.Json;

var testbed = new ALobbyTest();
var adam = testbed.NewUser("adam");
adam.CreateGame();
var player = adam.GetGame("adam's game");

System.Console.WriteLine(JsonConvert.SerializeObject(player));