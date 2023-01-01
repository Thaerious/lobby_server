using Newtonsoft.Json;

namespace frar.lobbyserver;

public partial class LobbyModel {
    public class Player {
        [JsonProperty] public readonly string Name = "";
        public string Game = "";

        // JSON Constructor
        private Player() { }

        public Player(string name) {
            this.Name = name;
        }

        [JsonIgnore] public bool HasGame { get => this.Game != ""; }
    }
}
