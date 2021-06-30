using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SysBot.Base;

namespace SysBot.ACNHOrders
{
    public class TempRoleIdentifier
    {
        public readonly string FirstOrderDate;
        public readonly string LastOrderDate;
        public readonly string TempOver;
        public readonly string Identity;

        public TempRoleIdentifier(string OrderDate, string RemovedDate, string TempEnded, string id)
        {
            FirstOrderDate = OrderDate;
            LastOrderDate = RemovedDate;
            TempOver = TempEnded;
            Identity = id;
        }

        public override string ToString()
        {
            return $"{FirstOrderDate},{LastOrderDate},{TempOver},{Identity}";
        }

        public static TempRoleIdentifier? FromString(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            var splits = s.Split(',');
            if (splits.Length != 4)
                return null;

            return new TempRoleIdentifier(splits[0], splits[1], splits[2], splits[3]);
        }

    }

    public class TempRole
    {
        private const string PathTemp = "tempuser.txt";

        public List<TempRoleIdentifier> TempRoleUsers { get; private set; } = new();

        public static TempRole CurrentInstance = new();

        public TempRole()
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
            var infos = txt.Split(new string[1] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var inf in infos)
            {
                var ident = TempRoleIdentifier.FromString(inf);
                if (ident != null)
                    TempRoleUsers.Add(ident);
            }
        }

        /// <summary>
        /// Log Temporary User
        /// </summary>
        /// <returns>If temporary user, returns string with state!</returns>
        public async System.Threading.Tasks.Task<string?> LogTempUserAsync(SocketUser trader, string player)
        {
            if (trader is SocketGuildUser socketUser)
            {
                SocketGuild socketGuild = socketUser.Guild;
                ulong uTempRole = Convert.ToUInt64(Globals.Bot.Config.RoleUseBotTemp);
                SocketRole socketRole = socketGuild.GetRole(uTempRole);

                if (socketUser.Roles.Any(r => r.Id == socketRole.Id))
                {
                    Double dTempRoleTime = Convert.ToDouble(Globals.Bot.Config.TempRoleTime);
                    DateTime TempEnd = DateTime.Now.AddSeconds(dTempRoleTime);
                    string id = trader.Id.ToString();

                    var exists = TempRoleUsers.FirstOrDefault(x => x.TempOver == "false" && x.Identity == id);
                    if (exists == default)
                    {
                        TempRoleUsers.Add(new TempRoleIdentifier(DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss"), DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss"), "false", id));
                        SaveAllUserInfo();
                        LogUtil.LogInfo($"FirstTempOrder for {player}-{id}.", Globals.Bot.Config.IP);
                        return $"Your temporary access was activated and will end with your last order after {TempEnd:yyyy-MM-dd hh:mm:ss tt}.";
                    }

                    //exists = TempRoleUsers.FirstOrDefault(x => x.TempOver == "false" && x.Identity == id);
                    if (exists != default && (exists.Identity == id && exists.TempOver == "false"))
                    {
                        DateTime eFirstOrder = DateTime.ParseExact(exists.FirstOrderDate, "yyyy-dd-M--HH-mm-ss", null);
                        DateTime eLastOrder = DateTime.ParseExact(exists.LastOrderDate, "yyyy-dd-M--HH-mm-ss", null);
                        TempRoleUsers.Remove(exists);

                        if (DateTime.Now < eFirstOrder.AddSeconds(dTempRoleTime))
                        {
                            TempRoleUsers.Add(new TempRoleIdentifier(eFirstOrder.ToString("yyyy-dd-M--HH-mm-ss"), DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss"), "false", id));
                            SaveAllUserInfo();
                            LogUtil.LogInfo($"TempRoleOrder updated for {player}-{id}.", Globals.Bot.Config.IP);
                            return $"Your temporary access will end with your last order after {TempEnd:yyyy-MM-dd hh:mm:ss tt}.";
                        }

                        if (DateTime.Now >= eFirstOrder.AddSeconds(dTempRoleTime))
                        {
                            TempRoleUsers.Add(new TempRoleIdentifier(eFirstOrder.ToString("yyyy-dd-M--HH-mm-ss"), DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss"), "true", id));
                            SaveAllUserInfo();
                            LogUtil.LogInfo($"Pinging <@{ Globals.Self.Owner}>: TempRole will be removed for {player}-{id}.", Globals.Bot.Config.IP);
                            await socketUser.RemoveRoleAsync(socketRole).ConfigureAwait(false);
                            return $"**Your temporary access has unfortunately now expired. Access to MerchantBot will disappear after this order.**";
                        }
                    }

                    LogUtil.LogInfo($"Pinging <@{ Globals.Self.Owner}>: Issue with TempRole for {player}-{id}.", Globals.Bot.Config.IP);
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
}
