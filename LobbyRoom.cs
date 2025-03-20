using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    class LobbyRoom(string name) : Room(name)
    {
        public override void ProcessMessage(string header, string content, GameClient sender, NetworkStream senderStream)
        {
            switch (header)
            {
                case "join":
                    TCPServerSample.JoinOrCreateRoom(content, sender, this);
                    break;
                case "changeName":
                    var validatedName = content.ToLower().Trim();
                    if (string.IsNullOrEmpty(validatedName))
                    {
                        Write(senderStream, "changeName", "Failed to change name, reason: empty name");
                        return;
                    }
                    else if (clients.Any(c => c.Name == content))
                    {
                        Write(senderStream, "changeName", "Failed to change name, reason: it already exists");
                        return;
                    }
                    Write(senderStream, "changeName", "You changed name to " + validatedName);
                    
                    sender.Name = validatedName;
                    break;
            }
        }

        public void ProcessJoinedMsg(GameClient sender, string name)
        {
            Write(sender.Client.GetStream(), "joined", "You Joined as: " + name);
        }

        protected override void OnPlayerLeft(GameClient player)
        {
            Write(player.Client.GetStream(), "chat", "You left the lobby ");
        }

        protected override void OnPlayerJoined(GameClient player)
        {
            Write(player.Client.GetStream(), "chat", "You joined the lobby ");
        }
    }
}
