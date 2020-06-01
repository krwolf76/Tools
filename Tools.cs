using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core;
using System;
using System.Text.RegularExpressions;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Tools", "KR_WOLF", "1.0.2")]
    [Description("KR_WOLF#5912")]
    class Tools : RustPlugin
    {
        //서버를 간단하게 수정할수있습니다.
        //https://github.com/krwolf76/Tools

        private Configuration _config;

        void OnServerInitialized(bool serverInitialized)
        {
            permission.RegisterPermission("tools.bypass", this);

            if (_config.ServerTitleToggle == true)
            {
                ConsoleSystem.Run(ConsoleSystem.Option.Unrestricted, ($"server.hostname \"{_config.ServerTitle}\""));
            }
            if (_config.ServerDescriptionToggle == true)
            {
                ConsoleSystem.Run(ConsoleSystem.Option.Unrestricted, ($"server.description \"{_config.ServerDescription}\""));
            }
            if (_config.ServerURLToggle == true)
            {
                ConsoleSystem.Run(ConsoleSystem.Option.Unrestricted, ($"server.url \"{_config.ServerURL}\""));
            }
            if (_config.ServerImageToggle == true)
            {
                ConsoleSystem.Run(ConsoleSystem.Option.Unrestricted, ($"server.headerimage \"{_config.ServerImage}\""));
            }
        }

        void Loaded()
        {
            User = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<String, PlayerDatas>>("Tools_PlayerData");

            foreach (BasePlayer basePlayer in BasePlayer.activePlayerList)
            {
                var ip = basePlayer.net.connection.ipaddress;
                ip = ip.Substring(0, ip.LastIndexOf(':'));
                String userId = basePlayer.UserIDString;
                if (!User.ContainsKey(userId))
                {
                    PlayerDatas data = new PlayerDatas();

                    data.DisplayName = new List<string>()
                    {
                        basePlayer.displayName
                    };
                    data.IP = new List<string>()
                    {
                        ip
                    };
                    data.Dis_ConnectTime = new List<string>()
                    {
                        $"처음 접속 | {DateTime.Now} | {ip} | {basePlayer.displayName} | {basePlayer.UserIDString}"
                    };
                    data.ConnectCount = 1;
                    data.PlayerID = basePlayer.UserIDString;

                    User.Add(userId, data);
                }
                else
                {
                    if (!User[basePlayer.UserIDString].DisplayName.Contains(basePlayer.displayName))
                    {
                        User[basePlayer.UserIDString].DisplayName.Add(basePlayer.displayName);
                    }
                    if (!User[basePlayer.UserIDString].IP.Contains(ip))
                    {
                        User[basePlayer.UserIDString].IP.Add(ip);
                    }
                }
            }

            

            DataSave();
        }
        private void OnPlayerConnected(BasePlayer player)
        {
            var ip = player.net.connection.ipaddress;
            ip = ip.Substring(0, ip.LastIndexOf(':'));
            String userId = player.UserIDString;
            if (!User.ContainsKey(userId))
            {
                PlayerDatas data = new PlayerDatas();

                data.DisplayName = new List<string>()
                {
                    player.displayName
                };
                data.IP = new List<string>()
                {
                    ip
                };
                data.Dis_ConnectTime = new List<string>()
                {
                    $"처음 접속 | {DateTime.Now} | {ip} | {player.displayName} | {player.UserIDString}"
                };
                data.ConnectCount = 1;
                data.PlayerID = player.UserIDString;

                User.Add(userId, data);
            }
            else
            {
                if(!User[player.UserIDString].DisplayName.Contains(player.displayName))
                {
                    User[player.UserIDString].DisplayName.Add(player.displayName);
                }
                if(!User[player.UserIDString].IP.Contains(ip))
                {
                    User[player.UserIDString].IP.Add(ip);
                }
                User[player.UserIDString].Dis_ConnectTime.Add($"접속 | {DateTime.Now}");
                
                User[player.UserIDString].ConnectCount += 1;
            }

            if (_config.ServerJoinMessage == true)
            {
                if(permission.UserHasPermission(player.UserIDString, "tools.bypass"))
                    return;

                var playerAddress = player.net.connection.ipaddress.Split(':')[0];

                webrequest.Enqueue("http://ip-api.com/json/" + playerAddress, null, (code, response) =>
                {
                    if (code != 200 || response == null)
                    {
                        Server.Broadcast(_config.Prefix + Lang("입장알수없음", null, player.displayName), player.userID);

                        if (_config.PrintToConsole)
                            Puts(StripRichText(Lang("입장알수없음", null, player.displayName)));

                        return;
                    }

                    var country = JsonConvert.DeserializeObject<Response>(response).Country;

                    Server.Broadcast(_config.Prefix + Lang("입장", null, player.displayName, country), player.userID);

                    if (_config.PrintToConsole)
                        Puts(StripRichText(Lang("입장", null, player.displayName, country)));

                }, this);
            }

            DataSave();
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var ip = player.net.connection.ipaddress;
            ip = ip.Substring(0, ip.LastIndexOf(':'));
            String userId = player.UserIDString;
            if (!User.ContainsKey(userId))
            {
                PlayerDatas data = new PlayerDatas();

                data.Dis_ConnectTime = new List<string>()
                {
                    $"퇴장 | {DateTime.Now}"
                };

                User.Add(userId, data);
            }
            else
            {
                User[player.UserIDString].Dis_ConnectTime.Add($"퇴장 | {DateTime.Now}");
            }

            if (_config.ServerLeaveMessage == true)
            {
                if (permission.UserHasPermission(player.UserIDString, "tools.bypass"))
                {
                    return;
                }
                else
                {
                    Server.Broadcast($"{_config.Prefix} {string.Format(Lang("퇴장", null, player.displayName, reason), player.userID)}");
                }
                
            }
            DataSave();
        }

        #region Config
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(_config);

        private class Configuration
        {
            [JsonProperty("0. true - 활성화 / false - 비활성화")]
            public string Notting { get; set; } = "이부분은 건들지마시오!";

            [JsonProperty("0. 플러그인 칭호 설정")]
            public string Prefix { get; set; } = "<color=#00ffff>[ 알림 ] </color>";

            [JsonProperty("0. 콘솔 로그")]
            public bool PrintToConsole { get; set; } = true;

            [JsonProperty("1. 서버 제목 설정 ON/OFF")]
            public bool ServerTitleToggle { get; set; } = false;

            [JsonProperty("1. 서버 제목 설정")]
            public string ServerTitle { get; set; } = "서버 제목을 적어주세요.";

            [JsonProperty("2. 서버 URL 설정 ON/OFF")]
            public bool ServerURLToggle { get; set; } = false;

            [JsonProperty("2. 서버 URL 설정 (URL 링크만)")]
            public string ServerURL { get; set; } = "https://discord.gg/XUEHw7M";

            [JsonProperty("3. 서버 설명 ON/OFF")]
            public bool ServerDescriptionToggle { get; set; } = false;

            [JsonProperty("3. 서버 설명 (서버 설명할만한걸 적으면된다.)")]
            public string ServerDescription { get; set; } = "여기는 무슨서버이다~ 설명글!\n엉덩이 팡팡!";

            [JsonProperty("4. 서버 이미지(로고) 설정 ON/OFF")]
            public bool ServerImageToggle { get; set; } = false;

            [JsonProperty("4. 서버 이미지(로고) 설정 (이미지링크)")]
            public string ServerImage { get; set; } = "https://i.imgur.com/mHpQINh.png";

            [JsonProperty("5. 서버 처음 접속 입장 알림 ON/OFF")]
            public bool ServerFirstJoinMessage { get; set; } = false;

            [JsonProperty("6. 서버 접속 입장 알림 ON/OFF")]
            public bool ServerJoinMessage { get; set; } = false;

            [JsonProperty("7. 서버 퇴장 입장 알림 ON/OFF")]
            public bool ServerLeaveMessage { get; set; } = false;

            [JsonProperty("8. 서버 규칙 알림 ON/OFF")]
            public bool ServerRulesMessage { get; set; } = false;
        }
        #endregion

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["처음입장"] = "{0}님이 서버에 처음 접속하셨습니다. [국가: {1}]",
                ["입장"] = "{0} 님은 서버에 접속하셨습니다. [국가: {1}]",
                ["입장알수없음"] = "{0}님은 서버에 접속하셨습니다. [국가: 알수없음]",
                ["퇴장"] = "{0} 님은 서버에서 퇴장하셨습니다. [이유: {1}]",
                ["권한"] = "<color=red>당신은 권한이 없습니다.</color>",
                ["규칙"] = "{0}님 서버에 접속하셨습니다. 규칙 읽어주세요.\n1. 핵 사용 금지\n2. 욕설/비하 금지\n3. 서로 존중하기"

            }, this);
        }

        private string Lang(string key, string id = null, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, id), args);
        }

        private string StripRichText(string text)
        {
            var stringReplacements = new string[]
            {
                "<b>", "</b>",
                "<i>", "</i>",
                "</size>",
                "</color>"
            };

            var regexReplacements = new Regex[]
            {
                new Regex(@"<color=.+?>"),
                new Regex(@"<size=.+?>"),
            };

            foreach (var replacement in stringReplacements)
                text = text.Replace(replacement, string.Empty);

            foreach (var replacement in regexReplacements)
                text = replacement.Replace(text, string.Empty);

            return Formatter.ToPlaintext(text);
        }

        class Response
        {
            [JsonProperty("country")]
            public string Country { get; set; }
        }
        #endregion

        #region Data
        Dictionary<String, PlayerDatas> User = new Dictionary<String, PlayerDatas>();
        class PlayerDatas
        {
            public List<string> DisplayName { get; set; }
            public List<string> IP { get; set; }
            public List<string> Dis_ConnectTime { get; set; }
            public string PlayerID { get; set; }
            public int ConnectCount { get; set; }
        }

        private void DataSave()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Tools_PlayerData", User);
        }
        #endregion
    }
}
