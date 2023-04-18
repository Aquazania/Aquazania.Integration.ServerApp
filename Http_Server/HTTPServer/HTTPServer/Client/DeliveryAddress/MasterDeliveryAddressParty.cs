using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.DeliveryAddress
{

    public class MasterDeliveryAddressParty : IMasterParty
    {
        public MasterDeliveryAddressParty(string url) { darielURL = url; }
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
                            + "WHERE PartyType = 'DeliveryAddress' AND "
                            + "	  PartyCode IN (SELECT PartyCode "
                            + "					FROM [Temp Master Party Contract] "
                            + "					WHERE [Synced] = 0 AND "
                            + "						  [PartyType] = 'DeliveryAddress' "
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
            List<MasterOwnedPartyContract> DeliveryAddressUpdates = new List<MasterOwnedPartyContract>();
            try
            {
                connection.Open();
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] "
                            + "WHERE [Synced] = 0 AND "
                            + "	  [PartyType] = 'DeliveryAddress' "
                            + "GROUP BY PartyCode ";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                var reader = command.ExecuteReader();
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
                                                "FROM [Delivery Address] T1 " +
                                                "   INNER JOIN [Customer] T2 ON " +
                                                "       T1.[Account No] = T2.[Account No] " +
                                                " WHERE [Delivery Address Code] = '" + reader["PartyCode"].ToString() + "'";
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                while (readerAcc.Read())
                                {
                                    MasterOwnedPartyContract DeliveryAddress = new MasterOwnedPartyContract();
                                    DeliveryAddress.ParentPartyCode = readerAcc["Account No"].ToString();
                                    DeliveryAddress.ParentPartyType = "Customer";
                                    DeliveryAddress.ParentPartyFullName = readerAcc["Account Name"].ToString();
                                    DeliveryAddress.PartyCode = readerAcc["Delivery Address Code"].ToString();
                                    DeliveryAddress.PartyType = "DeliveryAddress";
                                    DeliveryAddress.PartyFullName = readerAcc["Delivery Address Line 2"].ToString() + " " + readerAcc["Delivery Address Line 3"].ToString();
                                    DeliveryAddress.PartyPrimaryContactFullName = readerAcc["Contact Person"].ToString();
                                    DeliveryAddress.PartyPrimaryTelephoneNumber = readerAcc["Tel No For Contact Person"].ToString();
                                    DeliveryAddress.PartyPrimaryCellNumber = readerAcc["Cell No For Contact Person"].ToString();
                                    DeliveryAddress.IsActive = true;
                                    DeliveryAddressUpdates.Add(DeliveryAddress);
                                }
                            }
                            catch (OdbcException ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    return DeliveryAddressUpdates;
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
