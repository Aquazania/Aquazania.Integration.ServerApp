using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Text.RegularExpressions;
using System.Transactions;

namespace Aquazania.Integration.ServerApp.Client.UserExtension
{
    public class UserExtensionContract 
    {
        public UserExtensionContract(string url) { darielURL = url; }
        private string darielURL;
        public async Task SendMasterParty(ITimed_Client _httpClient, string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var data = buildMasterObject(connection, transaction, _DTS_connectionString);
                        if (data.Count > 0)
                        {
                            var response = await _httpClient.SendAsync(data, darielURL);
                            string message = await response.Content.ReadAsStringAsync();
                            if (response.IsSuccessStatusCode)
                            {
                                UpdateSyncMasterTable(connection, transaction);
                            }
                            else
                            {
                                LogUnsuccessfulRequest(_DTS_connectionString, data, response, message);
                            }
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        public List<UserContract> buildMasterObject(OdbcConnection connection, OdbcTransaction transaction, string _DTS_connectionString)
        {
            List<UserContract> userUpdates = new List<UserContract>();
            try
            {
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] T1 "
                            + "     INNER JOIN [User] T2 ON " 
                            + "   [PartyCode] = [User Name] "
                            + "WHERE T1.[Synced] = 0 AND "
                            + "	  T1.[PartyType] = 'User' AND "
                            + "   T2.[PBX Extension] IS NOT NULL "
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
                                string sqlAcc = "SELECT * " +
                                                "FROM [User] " +
                                                "WHERE [User Name] = '" + reader["PartyCode"].ToString() + "'";
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                while (readerAcc.Read())
                                {
                                    UserContract user = new UserContract();
                                    user.Surname = readerAcc["Surname"].ToString();
                                    user.UserName = readerAcc["User Name"].ToString();
                                    user.Extension = readerAcc["PBX Extension"].ToString();
                                    user.Name = readerAcc["First Name"].ToString();
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
                    return new List<UserContract>();
                }
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }

        public void LogUnsuccessfulRequest(string _DTS_connectionString, List<UserContract> payload, HttpResponseMessage response, string failedContracts)
        {
            using (var connectionAcc = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connectionAcc.Open();
                    string payloadJSON = JsonConvert.SerializeObject(payload);
                    string sql = "INSERT INTO  [Temp Failed Requests] ([Payload Sent] "
                               + "			   						  ,[Time Sent] "
                               + "			   						  ,[Dealt With] "
                               + "                                    ,[Party Type] "
                               + "                                    ,[Response] "
                               + "                                    ,[Response Detail])"
                               + ""
                               + "SELECT '" + payloadJSON + "', "
                               + "	     '" + DateTime.Now + "', "
                               + "	     0, "
                               + "       'User', "
                               + "       " + (int)response.StatusCode + ", "
                               + "       '" + failedContracts.Replace("'", "''") + "'";
                    var command = new OdbcCommand(sql, connectionAcc);
                    int rows = command.ExecuteNonQuery();
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }

        public void UpdateSyncMasterTable(OdbcConnection connection, OdbcTransaction transaction)
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
                            + "                    T2.[PBX Extension] IS NOT NULL "
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
    }
}
