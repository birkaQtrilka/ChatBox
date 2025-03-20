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
                TCPServerSample.JoinOrCreateRoom(content, sender, this);
                break;

            case "chat":
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
                break;

            case "list":
                Write(senderStream, "chat", "Current chatters:\n" + string.Join('\n', clients.Select(c => c.Name)));
                break;

            case "help":
                string possibleCommands = string.Join('\n', "help", "list", "changeName", "whisper");
                Write(senderStream, "chat", "Possible commands:\n" + possibleCommands);
                break;

            case "whisper":
                string[] commands = content.Split(' ');
                if (commands.Length < 2)
                {
                    Write(senderStream, "chat", "Command is not full");
                    return;
                }

                string targetName = commands[0];
                string messageStr = commands[1];
                GameClient target = clients.FirstOrDefault(c => c.Name == targetName);
                if (target == null)
                {
                    Write(senderStream, "chat", "Chatter could not be found");
                    return;
                }
                Write(senderStream, "chat", $"You whisper to {targetName}: {messageStr}");
                Write(target.Client.GetStream(), "chat", $"<{sender.Name}> whispes: {messageStr}");
                break;

        }
    }
}
