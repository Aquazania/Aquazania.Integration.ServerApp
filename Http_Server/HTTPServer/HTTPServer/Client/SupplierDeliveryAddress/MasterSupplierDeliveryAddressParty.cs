using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace Aquazania.Integration.ServerApp.Client.SupplierDeliveryAddress
{
    public class MasterSupplierDeliveryAddressParty : AbsMasterParty
    {
        public override void UpdateSyncMasterTable(OdbcConnection connection, OdbcTransaction transaction)
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
        public override List<MasterOwnedPartyContract> buildMasterObject(OdbcConnection connection, OdbcTransaction transaction, string _DTS_connectionString)
        {
            List<MasterOwnedPartyContract> DeliveryAddressUpdates = new List<MasterOwnedPartyContract>();
            try
            {
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] "
                            + "WHERE [Synced] = 0 AND "
                            + "	  [PartyType] = 'SupplierDeliveryAddress' "
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
                                string sqlAcc = "SELECT *, " +
                                                "       T3.[Account No], " +
                                                "       T3.[Account Name] " + 
                                                "FROM [Supplier Delivery Address] T1 " +
                                                "   INNER JOIN [Supplier] T2 ON " +
                                                "       T1.[Supplier No] = T2.[Supplier No] " +
                                                "   LEFT JOIN [Customer] T3 ON " + 
                                                "       T2.[Account No] = T3.[Account No] " + 
                                                " WHERE T1.[Delivery Address Code] = '" + reader["PartyCode"].ToString() + "'";
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                while (readerAcc.Read())
                                {
                                    MasterOwnedPartyContract DeliveryAddress = new MasterOwnedPartyContract();
                                    DeliveryAddress.ParentPartyCode = readerAcc["Supplier No"].ToString();
                                    DeliveryAddress.ParentPartyType = "Supplier";
                                    DeliveryAddress.ParentPartyFullName = readerAcc["Supplier Name"].ToString();
                                    DeliveryAddress.PartyCode = readerAcc["Delivery Address Code"].ToString();
                                    DeliveryAddress.PartyType = "DeliveryAddress";
                                    int accountNoIndex = readerAcc.GetOrdinal("Account No");
                                    if (!readerAcc.IsDBNull(accountNoIndex))
                                    {
                                        DeliveryAddress.AccountCode = readerAcc["Account No"].ToString();
                                        DeliveryAddress.AccountName = readerAcc["Account Name"].ToString();
                                    }
                                    else
                                    {
                                        DeliveryAddress.AccountCode = null;
                                        DeliveryAddress.AccountName = null;
                                    }
                                    DeliveryAddress.PartyFullName = readerAcc["Delivery Address Line 2"].ToString() + " " + readerAcc["Delivery Address Line 3"].ToString();
                                    DeliveryAddress.PartyPrimaryContactFullName = readerAcc["Contact Person"].ToString();                                    
                                    DeliveryAddress.PartyPrimaryTelephoneNumber = Regex.Replace(readerAcc["Tel No For Contact Person"].ToString(), @"\D", "");                                    
                                    DeliveryAddress.PartyPrimaryCellNumber = Regex.Replace(readerAcc["Cell No For Contact Person"].ToString(), @"\D", "");
                                    DeliveryAddress.IsActive = true;
                                    string filePath = @"C:\Tracking Folder\MasterPartySupplierDeliveryAddress.txt";
                                    using (StreamWriter writer = new StreamWriter(filePath, true))
                                    {
                                        writer.WriteLine();
                                    }
                                    File.AppendAllText(filePath, JsonConvert.SerializeObject(DeliveryAddress, Formatting.Indented) + ",");
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
