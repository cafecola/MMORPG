
using CommandLine;

namespace PacketGenerator
{
    public enum ProgramType
    {
        None = -1,
		Client = 0, 
        GameServer = 1,        
    }

	public class Options
	{
		[Option('o', "outputPath", Required = true, HelpText = "Set output path. ex) Server/GameServer/Packet/Generated/ or Client/Assets/Scripts/Packet/Generated/ (root = M2/부터 경로)")]
		public string outputPath { get; set; }
		[Option('t', "programType", Required = true, HelpText = "Client = 0, GameServer = 1")]
		public int programType { get; set; }
	}

	class Program
    {
        static string clientPacketManager;
        static string gameServerPacketManager;

        static string clientMsgIdList;
        static string gameServerMsgIdList;

        static int s_protocolId = 1;
        static string s_outPath = "";
        static ProgramType s_type = ProgramType.None;

		static void RunOptions(Options opts)
		{
			s_outPath = opts.outputPath;
			s_type = (ProgramType)opts.programType;
		}

		static void Main(string[] args)
        {
            string rootDirPath = "../../../";
            string protoPath = rootDirPath + "Common/Protocol/Protocol.proto";

            CommandLine.Parser.Default
                .ParseArguments<Options>(args)
                .WithParsed(RunOptions);

            foreach (string line in File.ReadAllLines(protoPath))
            {
                string[] names = line.Split(" ");
                if (names.Length == 0)
                    continue;

                if (!names[0].StartsWith("message"))
                    continue;

                ParsePacket(names[1]);
            }

            if (s_type == ProgramType.Client)
            {
                string clientManagerText = string.Format(PacketFormat.managerFormat, clientMsgIdList, clientPacketManager);
                File.WriteAllText(rootDirPath + s_outPath + "ClientPacketManager.cs", clientManagerText);
            }
            else if (s_type == ProgramType.GameServer)
            {
                string gameServerManagerText = string.Format(PacketFormat.managerFormat, gameServerMsgIdList, gameServerPacketManager);
                File.WriteAllText(rootDirPath + s_outPath + "GameServerPacketManager.cs", gameServerManagerText);
            }
        }

        public static void ParsePacket(string name)
        {
            if (name.StartsWith("S_")) // GameServer -> Client
            {
                clientPacketManager += string.Format(PacketFormat.managerRegisterFormat, name);
                gameServerMsgIdList += string.Format(PacketFormat.msgIdRegisterFormat, name, s_protocolId);
                clientMsgIdList += string.Format(PacketFormat.msgIdRegisterFormat, name, s_protocolId);
                s_protocolId++;
            }
            else if (name.StartsWith("C_")) // Client -> GameServer
            {
                gameServerPacketManager += string.Format(PacketFormat.managerRegisterFormat, name);
                clientMsgIdList += string.Format(PacketFormat.msgIdRegisterFormat, name, s_protocolId);
                gameServerMsgIdList += string.Format(PacketFormat.msgIdRegisterFormat, name, s_protocolId);

                s_protocolId++;
            }
        }
    }
}
