using System.Collections.Concurrent;

namespace GameServer
{
	public class DBJobQueue
	{
		private readonly object _locker = new();
		private Queue<Action> _jobQueue { get; } = new Queue<Action>();
		public bool Processing { get; private set; }
		
		public void Enqueue(Action job)
		{
			lock (_locker)
			{
				_jobQueue.Enqueue(job);
			}
		}

		public Action TryDequeue()
		{
			lock (_locker)
			{
				if (Processing)
					return null;

				if (_jobQueue.TryDequeue(out Action result))
				{
					Processing = true;
					return result;
				}
			}

			return null;
		}

		public void FinishProcessing()
		{
			lock (_locker)
			{
				Processing = false;
			}
		}
	}

	public partial class DBManager
	{
		public static ConcurrentDictionary<int/*heroDbId*/, DBJobQueue> _jobQueueDic = new ConcurrentDictionary<int, DBJobQueue>();
		public static ConcurrentQueue<int/*heroDbId*/> _executeQueue = new ConcurrentQueue<int>();

		#region JobQueue

		public static void Push(int heroDbId, Action action)
		{
			if (_jobQueueDic.ContainsKey(heroDbId) == false)
			{
				_jobQueueDic.TryAdd(heroDbId, new DBJobQueue());
				_executeQueue.Enqueue(heroDbId);
			}	

			_jobQueueDic[heroDbId].Enqueue(() => { action.Invoke(); FinishProcessing(heroDbId); });
		}

		public static Action TryPop(int heroDbId)
		{
			if (_jobQueueDic.TryGetValue(heroDbId, out DBJobQueue jobQueue) == false)
				return null;

			return jobQueue.TryDequeue();
		}

		public static void Clear(int heroDbId)
		{
			_jobQueueDic.TryRemove(heroDbId, out DBJobQueue jobQueue);
		}

		private static void FinishProcessing(int heroDbId)
		{
			if (_jobQueueDic.TryGetValue(heroDbId, out DBJobQueue jobQueue) == false)
				return;

			jobQueue.FinishProcessing();
		}

		private static bool ContainsKey(int heroDbId)
		{
			return _jobQueueDic.ContainsKey(heroDbId);
		}
		#endregion

		static int _threadCount = 0;

		public static void LaunchDBThreads(int threadCount)
		{
			_threadCount = threadCount;

			for (int i = 0; i < threadCount; i++)
			{
				Thread t = new Thread(new ParameterizedThreadStart(DBThreadJob));
				t.Name = $"DBThread_{i}";
				t.Start(i);
			}
		}

		static public void DBThreadJob(object arg)
		{
			int threadId = (int)arg;

			while (true)
			{
				if (_executeQueue.TryDequeue(out int heroDbId) == false)
					continue;

				if (ContainsKey(heroDbId) == false)
					continue;

				Action action = TryPop(heroDbId);
				if (action != null)
					action.Invoke();

				_executeQueue.Enqueue(heroDbId);

				Thread.Sleep(0);
			}
		}
	}
}
