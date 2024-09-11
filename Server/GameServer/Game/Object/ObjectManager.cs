using GameServer.Game;
using Google.Protobuf.Protocol;
using Server;

namespace GameServer
{
	public class ObjectManager : Singleton<ObjectManager>
	{
		private readonly object _locker = new();
		private Dictionary<int, Hero> _heroes = [];

		// [OBJ_TYPE(4)][TEMPLATE_ID(8)][ID(20)]
		int _counter = 0;

		public T Spawn<T>(int templateId = 0) where T : BaseObject, new()
		{
			T obj = new T();

			lock (_locker)
			{
				obj.ObjectId = GenerateId(obj.ObjectType, templateId);

				if (obj.ObjectType == EGameObjectType.Hero)
					_heroes.Add(obj.ObjectId, obj as Hero);
			}

			if (obj.ObjectType == EGameObjectType.Monster)
				(obj as Monster).Init(templateId);
			else if (obj.ObjectType == EGameObjectType.Npc)
			{ 
				(obj as Npc).Init(templateId);
			}

            return obj;
		}

		int GenerateId(EGameObjectType type, int templateId)
		{
			lock (_locker)
			{
				return ((int)type << 28) | (templateId << 20) | (_counter++);
			}
		}

		public static EGameObjectType GetObjectTypeFromId(int id)
		{
			int type = (id >> 28) & 0x0F;
			return (EGameObjectType)type;
		}

		public static int GetTemplateIdFromId(int id)
		{
			int templateId = (id >> 20) & 0xFF;
			return templateId;
		}

		public bool Remove(int objectId)
		{
			EGameObjectType objectType = GetObjectTypeFromId(objectId);

			lock (_locker)
			{
				if (objectType == EGameObjectType.Hero)
					return _heroes.Remove(objectId);
			}

			return false;
		}

		public Hero FindHero(int objectId)
		{
			EGameObjectType objectType = GetObjectTypeFromId(objectId);

			lock (_locker)
			{
				if (objectType == EGameObjectType.Hero)
				{
					if (_heroes.TryGetValue(objectId, out Hero hero))
						return hero;
				}
			}

			return null;
		}
	}
}
