using frar.clientserver;
namespace frar.lobbyserver;

public class Server : frar.clientserver.Server<LobbyRouter> {
    public static void Main(string[] args) {
        System.Console.WriteLine("Starting Server");
        var server = new Server();
        server.Connect("127.0.0.1", 5500);
        server.Listen();
    }

    public Server() : base(){
        var dbi = new DatabaseInterface();
        dbi.CreateTables(userTable: "userTest", sessionTable: "sessionTest");
        dbi.ClearAll();
    }

    public override LobbyRouter NewHandler() {
        var dbi = new DatabaseInterface();
        dbi.SetTables(userTable: "userTest", sessionTable: "sessionTest");
        var router = new LobbyRouter(dbi);
        router.AddHandler(new EchoHandler());
        return router;
    }
}

public class EchoHandler {
    [OnConnect]
    public void OnConnect(Connection connection) {
        System.Console.WriteLine("New Connection");
    }

    [Route(Rule = ".*", Index = -1)]
    public void Echo([Req]Packet packet) {
        System.Console.WriteLine(packet.ToString());
    }
}