using System.Text;
using System.Xml.Linq;

namespace PacketGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string filePath = "../../../../../Common/protoc-31.1-win64/bin/protocol.proto";
            string serverPacketManager;
            string clientPacketManager;
            StringBuilder serverHandler = new StringBuilder();
            StringBuilder clientHandler = new StringBuilder();

            if (args.Length > 0)
                filePath = args[0];

            var lines = File.ReadLines(filePath);
            bool isData = false;
            foreach (var line in lines)
            {
                // 메세지 정의가 시작되는 부분까지 건너 뜀
                if (isData == false)
                {
                    if (!line.Contains("MsgId"))
                        continue;
                    else
                    {
                        isData = true;
                        continue;
                    }  
                }

                // 메세지 정의가 끝나거나 메세지 아이디의 명명규칙에 맞지 않는 경우 종료
                if (line.Contains("}") || !(line.Contains("S_") || line.Contains("C_")))
                    break;

                string packetName = line.Split(" = ")[0].ToLower().Trim();
                // S_TEST_MESSAGE -> STestMessage
                StringBuilder nameType1 = new StringBuilder();
                foreach (var name in packetName.Split('_'))
                {
                    if (name.Length == 0)
                        continue;

                    string temp = name.Replace(name, char.ToUpper(name[0]) + name.Substring(1));
                    nameType1.Append(temp);

                }
                // S_TEST_MESSAGE -> S_TestMessage
                StringBuilder nameType2 = new StringBuilder();
                nameType2.Append(nameType1);
                nameType2.Insert(1, '_');

                // 핸들러 코드 템플릿에 맞춰 코드 생성
                string handlerCode = string.Format(CodeTempletes.PacketHandler, nameType1, nameType2);
                if (line.Contains("S_"))
                {
                    clientHandler.AppendLine(handlerCode);
                }
                else if (line.Contains("C_"))
                {
                    serverHandler.AppendLine(handlerCode);
                }
            }
            // 패킷 매니저 코드에 핸들러 코드 삽입
            serverPacketManager = string.Format(CodeTempletes.PacketManager, serverHandler);
            clientPacketManager = string.Format(CodeTempletes.PacketManager, clientHandler);

            File.WriteAllText("ServerPacketManager.cs", serverPacketManager);
            File.WriteAllText("ClientPacketManager.cs", clientPacketManager);
        }
    }
}
