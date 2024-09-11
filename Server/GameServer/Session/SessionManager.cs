
namespace Server
{
	class SessionManager : Singleton<SessionManager>
	{
		private readonly object _locker = new();
		private int _sessionId = 0;
		private Dictionary<int, ClientSession> _sessions = [];

		public List<ClientSession> GetSessions()
		{
			List<ClientSession> sessions;

			lock (_locker)
			{
				sessions = _sessions.Values.ToList();
			}

			return sessions;
		}

		public ClientSession Generate()
		{
			lock (_locker)
			{
				int sessionId = ++_sessionId;

				ClientSession session = new ClientSession();
				session.SessionId = sessionId;
				_sessions.Add(sessionId, session);

				Console.WriteLine($"Connected : {sessionId}");

				return session;
			}
		}

		public ClientSession Find(int id)
		{
			lock (_locker)
			{
				_sessions.TryGetValue(id, out ClientSession session);
				return session;
			}
		}

		public void Remove(ClientSession session)
		{
			lock (_locker)
			{
				_sessions.Remove(session.SessionId);
			}
		}
	}
}
