using Aquazania.Telephony.Integration.Models;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Text.RegularExpressions;

namespace Aquazania.Integration.ServerApp.Client.UnlinkingContacts
{
    public class UnlinkCustomerLinkedParty : AbsMasterUnlinkParty
    {
        public override List<MasterOwnedLinkedContactContract> buildMasterLinkObject(OdbcConnection connection, OdbcTransaction transaction, string _COM_connectionString, string _DTS_connectionString)
        {
            List<MasterOwnedLinkedContactContract> customerUpdates = new List<MasterOwnedLinkedContactContract>();
            try
            {
                string sql = "SELECT PartyCode, "
                           + "       ContactID "
                           + "FROM [Temp Master Unlinked Contacts] "
                           + "WHERE [Synced] = 0 AND "
                           + "	    [PartyType] = 'Customer' "
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
                                    MasterOwnedLinkedContactContract customer = new MasterOwnedLinkedContactContract();
                                    using (var connectionAccountInfo = new OdbcConnection(_DTS_connectionString))
                                    {
                                        try
                                        {
                                            string sqlAccInfo = "SELECT * "  +
                                                                "FROM [Customer] " +
                                                                "WHERE [Account No] = '" + reader["PartyCode"].ToString() + "'";
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
                                                        customer.AccountCode = readerAccInfo["Account No"].ToString();
                                                        customer.AccountName = readerAccInfo["Account Name"].ToString();
                                                    }
                                                    else
                                                    { customer.AccountName = null; customer.AccountCode = null; }
                                                }
                                            }
                                            else
                                            { customer.AccountName = null; customer.AccountCode = null; }
                                        }
                                        catch (OdbcException ex) { throw ex; }
                                    }
                                    customer.ParentPartyCode = reader["PartyCode"].ToString();
                                    customer.ParentPartyType = "Customer";
                                    customer.ContactFullName = readerAcc["ContactName"].ToString() + " " + (!readerAcc.IsDBNull(readerAcc.GetOrdinal("ContactLastName")) ? readerAcc["ContactLastName"].ToString() : "");
                                    customer.PhoneNumber = Regex.Replace(readerAcc["ContactPointValue"].ToString(), @"\D", "");
                                    customer.IsActive = false;
                                    customerUpdates.Add(customer);
                                    string filePath = @"C:\Tracking Folder\MasterUnlinkedPartycustomer.txt";
                                    using (StreamWriter writer = new StreamWriter(filePath, true))
                                    {
                                        writer.WriteLine();
                                    }
                                    File.AppendAllText(filePath, JsonConvert.SerializeObject(customer, Formatting.Indented) + ",");
                                }
                            }
                            catch (OdbcException ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    return customerUpdates;
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
                            + "WHERE PartyType = 'Customer' AND "
                            + "	  PartyCode IN (SELECT PartyCode "
                            + "					FROM [Temp Master Unlinked Contacts] "
                            + "					WHERE [Synced] = 0 AND "
                            + "						  [PartyType] = 'Customer' "
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
