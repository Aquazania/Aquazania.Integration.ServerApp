using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Text.RegularExpressions;

namespace Aquazania.Integration.ServerApp.Client.User
{
    public class MasterUserLinkedParty : IMasterLinkedParty
    {
        public MasterUserLinkedParty(string url) { darielURL = url; }
        private string darielURL;
        public async Task SendMasterLinkedParty(ITimed_Client _httpClient, string _COM_connectionString, string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var data = buildMasterLinkObject(connection, transaction, _COM_connectionString, _DTS_connectionString);
                        if (data.Count > 0)
                        {
                            var response = await _httpClient.SendAsync(data, darielURL);
                            string message = await response.Content.ReadAsStringAsync();
                            if (response.IsSuccessStatusCode)
                            {
                                UpdateSyncLinkMasterTable(connection, transaction);
                            }
                            else
                            {
                                LogUnsuccessfulRequest(data, response, message, _COM_connectionString);
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
        public void UpdateSyncLinkMasterTable(OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                string sql = "UPDATE [Temp Master Party Contract] "
                            + "	SET [Synced] = 1 "
                            + "WHERE PartyType = 'User' AND "
                            + "	  PartyCode IN (SELECT PartyCode "
                            + "					FROM [Temp Master Party Contract] "
                            + "					WHERE [Synced] = 0 AND "
                            + "						  [PartyType] = 'User' "
                            + "					GROUP BY PartyCode) ";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                int rows = command.ExecuteNonQuery();
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
        public List<MasterOwnedLinkedContactContract> buildMasterLinkObject(OdbcConnection connection, OdbcTransaction transaction, string _COM_connectionString, string _DTS_connectionString)
        {
            List<MasterOwnedLinkedContactContract> userUpdates = new List<MasterOwnedLinkedContactContract>();
            try
            {
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] "
                            + "WHERE [Synced] = 0 AND "
                            + "	    [PartyType] = 'User' "
                            + "GROUP BY PartyCode ";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        using (var connectionAcc = new OdbcConnection(_COM_connectionString))
                        {
                            try
                            {
                                connectionAcc.Open();
                                string sqlAcc = "SELECT * " +
                                                "FROM [viewContactDocumentReference] " +
                                                "WHERE [DocumentReferenceCode] = '" + reader["PartyCode"].ToString() + "'" +
                                                "  AND [ContactPointTypeID] = 2 ";
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                string prevAccountNo = null;
                                string accName = null;
                                string accNo = null;
                                while (readerAcc.Read())
                                {
                                    MasterOwnedLinkedContactContract user = new MasterOwnedLinkedContactContract();
                                    string curAccountNo = readerAcc["DocumentReferenceCode"].ToString();
                                    if (prevAccountNo != curAccountNo)
                                    {
                                        using (var connectionAccountInfo = new OdbcConnection(_DTS_connectionString))
                                        {
                                            try
                                            {
                                                string sqlAccInfo = "SELECT * FROM [User] WHERE [User Name] = '" + readerAcc["DocumentReferenceCode"].ToString() + "'";
                                                connectionAccountInfo.Open();
                                                var commandAccInfo = new OdbcCommand(sqlAccInfo, connectionAccountInfo);
                                                var readerAccInfo = commandAccInfo.ExecuteReader();
                                                if (readerAccInfo.HasRows)
                                                {
                                                    while (readerAccInfo.Read())
                                                    {
                                                        int accountNoIndex = readerAccInfo.GetOrdinal("Account No");
                                                        if (!readerAccInfo.IsDBNull(accountNoIndex))
                                                        {
                                                            user.AccountCode = readerAccInfo["Account No"].ToString();
                                                            user.AccountName = readerAccInfo["Account Name"].ToString();
                                                            accNo = readerAccInfo["Account No"].ToString();
                                                            accName = readerAccInfo["Account Name"].ToString();
                                                        }
                                                    }
                                                }
                                                else
                                                { user.AccountName = null; user.AccountCode = null; }
                                            }
                                            catch (OdbcException ex) { throw ex; }
                                        }
                                    }
                                    else
                                    { user.AccountCode = accNo; user.AccountName = accName; }
                                    user.ParentPartyCode = readerAcc["DocumentReferenceCode"].ToString();
                                    user.ParentPartyType = "User";
                                    user.ContactFullName = readerAcc["ContactName"].ToString() + " " + (!readerAcc.IsDBNull(readerAcc.GetOrdinal("ContactLastName")) ? readerAcc["ContactLastName"].ToString() : "");                                    
                                    user.PhoneNumber = Regex.Replace(readerAcc["ContactPointValue"].ToString(), @"\D", "");
                                    user.IsActive = true;
                                    userUpdates.Add(user);
                                    prevAccountNo = curAccountNo;
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
                    return new List<MasterOwnedLinkedContactContract>();
                }
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
        public void LogUnsuccessfulRequest(List<MasterOwnedLinkedContactContract> payload, HttpResponseMessage response, string failedContracts, string _COM_connectionString)
        {
            using (var connectionAcc = new OdbcConnection(_COM_connectionString))
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
    }
}
