using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.Supplier
{
    public class MasterSupplierLinkedParty
    {
        public MasterSupplierLinkedParty(string url) { darielURL = url; }
        private string darielURL;
        public async void SendMasterSupplierLinkedParty(ITimed_Client _httpClient, string _COM_connectionString)
        {
            var data = buildMasterSupplierLinkObject(_COM_connectionString);
            if (data.Count > 0)
            {
                var response = await _httpClient.SendAsync(data, darielURL);

                if (response.IsSuccessStatusCode)
                {
                    UpdateSyncSupplierLinkMasterTable(_COM_connectionString);
                }
            }
        }

        private void UpdateSyncSupplierLinkMasterTable(string _COM_connectionString)
        {
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "UPDATE [Temp Master Party Contract] "
                               + "	SET [Synced] = 1 "
                               + "WHERE PartyType = 'Supplier' AND "
                               + "	  PartyCode IN (SELECT PartyCode "
                               + "					FROM [Temp Master Party Contract] "
                               + "					WHERE [Synced] = 0 AND "
                               + "						  [PartyType] = 'Supplier' "
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
        private List<MasterOwnedLinkedContactContract> buildMasterSupplierLinkObject(string _COM_connectionString)
        {
            List<MasterOwnedLinkedContactContract> supplierUpdates = new List<MasterOwnedLinkedContactContract>();
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT PartyCode "
                               + "FROM [Temp Master Party Contract] "
                               + "WHERE [Synced] = 0 AND "
                               + "	    [PartyType] = 'Supplier' "
                               + "GROUP BY PartyCode ";
                    var command = new OdbcCommand(sql, connection);
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
                                    string sqlAcc = "SELECT * FROM [viewContactDocumentReference] WHERE [DocumentReferenceCode] = '" + reader["PartyCode"].ToString() + "'";
                                    var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                    var readerAcc = commandAcc.ExecuteReader();
                                    while (readerAcc.Read())
                                    {
                                        MasterOwnedLinkedContactContract supplier = new MasterOwnedLinkedContactContract();
                                        supplier.ParentPartyCode = readerAcc["DocumentReferenceCode"].ToString();
                                        supplier.ParentPartyType = "Supplier";
                                        supplier.ContactFullName = readerAcc["ContactName"].ToString() + " " + (!readerAcc.IsDBNull(readerAcc.GetOrdinal("ContactLastName")) ? readerAcc["ContactLastName"].ToString() : "");
                                        supplier.PhoneNumber = readerAcc["ContactPointValue"].ToString();
                                        supplier.IsActive = true;
                                        supplierUpdates.Add(supplier);
                                    }
                                }
                                catch (OdbcException ex)
                                {
                                    throw ex;
                                }
                            }
                        }
                        return supplierUpdates;
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
        }
    }
}
