using Aquazania.Integration.ServerApp.Factory;
using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Text.RegularExpressions;

namespace Aquazania.Integration.ServerApp.Client.SupplierDeliveryAddress
{
    public class MasterSupplierDeliveryAddressLinkedParty : AbsMasterLinkedParty
    {
        public override void UpdateSyncLinkMasterTable(OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                string sql = "UPDATE [Temp Master Party Contract] "
                            + "	SET [Synced] = 1 "
                            + "WHERE PartyType = 'SupplierDeliveryAddress' AND "
                            + "	  PartyCode IN (SELECT PartyCode "
                            + "					FROM [Temp Master Party Contract] "
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
        public override List<MasterOwnedLinkedContactContract> buildMasterLinkObject(OdbcConnection connection, OdbcTransaction transaction, string _COM_connectionString, string _DTS_connectionString)
        {
            List<MasterOwnedLinkedContactContract> DeliveryAddressUpdates = new List<MasterOwnedLinkedContactContract>();
            try
            {
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] "
                            + "WHERE [Synced] = 0 AND "
                            + "	    [PartyType] = 'SupplierDeliveryAddress' "
                            + "GROUP BY PartyCode ";
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
                                                "WHERE [DocumentReferenceCode] = '" + reader["PartyCode"].ToString() + "'" +
                                                "  AND [ContactPointTypeID] = 2 ";
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                string prevAccountNo = null;
                                string accName = null;
                                string accNo = null;
                                while (readerAcc.Read())
                                {
                                    MasterOwnedLinkedContactContract DeliveryAddress = new MasterOwnedLinkedContactContract();
                                    string curAccountNo = readerAcc["DocumentReferenceCode"].ToString();
                                    if (prevAccountNo != curAccountNo)
                                    {
                                        using (var connectionAccountInfo = new OdbcConnection(_DTS_connectionString))
                                        {
                                            try
                                            {
                                                string sqlAccInfo = "SELECT T1.*, " +
                                                                    "       T2.[Account No] " +
                                                                    "FROM [Supplier Delivery Address] T1 " +
                                                                    "   INNER JOIN [Supplier] T2 ON " +
                                                                    "       T1.[Supplier No] = T2.[Supplier No]" +
                                                                    "WHERE T1.[Supplier No] = '" + readerAcc["DocumentReferenceCode"].ToString() + "'";
                                                connectionAccountInfo.Open();
                                                var commandAccInfo = new OdbcCommand(sqlAccInfo, connectionAccountInfo);
                                                var readerAccInfo = commandAccInfo.ExecuteReader();
                                                if (readerAccInfo.HasRows)
                                                {
                                                    while (readerAccInfo.Read())
                                                    {
                                                        int accountNoIndex = readerAccInfo.GetOrdinal("Account No");
                                                        if (!readerAccInfo.IsDBNull(accountNoIndex))
                                                        {
                                                            DeliveryAddress.AccountCode = readerAccInfo["Account No"].ToString();
                                                            DeliveryAddress.AccountName = readerAccInfo["Account Name"].ToString();
                                                            accNo = readerAccInfo["Account No"].ToString();
                                                            accName = readerAccInfo["Account Name"].ToString();
                                                        }
                                                    }
                                                }
                                                else
                                                { DeliveryAddress.AccountName = null; DeliveryAddress.AccountCode = null; }
                                            }
                                            catch (OdbcException ex) { throw ex; }
                                        }
                                    }
                                    else
                                    { DeliveryAddress.AccountCode = accNo; DeliveryAddress.AccountName = accName; }
                                    DeliveryAddress.ParentPartyCode = readerAcc["DocumentReferenceCode"].ToString();
                                    DeliveryAddress.ParentPartyType = "DeliveryAddress";
                                    DeliveryAddress.ContactFullName = readerAcc["ContactName"].ToString() + " " + (!readerAcc.IsDBNull(readerAcc.GetOrdinal("ContactLastName")) ? readerAcc["ContactLastName"].ToString() : "");                                    
                                    DeliveryAddress.PhoneNumber = Regex.Replace(readerAcc["ContactPointValue"].ToString(), @"\D", "");
                                    DeliveryAddress.IsActive = true;
                                    DeliveryAddressUpdates.Add(DeliveryAddress);
                                    string filePath = @"C:\Tracking Folder\MasterLinkedPartySupplierDeliveryAddress.txt";
                                    using (StreamWriter writer = new StreamWriter(filePath, true))
                                    {
                                        writer.WriteLine();
                                    }
                                    File.AppendAllText(filePath, JsonConvert.SerializeObject(DeliveryAddress, Formatting.Indented) + ",");
                                    prevAccountNo = curAccountNo;
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
