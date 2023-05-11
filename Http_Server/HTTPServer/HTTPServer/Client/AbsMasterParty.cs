using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Transactions;

namespace Aquazania.Integration.ServerApp.Client
{
    public abstract class AbsMasterParty : IMasterParty
    {
        public async Task SendMasterParty(ITimed_Client _httpClient, string _DTS_connectionString, string darielURL)
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
                            DarielResponse result = JsonConvert.DeserializeObject<DarielResponse>(message);
                            UpdateSyncMasterTable(connection, transaction);
                            transaction.Commit();
                            if (result.NumberOfFailures > 0)
                                LogUnsuccessfulRequest(_DTS_connectionString, data, response, message, result);
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
        public void LogUnsuccessfulRequest(string _DTS_connectionString, List<MasterOwnedPartyContract> payload, HttpResponseMessage response, string failedContracts, DarielResponse message)
        {
            using (var connectionAcc = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connectionAcc.Open();
                    string payloadJSON = JsonConvert.SerializeObject(payload);
                    if (!payloadJSON.Equals(null))
                    {
                        string partytype = payload[0].PartyType;
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
                        _ = command.ExecuteNonQuery();
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
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        public abstract void UpdateSyncMasterTable(OdbcConnection connection, OdbcTransaction transaction);
        public abstract List<MasterOwnedPartyContract> buildMasterObject(OdbcConnection connection, OdbcTransaction transaction, string _DTS_connectionString);
    }
}
