using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.DeliveryAddress
{
    public class MasterDeliveryAddressParty
    {
        public MasterDeliveryAddressParty(string url) { darielURL = url; }
        private string darielURL;
        public async void SendMasterDeliveryAddressParty(ITimed_Client _httpClient, string _DTS_connectionString)
        {
            var data = buildMasterDeliveryAddressObject(_DTS_connectionString);
            if (data.Count > 0)
            {
                var response = await _httpClient.SendAsync(data, darielURL);

                if (response.IsSuccessStatusCode)
                {
                    UpdateSyncDeliveryAddressMasterTable(_DTS_connectionString);
                }
            }
        }
        private void UpdateSyncDeliveryAddressMasterTable(string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
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
                    int rows = command.ExecuteNonQuery();
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        private List<MasterOwnedPartyContract> buildMasterDeliveryAddressObject(string _DTS_connectionString)
        {
            List<MasterOwnedPartyContract> DeliveryAddressUpdates = new List<MasterOwnedPartyContract>();
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT PartyCode "
                               + "FROM [Temp Master Party Contract] "
                               + "WHERE [Synced] = 0 AND "
                               + "	  [PartyType] = 'DeliveryAddress' "
                               + "GROUP BY PartyCode ";
                    var command = new OdbcCommand(sql, connection);
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
                                                    "FROM [Delivery Address] T1 " +
                                                    "   INNER JOIN [Customer] T2 ON " +
                                                    "       T1.[Account No] = T2.[Account No] " +
                                                    " WHERE [Delivery Address Code] = '" + reader["PartyCode"].ToString() + "'";
                                    var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                    var readerAcc = commandAcc.ExecuteReader();
                                    while (readerAcc.Read())
                                    {
                                        MasterOwnedPartyContract DeliveryAddress = new MasterOwnedPartyContract();
                                        DeliveryAddress.ParentPartyCode = reader["Account No"].ToString();
                                        DeliveryAddress.ParentPartyType = "Customer";
                                        DeliveryAddress.ParentPartyFullName = reader["Account Name"].ToString();
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
}
