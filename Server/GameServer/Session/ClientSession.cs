using ServerCore;
using System.Net;
using Google.Protobuf;
using GameServer;
using Google.Protobuf.Protocol;

namespace Server
{
	public partial class ClientSession : PacketSession
	{
		public long AccountDbId { get; set; }
		public int SessionId { get; set; }
		public List<Hero> Heroes { get; set; } = [];
		public Hero MyHero { get; set; }

		private readonly object _locker = new();

        long _pingpongTick = 0;

        public void Ping()
        {
            if (_pingpongTick > 0)
            {
                long delta = (System.Environment.TickCount64 - _pingpongTick);
                if (delta > 60 * 1000)
                {
                    Console.WriteLine("Disconnected by PingCheck");
					Disconnect();
					return;
                }
            }

            S_Ping pingPacket = new S_Ping();
            Send(pingPacket);

            GameLogic.Instance.PushAfter(5000, Ping);
        }

        public void HandlePong()
        {
            _pingpongTick = System.Environment.TickCount64;
        }

        #region Network
        // 예약만 하고 보내지는 않는다
        public void Send(IMessage packet)
		{
			Send(new ArraySegment<byte>(MakeSendBuffer(packet)));
		}

		public static byte[] MakeSendBuffer(IMessage packet)
		{
			MsgId msgId = (MsgId)Enum.Parse(typeof(MsgId), packet.Descriptor.Name);
			ushort size = (ushort)packet.CalculateSize();
			byte[] sendBuffer = new byte[size + 4];
			Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));
			Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort));
			Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);
			return sendBuffer;
		}

		public override void OnConnected(EndPoint endPoint)
		{
			Console.WriteLine($"OnConnected : {endPoint}");

			GameLogic.Instance.PushAfter(5000, Ping);
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
		{
			PacketManager.Instance.OnRecvPacket(this, buffer);
		}

		public override void OnDisconnected(EndPoint endPoint)
		{
			GameLogic.Instance.Push(() =>
            {
                if (MyHero == null)
                    return;

                GameRoom room = GameLogic.Find(MyHero.HeroInfoComp.MapId);
				if (room == null)
					return;

                room.Push(room.LeaveGame, MyHero.ObjectId, ELeaveType.Disconnected);
            });

			foreach (Hero hero in Heroes)
				ObjectManager.Instance.Remove(hero.ObjectId);

			SessionManager.Instance.Remove(this);

			foreach (Hero hero in Heroes)
				DBManager.Clear(hero.HeroDbId);

			Console.WriteLine($"OnDisconnected : {endPoint}");
		}

		public override void OnSend(int numOfBytes)
		{
			//Console.WriteLine($"Transferred bytes: {numOfBytes}");
		}
		#endregion
	}
}
