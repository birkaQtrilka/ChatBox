using System.Net.Sockets;
using System.Net;
using shared;
using System.Text;
using server;
using System.Collections.ObjectModel;

partial class TCPServerSample
{
    static Dictionary<string, Room> rooms = new();
    static LobbyRoom lobbyRoom = new LobbyRoom("Lobby");
    static Stack<Action> _createRoomStack = new Stack<Action>();
    public static ReadOnlyDictionary<string, Room> Rooms { get; private set; }
    static int guestCount = 0;

    public static void Main (string[] args)
	{
        Rooms = new(rooms);
		Console.WriteLine("Server started on port 55555");

		TcpListener listener = new (IPAddress.Any, 55555);
		listener.Start ();


        rooms.Add("Lobby", lobbyRoom);

		while (true)
		{
            ProcessNewClients(listener);
            ProcessExistingClients();
            ProcessCommands();
            //Although technically not required, now that we are no longer blocking, 
            //it is good to cut your CPU some slack
            Thread.Sleep(100);
		}

	}

    static void ProcessNewClients(TcpListener listener)
    {
        while (listener.Pending())
        {
            string name = "Guest" + guestCount++;
            var newMember = new GameClient(listener.AcceptTcpClient(), name);

            lobbyRoom.AddMember(newMember);
            Console.WriteLine("Accepted new client.");

            lobbyRoom.ProcessJoinedMsg(newMember, name);
        }
    }

    static void ProcessExistingClients()
    {
        foreach (var currentRoom in rooms.Values)
        {
            currentRoom.SafeForEach(gameClient =>
            {
                TcpClient client = gameClient.Client;
                if (client.Available == 0) return;
                NetworkStream stream = client.GetStream();

                byte[] inBytes = StreamUtil.Read(client.GetStream());
                string[] input = Encoding.UTF8.GetString(inBytes).Split(',');
                string header = input[0];
                string content = input[1];

                currentRoom.ProcessMessage(header, content, gameClient, stream);

            });
        }
    }

    static void ProcessCommands()
    {
        while (_createRoomStack.Count > 0)
        {
            Action newRoomCall = _createRoomStack.Pop();
            newRoomCall();
        }
    }

    public static List<Room> GetRooms() => new(rooms.Values);

    public static void JoinOrCreateRoom(string name, GameClient sender, Room leavingRoom)
    {
        _createRoomStack.Push(() => {
            if (!rooms.TryGetValue(name, out Room room))
            {
                room = new GameRoom(name);
                rooms.Add(name, room);
            }

            leavingRoom.RemoveMember(sender);
            room.AddMember(sender);
        });
        
    }
}


