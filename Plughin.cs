using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using On.OTAPI;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace UserCheck
{
    [ApiVersion(2, 1)]
    public class UserCheck : TerrariaPlugin
    {

        public override string Author => "Cai";

        public override string Description => "查找疑似相同的用户";

        public override string Name => "UserCheck";
        public override Version Version => new Version(1, 0, 0, 0);

        public UserCheck(Main game)
        : base(game)
        {
            Order = int.MaxValue;
        }
        Command Command = new Command("checkuser", Check, "checkuser", "cu");

        private static void Check(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("格式错误!正确格式:/cu <玩家名> [IP最少匹配段数]");
                return;
            }
            int ipMatch = 4;
            if (args.Parameters.Count>=2)
            {
                int.TryParse(args.Parameters[1], out ipMatch);
            }
            
            string username = args.Parameters[0];
            if (!string.IsNullOrWhiteSpace(username))
            {
                var account = TShock.UserAccounts.GetUserAccountByName(username);
                
                if (account != null)
                {
                    List<string> accKnownIps = JsonConvert.DeserializeObject<List<string>>(account.KnownIps?.ToString() ?? string.Empty).Distinct().ToList();
                    List<Match> matches = new List<Match>();
                    List<TShockAPI.DB.UserAccount> userAccounts = TShock.UserAccounts.GetUserAccounts();
                    foreach (var user in userAccounts)
                    {
                        if (user == account)
                        {
                            continue;
                        }
                        Match match = new(user.Name, user.UUID == account.UUID,user.ID);
                        try
                        {
                            
                            //Console.WriteLine(user.Name + user.KnownIps);
                            List<string> KnownIps = JsonConvert.DeserializeObject<List<string>>(user.KnownIps?.ToString() ?? string.Empty).Distinct().ToList();
                            if (KnownIps != null)
                            {
                                foreach (var p in accKnownIps)
                                {
                                    var accIpParts = p.Split('.');

                                    foreach (var i in KnownIps)
                                    {
                                        var matchCount = 0;
                                        var ipParts = i.Split('.');
                                        for (int j = 0; j < ipParts.Length; j++)
                                        {
                                            //Console.WriteLine($"{accIpParts[j]} {ipParts[j]} {matchCount}");
                                            if (accIpParts[j] == ipParts[j])
                                            {
                                                matchCount++;
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        if (matchCount >= ipMatch)
                                        {
                                            if (matchCount == 4)
                                            {
                                                match.IP.Add($"[c/FF0000:{i}]");
                                            }
                                            else
                                            {
                                                match.IP.Add($"[c/dea318:{i}]");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                        
                        
                        if (match.UUID || match.IP.Any())
                        {
                            match.IP = match.IP.Distinct().ToList();
                            matches.Add(match);
                        }
                    }
                    if (account.Name=="")
                    {
                        account.Name = $"无名字(acc:{account.ID})";
                    }
                    args.Player.SendSuccessMessage($"用户[{account.Name}]的相关信息:");
                    args.Player.SendSuccessMessage($"->用户组: {account.Name}");
                    args.Player.SendSuccessMessage($"->IP: {string.Join(',',accKnownIps)}");
                    args.Player.SendSuccessMessage($"->相关账号: \n");
                    if (!matches.Any())
                    {
                        args.Player.SendErrorMessage("没有查询到用户{0}的相关账号", username);
                    }
                    else
                    {
                        args.Player.SendInfoMessage(string.Join("\n", matches));
                    }

                    //Console.WriteLine(ipMatch);

                }
                else
                    args.Player.SendErrorMessage("用户{0}不存在", username);
            }
            else args.Player.SendErrorMessage("格式错误!正确格式:/cu <玩家名> [IP最少匹配端数]");
        }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(Command);

        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Commands.ChatCommands.Remove(Command);

            }
            base.Dispose(disposing);
        }

        public class Match
        {
            public Match(string name, bool uuid, int accID)
            {
                Name = name;
                UUID = uuid;
                AccID = accID;
            }

            public string Name;
            public int AccID;
            public List<string> IP = new();
            public bool UUID = false;
            public override string ToString()
            {
                StringBuilder stringBuilder = new StringBuilder();
                if (Name == "")
                {
                    Name = $"无名字(acc:{AccID})";
                }
                stringBuilder.AppendLine($"[{Name}]");
                if (UUID) 
                {
                    stringBuilder.AppendLine($"->[c/FF0000:设备相同(UUID)]");
                }
                if (IP.Any())
                {
                    stringBuilder.AppendLine($"->IP地址(相同或相似)");
                    foreach (string ip in IP)
                    {
                        stringBuilder.AppendLine($"-->{ip}");
                    }
                }
                return stringBuilder.ToString();
            }

        }
    }
}
