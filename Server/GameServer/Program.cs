using System.Net;
using GameServer;
using ServerCore;

namespace Server
{
	// 1. Recv (N개)
	// 2. GameLogic (1)
	// 3. Send (1개)
	class Program
	{
		private static Listener _listener = new();
		private static Connector _connector = new();

		static void Main(string[] args)
		{
			ConfigManager.LoadConfig();
			DataManager.LoadData();

			IPAddress ipAddr = IPAddress.Parse(ConfigManager.Config.ip);			
			IPEndPoint endPoint = new IPEndPoint(ipAddr, ConfigManager.Config.port);
			_listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
			
			Console.WriteLine("Listening...");

			// GameLogic
			const int GameThreadCount = 2;
			GameLogic.LaunchGameThreads(GameThreadCount);
			//GameLogic.LaunchRoomUpdateTasks();

			// DB
			const int DbThreadCount = 2;
			DBManager.LaunchDBThreads(DbThreadCount);

			// MainThread
			GameLogic.FlushMainThreadJobs();
		}
	}
}
