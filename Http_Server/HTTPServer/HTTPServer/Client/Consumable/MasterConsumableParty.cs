using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.Consumable
{
    public class MasterConsumableParty : IMasterParty
    {
        public MasterConsumableParty(string url) { darielURL = url; }
        private string darielURL;
        public async void SendMasterParty(ITimed_Client _httpClient, string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var data = buildMasterObject(connection, transaction);
                        if (data.Count > 0)
                        {
                            var response = await _httpClient.SendAsync(data, darielURL);

                            if (response.IsSuccessStatusCode)
                            {
                                UpdateSyncMasterTable(connection, transaction);
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
        public void UpdateSyncMasterTable(OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                connection.Open();
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
        public List<MasterOwnedPartyContract> buildMasterObject(OdbcConnection connection, OdbcTransaction transaction)
        {
            List<MasterOwnedPartyContract> ConsumablesUpdates = new List<MasterOwnedPartyContract>();
            try
            {
                connection.Open();
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] "
                            + "WHERE [Synced] = 0 AND "
                            + "	  [PartyType] = 'Consumables' "
                            + "GROUP BY PartyCode ";
                var command = new OdbcCommand(sql, connection);
                var reader = command.ExecuteReader();
                command.Transaction = transaction;
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        using (var connectionAcc = new OdbcConnection(connection.ConnectionString))
                        {
                            try
                            {
                                connectionAcc.Open();
                                string sqlAcc = "SELECT T1.*,  " +
                                                "       T2.[Account Name]" +
                                                "FROM [Consumables] T1 " +
                                                "   INNER JOIN [Customer] T2 ON " +
                                                "       T1.[Account No] = T2.[Account No] " +
                                                " WHERE [Delivery Address Code] = '" + reader["PartyCode"].ToString() + "'";
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                while (readerAcc.Read())
                                {
                                    MasterOwnedPartyContract Consumable = new MasterOwnedPartyContract();
                                    Consumable.ParentPartyCode = readerAcc["Account No"].ToString();
                                    Consumable.ParentPartyType = "Customer";
                                    Consumable.ParentPartyFullName = readerAcc["Account Name"].ToString();
                                    Consumable.PartyCode = readerAcc["Delivery Address Code"].ToString();
                                    Consumable.PartyType = "Consumable";
                                    Consumable.PartyFullName = readerAcc["Account Name"].ToString();
                                    Consumable.PartyPrimaryContactFullName = readerAcc["Consumables Contact Person"].ToString();
                                    Consumable.PartyPrimaryTelephoneNumber = readerAcc["Tel No For Consumables Contact Person"].ToString();
                                    Consumable.PartyPrimaryCellNumber = readerAcc["Cell No For Consumables Contact Person"].ToString();
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
    }
}
