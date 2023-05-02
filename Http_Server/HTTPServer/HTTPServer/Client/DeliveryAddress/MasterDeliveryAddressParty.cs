using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Text.RegularExpressions;

namespace Aquazania.Integration.ServerApp.Client.DeliveryAddress
{

    public class MasterDeliveryAddressParty : AbsMasterParty
    {
        public override void UpdateSyncMasterTable(OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
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
        public override List<MasterOwnedPartyContract> buildMasterObject(OdbcConnection connection, OdbcTransaction transaction, string _DTS_connectionString)
        {
            List<MasterOwnedPartyContract> DeliveryAddressUpdates = new List<MasterOwnedPartyContract>();
            try
            {
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
                        using (var connectionAcc = new OdbcConnection(_DTS_connectionString))
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
                                    DeliveryAddress.AccountCode = readerAcc["Account No"].ToString();
                                    DeliveryAddress.AccountName = readerAcc["Account Name"].ToString();
                                    DeliveryAddress.PartyType = "DeliveryAddress";
                                    DeliveryAddress.PartyFullName = readerAcc["Delivery Address Line 2"].ToString() + " " + readerAcc["Delivery Address Line 3"].ToString();
                                    DeliveryAddress.PartyPrimaryContactFullName = readerAcc["Contact Person"].ToString();
                                    DeliveryAddress.PartyPrimaryTelephoneNumber = Regex.Replace(readerAcc["Tel No For Contact Person"].ToString(), @"\D", "");
                                    DeliveryAddress.PartyPrimaryCellNumber = Regex.Replace(readerAcc["Cell No For Contact Person"].ToString(), @"\D", "");
                                    DeliveryAddress.IsActive = true;
                                    string filePath = @"C:\Tracking Folder\MasterParty.txt";
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
