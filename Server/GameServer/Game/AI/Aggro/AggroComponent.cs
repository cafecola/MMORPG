
namespace GameServer.Game
{
	public class AggroComponent
	{
		private Dictionary</*ObjectId*/int, /*Damage*/float> _attackers = new Dictionary<int, float>();

		public List<int> GetTopAttackers()
		{
			return _attackers.OrderBy(x => x.Value).Select(x => x.Key).ToList();
		}

		public void OnDamaged(int objectId, float damage)
		{
			if (_attackers.ContainsKey(objectId))
				_attackers[objectId] += damage;
			else
				_attackers.Add(objectId, damage);
		}

		public void Reset()
		{
			_attackers.Clear();
		}
	}
}
