using ServerCore;

namespace GameServer
{
	struct JobTimerElem : IComparable<JobTimerElem>
	{
		public long execTick; // 실행 시간
		public IJob job;

		public int CompareTo(JobTimerElem other)
		{
			return (int)(other.execTick - execTick);
		}
	}

	public class JobTimer
	{
		private PriorityQueue<JobTimerElem> _pq = new PriorityQueue<JobTimerElem>();
		private readonly object _locker = new();

		public int Count { get { lock (_locker) { return _pq.Count; } } }

		public void Push(IJob job, float tickAfter = 0)
		{
			JobTimerElem jobElement;
			jobElement.execTick = (long)(Utils.TickCount + tickAfter);
			jobElement.job = job;

			lock (_locker)
			{
				_pq.Push(jobElement);
			}
		}

		public void Flush()
		{
			while (true)
			{
				long now = Utils.TickCount;

				JobTimerElem jobElement;

				lock (_locker)
				{
					if (_pq.Count == 0)
						break;

					jobElement = _pq.Peek();
					if (jobElement.execTick > now)
						break;

					_pq.Pop();
				}

				jobElement.job.Execute();
			}
		}
	}
}
