using Aquazania.Telephony.Integration.Models;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Text.RegularExpressions;

namespace Aquazania.Integration.ServerApp.Client.UnlinkingContacts
{
    public class UnlinkSupplierDeliveryAddressLinkedParty : AbsMasterUnlinkParty
    {
        public override List<MasterOwnedLinkedContactContract> buildMasterLinkObject(OdbcConnection connection, OdbcTransaction transaction, string _COM_connectionString, string _DTS_connectionString)
        {
            List<MasterOwnedLinkedContactContract> SupplierDeliveryAddressUpdates = new List<MasterOwnedLinkedContactContract>();
            try
            {
                string sql = "SELECT PartyCode, "
                           + "       ContactID "
                           + "FROM [Temp Master Unlinked Contacts] "
                           + "WHERE [Synced] = 0 AND "
                           + "	    [PartyType] = 'SupplierDeliveryAddress' "
                           + "GROUP BY PartyCode, ContactID ";
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
                                                "WHERE [ContactID] = " + reader["ContactID"].ToString() +
                                                "  AND [ContactPointTypeID] = 2";
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                while (readerAcc.Read())
                                {
                                    MasterOwnedLinkedContactContract supplierDeliveryAddress = new MasterOwnedLinkedContactContract();
                                    using (var connectionAccountInfo = new OdbcConnection(_DTS_connectionString))
                                    {
                                        try
                                        {
                                            string sqlAccInfo = "SELECT T1.*," +
                                                                "       T2.[Supplier Name] " +
                                                                "FROM [Supplier Delivery Address] T1 " +
                                                                "   LEFT JOIN [Supplier] T2 ON " +
                                                                "       T1.[Supplier No] = T2.[Supplier No]" +
                                                                "WHERE [Delivery Address Code] = '" + reader["PartyCode"].ToString() + "'";
                                            connectionAccountInfo.Open();
                                            var commandAccInfo = new OdbcCommand(sqlAccInfo, connectionAccountInfo);
                                            var readerAccInfo = commandAccInfo.ExecuteReader();
                                            if (readerAccInfo.HasRows)
                                            {
                                                while (readerAccInfo.Read())
                                                {
                                                    int accountNoIndex = readerAccInfo.GetOrdinal("Supplier No");
                                                    if (!readerAccInfo.IsDBNull(accountNoIndex))
                                                    {
                                                        supplierDeliveryAddress.AccountCode = readerAccInfo["Supplier No"].ToString();
                                                        supplierDeliveryAddress.AccountName = readerAccInfo["Supplier Name"].ToString();
                                                    }
                                                    else
                                                    { supplierDeliveryAddress.AccountName = null; supplierDeliveryAddress.AccountCode = null; }
                                                }
                                            }
                                            else
                                            { supplierDeliveryAddress.AccountName = null; supplierDeliveryAddress.AccountCode = null; }
                                        }
                                        catch (OdbcException ex) { throw ex; }
                                    }
                                    supplierDeliveryAddress.ParentPartyCode = reader["PartyCode"].ToString();
                                    supplierDeliveryAddress.ParentPartyType = "SupplierDeliveryAddress";
                                    supplierDeliveryAddress.ContactFullName = readerAcc["ContactName"].ToString() + " " + (!readerAcc.IsDBNull(readerAcc.GetOrdinal("ContactLastName")) ? readerAcc["ContactLastName"].ToString() : "");
                                    supplierDeliveryAddress.PhoneNumber = Regex.Replace(readerAcc["ContactPointValue"].ToString(), @"\D", "");
                                    supplierDeliveryAddress.IsActive = false;
                                    SupplierDeliveryAddressUpdates.Add(supplierDeliveryAddress);
                                    string filePath = @"C:\Tracking Folder\MasterUnlinkedPartySupplierDeliveryAddress.txt";
                                    using (StreamWriter writer = new StreamWriter(filePath, true))
                                    {
                                        writer.WriteLine();
                                    }
                                    File.AppendAllText(filePath, JsonConvert.SerializeObject(supplierDeliveryAddress, Formatting.Indented) + ",");
                                }
                            }
                            catch (OdbcException ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    return SupplierDeliveryAddressUpdates;
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
        public override void UpdateSyncLinkMasterTable(OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                string sql = "UPDATE [Temp Master Unlinked Contacts] "
                            + "	SET [Synced] = 1 "
                            + "WHERE PartyType = 'SupplierDeliveryAddress' AND "
                            + "	  PartyCode IN (SELECT PartyCode "
                            + "					FROM [Temp Master Unlinked Contacts] "
                            + "					WHERE [Synced] = 0 AND "
                            + "						  [PartyType] = 'SupplierDeliveryAddress' "
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
    }
}
