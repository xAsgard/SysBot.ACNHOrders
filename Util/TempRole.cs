using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SysBot.Base;

namespace SysBot.ACNHOrders
{
    [Serializable]
    public class TempRoleIdentifier<T>
    {
        public ulong discordid { get; set; }
        public int TravelCount { get; set; }
        public DateTime FirstOrderDate { get; set; }
        public DateTime LastOrderDate { get; set; }
        public string Username { get; set; }

        public TempRoleIdentifier(ulong userid, int travels, DateTime firstorder, DateTime lastorder, string name)
        {
            discordid = userid;
            TravelCount = travels;
            FirstOrderDate = firstorder;
            LastOrderDate = lastorder;
            Username = name;
        }

#pragma warning disable CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.
        public TempRoleIdentifier()
#pragma warning restore CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.
        {
        }

        public override string ToString() => JsonSerializer.Serialize(this, typeof(TempRoleIdentifier<T>));
        public static TempRoleIdentifier<T>? FromString(string s) => (TempRoleIdentifier<T>?)JsonSerializer.Deserialize(s, typeof(TempRoleIdentifier<T>));
    }

        public class TempRoleDetection<T>
        {
            private const string PathTemp = "tempusernew.txt";

            public List<TempRoleIdentifier<T>> TempRoleUsers { get; private set; } = new();

            public TempRoleDetection()
            {
                if (!File.Exists(PathTemp))
                {
                    var str = File.Create(PathTemp);
                    str.Close();
                }
                LoadAllUserInfo();
            }

            private void SaveAllUserInfo()
            {
                string[] toSave = new string[TempRoleUsers.Count];
                for (int i = 0; i < TempRoleUsers.Count; ++i)
                    toSave[i] = $"{TempRoleUsers[i]}\r\n";
                File.WriteAllLines(PathTemp, toSave);
            }

            private void LoadAllUserInfo()
            {
                TempRoleUsers.Clear();
                var txt = File.ReadAllText(PathTemp);
                var infos = txt.Split(new string[3] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var inf in infos)
                {
                    var ident = TempRoleIdentifier<T>.FromString(inf);
                    if (ident != null)
                        TempRoleUsers.Add(ident);
                }
            }

            /// <summary>
            /// Log Temporary User
            /// </summary>
            /// <returns>If temporary user, returns string with state!</returns>
            public async Task<string?> LogTempUserAsync(SocketUser trader, ulong userid, string player)
            {
                
                if (trader is SocketGuildUser socketUser && userid != default(ulong))
                {
                    SocketGuild socketGuild = socketUser.Guild;
                    ulong uTempRole = Convert.ToUInt64(Globals.Bot.Config.RoleUseBotTemp);
                    SocketRole socketRole = socketGuild.GetRole(uTempRole);
                    int TempAmount = Globals.Bot.Config.TempRoleAmount;

                    if (socketUser.Roles.Any(r => r.Id == socketRole.Id))
                    {
                        var exists = TempRoleUsers.FirstOrDefault(x => x.discordid != default(ulong) && x.discordid.Equals(userid) && (!x.Username.Contains("expired")));
                        if (exists == null)
                        {
                            TempRoleUsers.Add(new TempRoleIdentifier<T>(userid, 1, DateTime.Now, DateTime.Now, player));
                            SaveAllUserInfo();
                            LogUtil.LogInfo($"FirstTempOrder for {player}-{userid}.", Globals.Bot.Config.IP);
                            return $"**This is your first order with temporary role** - you have {TempAmount - 1} orders left.";
                        }

                        //exists = TempRoleUsers.FirstOrDefault(x => x.TempOver == "false" && x.Identity == id);
                        if (exists != default && exists.discordid != default(ulong) && exists.discordid.Equals(userid))
                        {
                            int TimesTraveled = exists.TravelCount + 1;
                            DateTime FirstOrder = exists.FirstOrderDate;
                            TempRoleUsers.Remove(exists);

                            if (TimesTraveled < TempAmount)
                            {
                                TempRoleUsers.Add(new TempRoleIdentifier<T>(userid, TimesTraveled, FirstOrder, DateTime.Now, player));
                                SaveAllUserInfo();
                                LogUtil.LogInfo($"TempRoleOrder updated for {player}-{userid}-TravelCount{TimesTraveled:00}.", Globals.Bot.Config.IP);
                                return $"**You are using the temporary role** - you have {TempAmount - TimesTraveled} order(s) left.";
                            }

                            if (TimesTraveled >= TempAmount)
                            {
                                TempRoleUsers.Add(new TempRoleIdentifier<T>(userid, TimesTraveled, FirstOrder, DateTime.Now, "expired-" + player));
                                SaveAllUserInfo();
                                LogUtil.LogInfo($"Pinging <@{ Globals.Self.Owner}>: TempRole will be removed for {player}-{userid}.", Globals.Bot.Config.IP);
                                await socketUser.RemoveRoleAsync(socketRole).ConfigureAwait(false);
                                return $"**Your temporary access has unfortunately now expired. Access to MerchantBot will disappear after this order.**";
                            }
                        }

                        LogUtil.LogInfo($"Pinging <@{ Globals.Self.Owner}>: Issue with TempRole for {player}-{userid}.", Globals.Bot.Config.IP);
                        return null;
                    }

                    return null;

                }

                LogUtil.LogInfo($"Pinging <@{ Globals.Self.Owner}>: Issue with SocketGuildUser {player}-{trader.Id}.", Globals.Bot.Config.IP);
                return null;
            }

            //public bool Remove(string id)
            //{
            //    if (string.IsNullOrWhiteSpace(id))
            //        return false;
            //    var exists = TempRoleUsers.FirstOrDefault(x => x.Identity.StartsWith(id));
            //    if (exists == default)
            //        return false;

            //    TempRoleUsers.Remove(exists);
            //    SaveAllUserInfo();
            //    return true;
            //}
        }

    public class NewTempRole : TempRoleDetection<uint>
    {
        private static NewTempRole? instance = null;
        public static NewTempRole Instance
        { get
            {
                if (instance == null)
                    instance = new();
                return instance;
            }
        }
        public NewTempRole() : base() { }
    }

    }

