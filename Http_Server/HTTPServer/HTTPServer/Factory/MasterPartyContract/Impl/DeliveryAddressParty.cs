using Aquazania.Telephony.Integration.Models;
using Microsoft.Extensions.Configuration;
using System.Data.Odbc;
using System.IO;

namespace HTTPServer.Factory.MasterPartyContract.Impl
{
    public class DeliveryAddressParty : IPartyConvertor
    {
        public DeliveryAddressParty(IConfiguration configuration)
        {
            _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
        }

        private string _DTS_connectionString;
        public async Task Convert(ChangedPartyContactContract party)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int rows = 0;
                        if (ValidateParty(party, connection, transaction))
                        {
                            rows += UpdateRequired(party, connection, transaction);
                        }
                        else
                        {
                            throw new KeyNotFoundException($"Code : {party.PartyCode} was not found within the database");
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
        public bool ValidateParty(ChangedPartyContactContract party, OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                string sql = "";
                if (party.ParentPartyType == "Supplier")
                    sql = "SELECT [Delivery Address Code] FROM [Supplier Delivery Address] WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
                else
                    sql = "SELECT [Delivery Address Code] FROM [Delivery Address] WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
        public int UpdateRequired(ChangedPartyContactContract party, OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                int rows = 0;
                string sql = "";
                if (party.ParentPartyType == "Supplier")
                    sql = "SELECT * FROM [Supplier Delivery Address] WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
                else
                    sql = "SELECT * FROM [Delivery Address] WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (party.PartyPrimaryContactFullName != reader["Contact Person"].ToString())
                        rows += PerformUpdate("Contact Person",
                                                reader["Contact Person"].ToString(),
                                                party.PartyPrimaryContactFullName,
                                                party, connection, transaction);
                    if (party.PartyPrimaryTelephoneNumber != reader["Tel No For Contact Person"].ToString())
                        rows += PerformUpdate("Tel No For Contact Person",
                                                reader["Tel No For Contact Person"].ToString(),
                                                party.PartyPrimaryTelephoneNumber,
                                                party, connection, transaction);
                    if (party.PartyPrimaryCellNumber != reader["Cell No For Contact Person"].ToString())
                        rows += PerformUpdate("Cell No For Contact Person",
                                                reader["Cell No For Contact Person"].ToString(),
                                                party.PartyPrimaryCellNumber,
                                                party, connection, transaction);
                }
                return rows;
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
        public int PerformUpdate(string updatedField, string oldValue, string newValue, ChangedPartyContactContract party, OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                string sql = "";
                if (party.ParentPartyType == "Supplier")
                {
                    EnterHistoryRecord(updatedField, oldValue, newValue, party.PartyCode, 44, "Supplier Delivery Address", party.User.UserName, connection, transaction);
                    sql = "UPDATE [Supplier Delivery Address] "
                        + "	SET [" + updatedField + "] = '" + newValue + "' "
                        + "WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
                }
                else
                {
                    EnterHistoryRecord(updatedField, oldValue, newValue, party.PartyCode, 14, "Delivery Address", party.User.UserName, connection, transaction);
                    sql = "UPDATE [Delivery Address] "
                        + "	SET [" + updatedField + "] = '" + newValue + "' "
                        + "WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
                }
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                return command.ExecuteNonQuery();
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
        public void EnterHistoryRecord(string updatedField, string oldValue, string newValue, string deliveryAddressCode, int referenceType, string tableName, string userName, OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                string sql = "DECLARE @UpdateNo INT "
                            + "INSERT INTO [Update History] ([User Name] "
                            + "							   ,[Requested By] "
                            + "							   ,[Reference Type] "
                            + "							   ,[Key Value] "
                            + "							   ,[Date Stamp]) "
                            + "SELECT '" + userName + "', "
                            + "	     NULL, "
                            + "	     " + referenceType + ", "
                            + "	     '" + deliveryAddressCode + "', "
                            + "	     '" + DateTime.Now + "' "
                            + "SELECT @UpdateNo = SCOPE_IDENTITY() "
                            + "INSERT INTO [Update History Detail] ([Column Name], "
                            + "								       [New Value], "
                            + "									   [Old Value], "
                            + "									   [Table Name], "
                            + "									   [Update No]) "
                            + "SELECT '" + updatedField + "', "
                            + "	     '" + newValue + "', "
                            + "	     '" + oldValue + "', "
                            + "	     '" + tableName + "', "
                            + "	     @UpdateNo ";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
    }
}
