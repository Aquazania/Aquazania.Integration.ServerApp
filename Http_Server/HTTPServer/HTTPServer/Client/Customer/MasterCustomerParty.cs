using Aquazania.Integration.ServerApp.Client;
using Aquazania.Telephony.Integration.Models;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Transactions;

namespace HTTPServer.Client.Customer
{
    public class MasterCustomerParty : IMasterParty
    {
        public MasterCustomerParty(string url) { darielURL = url; }
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
                            DarielResponse result = JsonConvert.DeserializeObject<DarielResponse>(message);
                            UpdateSyncMasterTable(connection, transaction);
                            transaction.Commit();
                            if (result.NumberOfFailures > 0)
                                LogUnsuccessfulRequest(_DTS_connectionString, data, response, message, result);
                        }

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        public void UpdateSyncMasterTable(OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                string sql = "UPDATE [Temp Master Party Contract] "
                            + "	SET [Synced] = 1 "
                            + "WHERE PartyType = 'Customer' AND "
                            + "	  PartyCode IN (SELECT PartyCode "
                            + "					FROM [Temp Master Party Contract] "
                            + "					WHERE [Synced] = 0 AND "
                            + "						  [PartyType] = 'Customer' "
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
        public List<MasterOwnedPartyContract> buildMasterObject(OdbcConnection connection, OdbcTransaction transaction, string _DTS_connectionString)
        {
            List<MasterOwnedPartyContract> customerUpdates = new List<MasterOwnedPartyContract>();
            try
            {
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] "
                            + "WHERE [Synced] = 0 AND "
                            + "	  [PartyType] = 'Customer' "
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
                                string sqlAcc = "SELECT * FROM [Customer] WHERE [Account No] = '" + reader["PartyCode"].ToString() + "'";
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                while (readerAcc.Read())
                                {
                                    MasterOwnedPartyContract customer = new MasterOwnedPartyContract();
                                    customer.ParentPartyCode = null;
                                    customer.ParentPartyType = null;
                                    customer.ParentPartyFullName = null;
                                    customer.PartyCode = readerAcc["Account No"].ToString();
                                    customer.PartyType = "Customer";
                                    customer.AccountCode = readerAcc["Account No"].ToString();
                                    customer.AccountName = readerAcc["Account Name"].ToString();
                                    customer.PartyFullName = readerAcc["Account Name"].ToString();
                                    customer.PartyPrimaryContactFullName = readerAcc["Creditors Clerk"].ToString();
                                    customer.PartyPrimaryTelephoneNumber = Regex.Replace(readerAcc["Telephone No"].ToString(), @"\D", "");
                                    customer.PartyPrimaryCellNumber = Regex.Replace(readerAcc["Cell Phone No"].ToString(), @"\D", "");
                                    customer.IsActive = true;
                                    string filePath = @"C:\Tracking Folder\MasterParty.txt";
                                    using (StreamWriter writer = new StreamWriter(filePath, true))
                                    {
                                        writer.WriteLine();
                                    }
                                    File.AppendAllText(filePath, JsonConvert.SerializeObject(customer, Formatting.Indented));
                                    customerUpdates.Add(customer); 
                                }
                            }
                            catch (OdbcException ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    return customerUpdates;
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
        public void LogUnsuccessfulRequest(string _DTS_connectionString, List<MasterOwnedPartyContract> payload, HttpResponseMessage response, string failedContracts, DarielResponse message)
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
                               + "SELECT '" + payloadJSON.Replace("'", "''") + "', "
                               + "	     '" + DateTime.Now + "', "
                               + "	     0, "
                               + "       'Customer', "
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
                                               "WHERE EntryNo = ( " +
                                               "    SELECT MAX(EntryNo) " +
                                               "    FROM [Temp Master Party Contract] " +
                                               "    WHERE PartyCode = '" + accountno + "')";
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
    }
}
