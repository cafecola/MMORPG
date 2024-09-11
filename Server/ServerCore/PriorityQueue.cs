
namespace ServerCore
{
	public class PriorityQueue<T> where T : IComparable<T>
	{
		List<T> _heap = new List<T>();

		public int Count { get { return _heap.Count; } }

		public void Push(T data)
		{
			// 힙의 맨 끝에 새로운 데이터 삽입.
			_heap.Add(data);

			int now = _heap.Count - 1;
			
			while (now > 0)
			{
				int next = (now - 1) / 2;
				if (_heap[now].CompareTo(_heap[next]) < 0)
					break; // 실패

				// swap
				T temp = _heap[now];
				_heap[now] = _heap[next];
				_heap[next] = temp;

				// 검사위치 이동.
				now = next;
			}
		}

		public T Pop()
		{
			// 반환할 데이터 저장
			T ret = _heap[0];

			// 마지막 데이터 루트로 이동.
			int lastIndex = _heap.Count - 1;
			_heap[0] = _heap[lastIndex];
			_heap.RemoveAt(lastIndex);
			lastIndex--;

			int now = 0;
			while (true)
			{
				int left = 2 * now + 1;
				int right = 2 * now + 2;

				int next = now;

				if (left <= lastIndex && _heap[next].CompareTo(_heap[left]) < 0)
					next = left;

				if (right <= lastIndex && _heap[next].CompareTo(_heap[right]) < 0)
					next = right;

				if (next == now)
					break;

				// swap
				T temp = _heap[now];
				_heap[now] = _heap[next];
				_heap[next] = temp;

				// 검사위치 이동.
				now = next;
			}

			return ret;
		}

		public T Peek()
		{
			if (_heap.Count == 0)
				return default(T);
			return _heap[0];
		}
	}
}
