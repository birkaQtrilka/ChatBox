using System.Net.Sockets;

    public class GameClient(TcpClient client, string name)
    {
        public TcpClient Client { get; private set; } = client;
        public string Name = name;
    }


