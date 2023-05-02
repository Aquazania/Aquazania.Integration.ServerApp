using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Text.RegularExpressions;

namespace Aquazania.Integration.ServerApp.Client.User
{
    public class MasterUserParty : AbsMasterParty
    {
        public override void UpdateSyncMasterTable(OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                string sql = "UPDATE [Temp Master Party Contract] "
                            + "	SET [Synced] = 1 "
                            + "WHERE PartyType = 'User' AND "
                            + "	  PartyCode IN (SELECT PartyCode "
                            + "                 FROM [Temp Master Party Contract] T1 "
                            + "                      INNER JOIN [User] T2 ON "
                            + "                    [PartyCode] = [User Name] "
                            + "                 WHERE T1.[Synced] = 0 AND "
                            + "                 	  T1.[PartyType] = 'User' AND "
                            + "                    T2.[PBX Extension] IS NULL "
                            + "                 GROUP BY PartyCode)";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                int rows = command.ExecuteNonQuery();
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
        public override List<MasterOwnedPartyContract> buildMasterObject(OdbcConnection connection, OdbcTransaction transaction, string _DTS_connectionString)
        {
            List<MasterOwnedPartyContract> userUpdates = new List<MasterOwnedPartyContract>();
            try
            {
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] T1 "
                            + "     INNER JOIN [User] T2 ON "
                            + "   [PartyCode] = [User Name] "
                            + "WHERE T1.[Synced] = 0 AND "
                            + "	  T1.[PartyType] = 'User' AND "
                            + "   T2.[PBX Extension] IS NULL "
                            + "GROUP BY PartyCode ";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        using (var connectionAcc = new OdbcConnection(_DTS_connectionString))
                        {
                            try
                            {
                                connectionAcc.Open();
                                string sqlAcc = "SELECT T1.*, " +
                                                "       T2.[Account Name] " +
                                                "FROM [User] T1 " +
                                                "   LEFT JOIN [Customer] T2 ON " +
                                                "       T1.[Account No] = T2.[Account No] " +
                                                "WHERE [User Name] = '" + reader["PartyCode"].ToString() + "'";
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                while (readerAcc.Read())
                                {
                                    MasterOwnedPartyContract user = new MasterOwnedPartyContract();
                                    int accountNoIndex = readerAcc.GetOrdinal("Account No");
                                    if (!readerAcc.IsDBNull(accountNoIndex))
                                    {
                                        user.ParentPartyCode = readerAcc["Account No"].ToString();
                                        user.ParentPartyType = "Customer";
                                        user.ParentPartyFullName = readerAcc["Account Name"].ToString();
                                        user.AccountCode = readerAcc["Account No"].ToString();
                                        user.AccountName = readerAcc["Account Name"].ToString();
                                    }
                                    else
                                    {
                                        user.ParentPartyCode = null;
                                        user.ParentPartyType = null;
                                        user.ParentPartyFullName = null;
                                    }
                                    user.PartyCode = readerAcc["User Name"].ToString();
                                    user.PartyType = "User";
                                    user.PartyFullName = readerAcc["First Name"].ToString() + " " + readerAcc["Surname"].ToString();
                                    user.PartyPrimaryContactFullName = readerAcc["First Name"].ToString() + " " + readerAcc["Surname"].ToString();                                    
                                    user.PartyPrimaryTelephoneNumber = Regex.Replace(readerAcc["Telephone No"].ToString(), @"\D", "");                                    
                                    user.PartyPrimaryCellNumber = Regex.Replace(readerAcc["Cell Phone No"].ToString(), @"\D", "");
                                    user.IsActive = true;
                                    string filePath = @"C:\Tracking Folder\MasterParty.txt";
                                    using (StreamWriter writer = new StreamWriter(filePath, true))
                                    {
                                        writer.WriteLine();
                                    }
                                    File.AppendAllText(filePath, JsonConvert.SerializeObject(user + ",", Formatting.Indented));
                                    userUpdates.Add(user);
                                }
                            }
                            catch (OdbcException ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    return userUpdates;
                }
                else
                {
                    return new List<MasterOwnedPartyContract>();
                }
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
    }
}
