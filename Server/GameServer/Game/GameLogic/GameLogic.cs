using Server.Data;
using System.Collections.Concurrent;

namespace GameServer
{
	// GameLogic
	// - GameRoom
	// -- Zone
	public class GameLogic : JobSerializer
	{
		// 메인 쓰레드 Job 등록.
		public static GameLogic Instance { get; } = new GameLogic();

		// 게임 관리.
		private static Dictionary<int, GameRoom> _rooms;
		private static int _roomIdGenerator = 1;
		private static int _threadCount;

		static ConcurrentQueue<GameRoom> _updateQueue;

        public GameLogic()
        {
			_rooms = new Dictionary<int, GameRoom>();
			_roomIdGenerator = 1;
			_updateQueue = new ConcurrentQueue<GameRoom>();

			foreach (RoomData roomData in DataManager.RoomDict.Values)
			{
				GameRoom room = Add(roomData);
				_updateQueue.Enqueue(room);
			}
		}

		#region Add & Find
		static private GameRoom Add(RoomData roomData)
		{
			GameRoom gameRoom = new GameRoom();
			gameRoom.Init(roomData, 10);

			gameRoom.RoomId = _roomIdGenerator;
			_rooms.Add(_roomIdGenerator, gameRoom);
			_roomIdGenerator++;

			return gameRoom;
		}

		static public GameRoom Find(int roomId)
		{
			if (_rooms.TryGetValue(roomId, out GameRoom room))
				return room;

			return null;
		}
		#endregion

		static public void FlushMainThreadJobs()
		{
			// 메인 쓰레드.
			Thread.CurrentThread.Name = "MainThread";

			while (true)
			{
				Instance.Flush();
				Thread.Sleep(0);
			}
		}

		#region Job 배분 : Multi-Thread Version
		static public void LaunchGameThreads(int threadCount)
		{
			_threadCount = threadCount;

			// Thread 생성.
			for (int i = 0; i < threadCount; i++)
			{
				Thread t = new Thread(new ParameterizedThreadStart(GameThreadJob_1));
				t.Name = $"GameLogic_{i}";
				t.Start(i);
			}
		}

		static public void GameThreadJob_1(object arg)
		{
			int threadId = (int)arg;
			int idx = threadId % _threadCount;

			// 쓰레드가 담당하는 Room 찾기.
			List<GameRoom> rooms = _rooms
				.Where(r => r.Key % _threadCount == idx)
				.Select(r => r.Value)
				.ToList();

			while (true)
			{
				foreach (GameRoom room in rooms)
					room.Update();

				Thread.Sleep(0);
			}
		}

		static public void GameThreadJob_2(object arg)
		{
			int threadId = (int)arg;

			// 담당할 Room을 경쟁을 통해 뽑기.
			while (true)
			{
				if (_updateQueue.TryDequeue(out GameRoom room) == false)
					continue;

				room.Flush();				

				_updateQueue.Enqueue(room);

				Thread.Sleep(0);
			}
		}
		#endregion

		#region Job 배분 : Task Version
		static public void LaunchRoomUpdateTasks()
		{
			foreach (GameRoom room in _rooms.Values)
			{
				StartRoomUpdateTask(room);
			}
		}

		static public void StartRoomUpdateTask(GameRoom room)
		{
			Task.Run(() =>
			{
				room.Update();
				StartRoomUpdateTask(room);
			});
		}
		#endregion
	}
}
