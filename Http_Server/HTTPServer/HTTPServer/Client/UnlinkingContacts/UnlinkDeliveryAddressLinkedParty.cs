using Aquazania.Telephony.Integration.Models;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Text.RegularExpressions;

namespace Aquazania.Integration.ServerApp.Client.UnlinkingContacts
{
    public class UnlinkDeliveryAddressLinkedParty : AbsMasterUnlinkParty
    {
        public override List<MasterOwnedLinkedContactContract> buildMasterLinkObject(OdbcConnection connection, OdbcTransaction transaction, string _COM_connectionString, string _DTS_connectionString)
        {
            List<MasterOwnedLinkedContactContract> deliveryAddressUpdates = new List<MasterOwnedLinkedContactContract>();
            try
            {
                string sql = "SELECT PartyCode, "
                           + "       ContactID "
                           + "FROM [Temp Master Unlinked Contacts] "
                           + "WHERE [Synced] = 0 AND "
                           + "	    [PartyType] = 'DeliveryAddress' "
                           + "GROUP BY PartyCode , ContactID";
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
                                    MasterOwnedLinkedContactContract deliveryAddress = new MasterOwnedLinkedContactContract();
                                    using (var connectionAccountInfo = new OdbcConnection(_DTS_connectionString))
                                    {
                                        try
                                        {
                                            string sqlAccInfo = "SELECT T1.*," +
                                                                "       T2.[Account Name] " +
                                                                "FROM [Delivery Address] T1 " +
                                                                "   INNER JOIN [Customer] T2 ON " +
                                                                "       T1.[Account No] = T2.[Account No]" +
                                                                "WHERE [Delivery Address Code] = '" + reader["PartyCode"].ToString() + "'";
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
                                                        deliveryAddress.AccountCode = readerAccInfo["Account No"].ToString();
                                                        deliveryAddress.AccountName = readerAccInfo["Account Name"].ToString();
                                                    }
                                                    else
                                                    { deliveryAddress.AccountName = null; deliveryAddress.AccountCode = null; }
                                                }
                                            }
                                            else
                                            { deliveryAddress.AccountName = null; deliveryAddress.AccountCode = null; }
                                        }
                                        catch (OdbcException ex) { throw ex; }
                                    }
                                    deliveryAddress.ParentPartyCode = reader["PartyCode"].ToString();
                                    deliveryAddress.ParentPartyType = "DeliveryAddress";
                                    deliveryAddress.ContactFullName = readerAcc["ContactName"].ToString() + " " + (!readerAcc.IsDBNull(readerAcc.GetOrdinal("ContactLastName")) ? readerAcc["ContactLastName"].ToString() : "");
                                    deliveryAddress.PhoneNumber = Regex.Replace(readerAcc["ContactPointValue"].ToString(), @"\D", "");
                                    deliveryAddress.IsActive = false;
                                    deliveryAddressUpdates.Add(deliveryAddress);
                                    string filePath = @"C:\Tracking Folder\MasterUnlinkedPartydeliveryAddress.txt";
                                    using (StreamWriter writer = new StreamWriter(filePath, true))
                                    {
                                        writer.WriteLine();
                                    }
                                    File.AppendAllText(filePath, JsonConvert.SerializeObject(deliveryAddress, Formatting.Indented) + ",");
                                }
                            }
                            catch (OdbcException ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    return deliveryAddressUpdates;
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
                            + "WHERE PartyType = 'DeliveryAddress' AND "
                            + "	  PartyCode IN (SELECT PartyCode "
                            + "					FROM [Temp Master Unlinked Contacts] "
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
    }
}
