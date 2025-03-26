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
    public static void Main (string[] args)
	{
        Rooms = new(rooms);
		Console.WriteLine("Server started on port 55555");

		TcpListener listener = new (IPAddress.Any, 55555);
		listener.Start ();

		int guestCount = 0;

        rooms.Add("Lobby", lobbyRoom);

		while (true)
		{
			//First big change with respect to example 001
			//We no longer block waiting for a client to connect, but we only block if we know
			//a client is actually waiting (in other words, we will not block)
			//In order to serve multiple clients, we add that client to a list
			while (listener.Pending()) {
				string name = "Guest" + guestCount++;
                var newMember = new GameClient(listener.AcceptTcpClient(), name);

                lobbyRoom.AddMember(newMember);
				Console.WriteLine("Accepted new client.");

                lobbyRoom.ProcessJoinedMsg(newMember, name);
            }

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
            while(_createRoomStack.Count > 0)
            {
                Action newRoomCall = _createRoomStack.Pop();
                newRoomCall();
            }
            //Although technically not required, now that we are no longer blocking, 
            //it is good to cut your CPU some slack
            Thread.Sleep(100);
		}

	}

	//public static bool TrySwitchToRoom(string name)
 //   {
 //       if (!rooms.TryGetValue(name, out Room value))
 //       {
 //           Console.WriteLine("Room could not be found");
 //           return false;
 //       }
 //       SwitchToRoom(value);
 //       return true;
 //   }

    //public static void SwitchToRoom(string name)
    //{
    //    SwitchToRoom(rooms[name]);
    //}

    //public static void SwitchToRoom(Room room)
    //{
    //    currentRoom.OnExit();
    //    currentRoom = room;
    //    currentRoom.OnEnter();
    //}

    //public static bool CreateRoom(string name)
    //{
    //    if (!rooms.ContainsKey(name))
    //    {
    //        Console.WriteLine("Room already exists");
    //        return false;
    //    }

    //    rooms.Add(name, new GameRoom(name));

    //    return true;
    //}

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


