using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

public class GameRoom(string name) : Room(name)
{

    public override void ProcessMessage(string header, string content, GameClient sender, NetworkStream senderStream)
    {
        switch (header)
        {
            case "join":
                OnJoin(content, sender, senderStream);
                break;

            case "chat":
                OnChat(content, sender, senderStream);
                break;

            case "changeName":
                OnChangeName(content, sender, senderStream);
                break;

            case "list":
                OnList(content, sender, senderStream);
                break;

            case "help":
                OnHelp(content, sender, senderStream);
                break;

            case "listRooms":
                OnListRooms(content, sender, senderStream);
                break;

            case "whisper":
                OnListRooms(content, sender, senderStream);
                break;

        }
    }

    void OnJoin(string content, GameClient sender, NetworkStream senderStream)
    {
        TCPServerSample.JoinOrCreateRoom(content, sender, this);
    }

    void OnChat(string content, GameClient sender, NetworkStream senderStream)
    {
        SafeForEach(other =>
        {
            if (sender == other)
            {
                Write(senderStream, "chat", "You: " + content);
            }
            else
            {
                Write(other.Client.GetStream(), "chat", sender.Name + ": " + content);
            }

        });
    }

    void OnChangeName(string content, GameClient sender, NetworkStream senderStream)
    {
        var validatedName = content.ToLower().Trim();
        if (string.IsNullOrEmpty(validatedName))
        {
            Write(senderStream, "changeName", "Failed to change name, reason: empty name");
            return;
        }
        else 
        {
            bool alreadyExistsInAllRooms = TCPServerSample.GetRooms().Any(r => r.Clients.Any(c => c.Name == content));
            if (alreadyExistsInAllRooms)
            {
                Write(senderStream, "changeName", "Failed to change name, reason: it already exists");
                return;
            }
        }

        SafeForEach(other =>
        {
            NetworkStream otherStream = other == sender ? senderStream : other.Client.GetStream();

            if (sender == other)
            {
                Write(otherStream, "changeName", "You changed name to " + validatedName);
            }
            else
            {
                Write(otherStream, "changeName", $"{sender.Name} changed name to {validatedName}");
            }

        });
        sender.Name = validatedName;
    }
    void OnList(string content, GameClient sender, NetworkStream senderStream)
    {
        Write(senderStream, "chat", "Current chatters:\n" + string.Join('\n', clients.Select(c => c.Name)));
    }

    void OnHelp(string content, GameClient sender, NetworkStream senderStream)
    {
        string possibleCommands = string.Join('\n', "help", "list", "changeName", "whisper");
        Write(senderStream, "chat", "Possible commands:\n" + possibleCommands);
    }

    void OnListRooms(string content, GameClient sender, NetworkStream senderStream)
    {
        var roomsEnumerable = TCPServerSample.Rooms.Select(r => r.Key);
        Write(senderStream, "chat", "Current rooms:\n" + string.Join('\n', roomsEnumerable));
    }

    void OnWhisper(string content, GameClient sender, NetworkStream senderStream)
    {
        string[] commands = content.Split(' ');
        if (commands.Length < 2)
        {
            Write(senderStream, "chat", "Command is not full");
            return;
        }

        string targetName = commands[0];
        string messageStr = string.Join(' ', commands[1..]);
        GameClient target = clients.FirstOrDefault(c => c.Name == targetName);
        if (target == null)
        {
            Write(senderStream, "chat", "Chatter could not be found");
            return;
        }
        Write(senderStream, "chat", $"You whisper to {targetName}: {messageStr}");
        Write(target.Client.GetStream(), "chat", $"<{sender.Name}> whispers: {messageStr}");
    }
}
