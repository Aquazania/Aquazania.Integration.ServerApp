using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Text.RegularExpressions;

namespace Aquazania.Integration.ServerApp.Client.Consumable
{
    public class MasterConsumableParty : IMasterParty
    {
        public MasterConsumableParty(string url) { darielURL = url; }
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
                            + "WHERE PartyType = 'Consumables' AND "
                            + "	  PartyCode IN (SELECT PartyCode "
                            + "					FROM [Temp Master Party Contract] "
                            + "					WHERE [Synced] = 0 AND "
                            + "						  [PartyType] = 'Consumables' "
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
            List<MasterOwnedPartyContract> ConsumablesUpdates = new List<MasterOwnedPartyContract>();
            try
            {
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] "
                            + "WHERE [Synced] = 0 AND "
                            + "	  [PartyType] = 'Consumables' "
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
                                string sqlAcc = "SELECT T1.*,  " +
                                                "       T2.[Delivery Address Line 2], " +
                                                "       T2.[Delivery Address Line 3], " +
                                                "       T3.[Account Name]" +
                                                "FROM [Consumables] T1 " +
                                                "   INNER JOIN [Delivery Address] T2 ON " +
                                                "       T1.[Delivery Address Code] = T2.[Delivery Address Code] " +
                                                "   INNER JOIN [Customer] T3 ON " +
                                                "       T2.[Account No] = T3.[Account No] " +
                                                " WHERE T1.[Delivery Address Code] = '" + reader["PartyCode"].ToString() + "'";
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                while (readerAcc.Read())
                                {
                                    MasterOwnedPartyContract Consumable = new MasterOwnedPartyContract();
                                    Consumable.ParentPartyCode = readerAcc["Delivery Address Code"].ToString();
                                    Consumable.ParentPartyType = "DeliveryAddress";
                                    int accountNoIndex = readerAcc.GetOrdinal("Account No");
                                    if (!readerAcc.IsDBNull(accountNoIndex))
                                    {
                                        Consumable.AccountCode = readerAcc["Account No"].ToString();
                                        Consumable.AccountName = readerAcc["Account Name"].ToString();
                                    }
                                    Consumable.ParentPartyFullName = readerAcc["Delivery Address Line 2"].ToString() + " " + readerAcc["Delivery Address Line 3"].ToString();
                                    Consumable.PartyCode = readerAcc["Delivery Address Code"].ToString();
                                    Consumable.PartyType = "Consumable";
                                    Consumable.PartyFullName = readerAcc["Delivery Address Line 2"].ToString() + " " + readerAcc["Delivery Address Line 3"].ToString();
                                    Consumable.PartyPrimaryContactFullName = readerAcc["Consumables Contact Person"].ToString();
                                    Consumable.PartyPrimaryTelephoneNumber = Regex.Replace(readerAcc["Tel No For Consumables Contact Person"].ToString(), @"\D", "");
                                    Consumable.PartyPrimaryCellNumber = Regex.Replace(readerAcc["Cell No For Consumables Contact Person"].ToString(), @"\D", "");
                                    Consumable.IsActive = true;
                                    ConsumablesUpdates.Add(Consumable);
                                }
                            }
                            catch (OdbcException ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    return ConsumablesUpdates;
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
                               + "       'Consumables', "
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
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
    }
}
