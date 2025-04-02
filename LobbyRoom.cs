using System.Net.Sockets;

namespace server
{
    class LobbyRoom(string name) : Room(name)
    {
        public override void ProcessMessage(string header, string content, GameClient sender, NetworkStream senderStream)
        {
            switch (header)
            {
                case "join":
                    OnJoin(content, sender, senderStream);
                    break;
                case "listRooms":
                    OnListRooms(content, sender, senderStream);
                    break;
                case "changeName":
                    OnChangeName(content, sender, senderStream);
                    break;
            }
        }

        protected override void OnPlayerLeft(GameClient player)
        {
            Write(player.Client.GetStream(), "chat", "You left the lobby ");
        }

        protected override void OnPlayerJoined(GameClient player)
        {
            Write(player.Client.GetStream(), "chat", "You joined the lobby ");
        }

        public void ProcessJoinedMsg(GameClient sender, string name)
        {
            Write(sender.Client.GetStream(), "joined", "You Joined as: " + name);
        }

        void OnJoin(string content, GameClient sender, NetworkStream senderStream)
        {
            TCPServerSample.JoinOrCreateRoom(content, sender, this);
        }

        void OnListRooms(string content, GameClient sender, NetworkStream senderStream)
        {
            var roomsEnumerable = TCPServerSample.Rooms.Select(r => r.Key);
            Write(senderStream, "chat", "Current rooms:\n" + string.Join('\n', roomsEnumerable));
        }

        void OnChangeName(string content, GameClient sender, NetworkStream senderStream)
        {
            var validatedName = content.ToLower().Trim();
            if (string.IsNullOrEmpty(validatedName))
            {
                Write(senderStream, "changeName", "Failed to change name, reason: empty name");
                return;
            }
            else if (TCPServerSample.GetRooms().Any(r=> r.Clients.Any(c => c.Name == content)))
            {
                Write(senderStream, "changeName", "Failed to change name, reason: it already exists");
                return;
            }
            Write(senderStream, "changeName", "You changed name to " + validatedName);
            sender.Name = validatedName;
        }
    }
}
