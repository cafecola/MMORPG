using GameServer;
using ServerCore;
using Google.Protobuf.Protocol;

namespace Server
{
	public partial class ClientSession : PacketSession
	{
		public void HandleAuthReq(C_AuthReq reqPacket)
		{
			// TODO : Validation
			// reqPacket.Jwt

			S_AuthRes resPacket = new S_AuthRes();
			resPacket.Success = true;

			Send(resPacket);
		}

		public void HandleHeroListReq()
		{
			// 캐싱된게 있으면 재사용.
			if (Heroes.Count == 0)
			{
				// 1) DB에서 정보 가져오고
				// 2) 그것을 메모리에 Hero로 만들어서 캐싱한다.
				List<HeroDb> heroDbs = DBManager.LoadHeroDb(AccountDbId);
				foreach (HeroDb heroDb in heroDbs)
				{
					Hero hero = MakeHeroFromHeroDb(heroDb);
					Heroes.Add(hero);
				}
			}			

			S_HeroListRes resPacket = new S_HeroListRes();
			foreach (Hero hero in Heroes)
				resPacket.Heroes.Add(hero.HeroInfoComp.MyHeroInfo);

			Send(resPacket);
		}

		public void HandleCreateHeroReq(C_CreateHeroReq reqPacket)
		{
			S_CreateHeroRes resPacket = new S_CreateHeroRes();

			// 1) 이름이 안 겹치는지 확인
			// 2) 생성 진행
			HeroDb heroDb = DBManager.CreateHeroDb(AccountDbId, reqPacket);
			if (heroDb != null)
			{
				resPacket.Result = ECreateHeroResult.Success;
				// 메모리에 캐싱
				Hero hero = MakeHeroFromHeroDb(heroDb);
				Heroes.Add(hero);
			}
			else
			{
				resPacket.Result = ECreateHeroResult.FailDuplicateName;
			}

			Send(resPacket);
		}

		public void HandleDeleteHeroReq(C_DeleteHeroReq reqPacket)
		{
			Console.WriteLine("HandleEnterGame");

			int index = reqPacket.HeroIndex;
			if (index < 0 || index >= Heroes.Count)
				return;

			Hero hero = Heroes[index];
			if (hero == null)
				return;

			// 1) 이름이 안 겹치는지 확인
			// 2) 생성 진행
			bool success = DBManager.DeleteHeroDb(hero.HeroDbId);

			if (success)
			{
				Heroes.Remove(hero);
			}

			S_DeleteHeroRes resPacket = new S_DeleteHeroRes();
			resPacket.Success = success;
			resPacket.HeroIndex = index;
			Send(resPacket);
		}

		public void HandleEnterGame(C_EnterGame enterGamePacket)
		{
			Console.WriteLine("HandleEnterGame");

			int index = enterGamePacket.HeroIndex;
			if (index < 0 || index >= Heroes.Count)
				return;

			MyHero = Heroes[index];

			Hero hero = MyHero;
			if (hero == null)
				return;

			GameRoom room = GameLogic.Find(hero.HeroInfoComp.MyHeroInfo.MapId);
			if (room == null)
				return;

			room.Push(room.EnterGame, hero, hero.CellPos, false);
		}

		public void HandleLeaveGame()
		{
			Console.WriteLine("HandleLeaveGame");
			Disconnect();
		}

		Hero MakeHeroFromHeroDb(HeroDb heroDb)
		{
            int templateId = (int)heroDb.ClassType;

			Hero hero = ObjectManager.Instance.Spawn<Hero>(templateId);
			hero.Init(heroDb);
            hero.Session = this;

			return hero;
		}
	}
}
