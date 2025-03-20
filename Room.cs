
using shared;
using System.Net.Sockets;
using System.Text;

public abstract class Room(string name)
{
    protected readonly List<GameClient> clients = [];
    public string Name { get; } = name;

    public void SafeForEach(Action<GameClient> method)
    {
        for (int i = clients.Count - 1; i >= 0; i--)
        {
            try
            {
                method(clients[i]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                if (!clients[i].Client.Connected) clients.RemoveAt(i);
            }
        }

    }

    public virtual void OnEnter() { }
    public virtual void OnExit() { }

    public abstract void ProcessMessage(string header, string content, GameClient sender, NetworkStream senderStream);

    protected static void Write(NetworkStream stream, string header, string content)
    {
        StreamUtil.Write(stream, GetBytes(string.Join(',', header, content)));

    }

    protected static byte[] GetBytes(string input)
    {
        return Encoding.UTF8.GetBytes(input);
    }

    public void AddMember(GameClient client)
    {
        clients.Add(client);
        OnPlayerJoined(client);
        Console.WriteLine($"{client.Name} joined the room: {Name}");

    }

    public void RemoveMember(GameClient client)
    {
        clients.Remove(client);
        OnPlayerLeft(client);
        Console.WriteLine($"{client.Name} left the room: {Name}");
    }

    protected virtual void OnPlayerLeft(GameClient player)
    {
        SafeForEach(other =>
        {
            Write(other.Client.GetStream(), "chat", player.Name + " left the chat: " + Name);

        });
    }

    protected virtual void OnPlayerJoined(GameClient player)
    {
        SafeForEach(other =>
        {
            if(other == player)
                Write(other.Client.GetStream(), "chat", "you joined the chat: " + Name);
            else
                Write(other.Client.GetStream(), "chat", player.Name + " joined the chat: " + Name);

        });
    }
}
