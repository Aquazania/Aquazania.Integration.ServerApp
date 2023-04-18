using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.Supplier
{
    public class MasterSupplierLinkedParty : IMasterLinkedParty
    {
        public MasterSupplierLinkedParty(string url) { darielURL = url; }
        private string darielURL;
        public async void SendMasterLinkedParty(ITimed_Client _httpClient, string _COM_connectionString)
        {
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var data = buildMasterLinkObject(connection, transaction);
                        if (data.Count > 0)
                        {
                            var response = await _httpClient.SendAsync(data, darielURL);

                            if (response.IsSuccessStatusCode)
                            {
                                UpdateSyncLinkMasterTable(connection, transaction);
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
                command.Transaction = transaction;
                int rows = command.ExecuteNonQuery();
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
        public List<MasterOwnedLinkedContactContract> buildMasterLinkObject(OdbcConnection connection, OdbcTransaction transaction)
        {
            List<MasterOwnedLinkedContactContract> supplierUpdates = new List<MasterOwnedLinkedContactContract>();
            try
            {
                connection.Open();
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] "
                            + "WHERE [Synced] = 0 AND "
                            + "	    [PartyType] = 'Supplier' "
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
                                string sqlAcc = "SELECT * " +
                                                "FROM [viewContactDocumentReference] " +
                                                "WHERE [DocumentReferenceCode] = '" + reader["PartyCode"].ToString() + "' " +
                                                "  AND [ContactPointTypeID] = 2 ";
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
