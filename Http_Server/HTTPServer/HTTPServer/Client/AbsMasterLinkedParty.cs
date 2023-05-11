using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Transactions;

namespace Aquazania.Integration.ServerApp.Client
{
    public abstract class AbsMasterLinkedParty : IMasterLinkedParty
    {
        public async Task SendMasterLinkedParty(ITimed_Client _httpClient, string _COM_connectionString, string _DTS_connectionString, string darielURL)
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
                            DarielResponse result = JsonConvert.DeserializeObject<DarielResponse>(message);
                            UpdateSyncLinkMasterTable(connection, transaction);
                            transaction.Commit();
                            if (result.NumberOfFailures > 0)
                                LogUnsuccessfulRequest(data, response, message, _COM_connectionString, result);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Transaction.Current != null)
                            transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        public void LogUnsuccessfulRequest(List<MasterOwnedLinkedContactContract> payload, HttpResponseMessage response, string failedContracts, string _COM_connectionString, DarielResponse message)
        {
            using (var connectionAcc = new OdbcConnection(_COM_connectionString))
            {
                try
                {
                    connectionAcc.Open();
                    string payloadJSON = JsonConvert.SerializeObject(payload);
                    string partytype = payload[0].ParentPartyType;
                    string sql = "INSERT INTO  [Temp Failed Requests] ([Payload Sent] "
                               + "			   						  ,[Time Sent] "
                               + "			   						  ,[Dealt With] "
                               + "                                    ,[Party Type] "
                               + "                                    ,[Response] "
                               + "                                    ,[Response Detail])"
                               + ""
                               + "SELECT '" + payloadJSON.Replace("'", "''") + "', "
                               + "	     '" + DateTime.Now + "', "
                               + "	     0, "
                               + "       '" + partytype + "', "
                               + "       " + (int)response.StatusCode + ", "
                               + "       '" + failedContracts.Replace("'", "''") + "'";
                    var command = new OdbcCommand(sql, connectionAcc);
                    int rows = command.ExecuteNonQuery();

                    foreach (var error in message.errors)
                    {
                        string errormessage = error.ToString();
                        int firstBracketIndex = errormessage.IndexOf('[');
                        int secondBracketIndex = errormessage.IndexOf('[', firstBracketIndex + 1);
                        int secondBracketEndIndex = errormessage.IndexOf(']', secondBracketIndex + 1);

                        if (firstBracketIndex != -1 && secondBracketIndex != -1 && secondBracketEndIndex != -1)
                        {
                            string accountno = errormessage.Substring(secondBracketIndex + 1, secondBracketEndIndex - secondBracketIndex - 1);
                            string sqlupdate = "UPDATE [Temp Master Party Contract] " +
                                               "	SET Synced = 0 " +
                                               "WHERE EntryNo = (SELECT MAX(EntryNo) " +
                                               "                 FROM [Temp Master Party Contract] " +
                                               "                 WHERE PartyCode = '" + accountno + "')";
                            var command1 = new OdbcCommand(sqlupdate, connectionAcc);
                            _ = command1.ExecuteNonQuery();
                        }
                    }
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        public abstract List<MasterOwnedLinkedContactContract> buildMasterLinkObject(OdbcConnection connection, OdbcTransaction transaction, string _COM_connectionString, string _DTS_connectionString);
        public abstract void UpdateSyncLinkMasterTable(OdbcConnection connection, OdbcTransaction transaction);
    }
}
