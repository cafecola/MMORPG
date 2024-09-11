
namespace Server
{
	public class Singleton<T> where T : new()
	{
		static T _instance = new T();

		public static T Instance { get { return _instance; } }
	}
}
