using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using frar.lobbyserver;
using System.Diagnostics;
using System.IO;
using frar.lobbyserver.test;

var router = new LobbyRouter();

var dbi = new DatabaseInterface();
dbi.CreateTables(userTable: "test_users", sessionTable: "test_sessions");
dbi.DropTables();
