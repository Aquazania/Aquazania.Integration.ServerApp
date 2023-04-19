using Aquazania.Telephony.Integration.Models;
using HTTPServer.Factory.MasterPartyContract;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Factory.MasterPartyContract.Impl
{
    public class ConsumableParty : IPartyConvertor
    {
        private string _DTS_connectionString;
        public ConsumableParty(IConfiguration configuration)
        {
            _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
        }
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
        public int PerformUpdate(string updatedField, string oldValue, string newValue, ChangedPartyContactContract party, OdbcConnection connection, OdbcTransaction transaction)
        {
            EnterHistoryRecord(updatedField, oldValue, newValue, party.PartyCode, party.User.UserName, connection, transaction);
            try
            {
                string sql = "UPDATE [Consumables] "
                            + "	SET [" + updatedField + "] = '" + newValue + "' "
                            + "WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                return command.ExecuteNonQuery();
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
                string sql = "SELECT * FROM [Consumables] WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (party.PartyPrimaryContactFullName != reader["Consumables Contact Person"].ToString())
                        rows += PerformUpdate("Consumables Contact Person",
                                                reader["Consumables Contact Person"].ToString(),
                                                party.PartyPrimaryContactFullName,
                                                party, connection, transaction);
                    if (party.PartyPrimaryTelephoneNumber != reader["Tel No For Consumables Contact Person"].ToString())
                        rows += PerformUpdate("Tel No For Consumables Contact Person",
                                                reader["Tel No For Consumables Contact Person"].ToString(),
                                                party.PartyPrimaryTelephoneNumber,
                                                party, connection, transaction);
                    if (party.PartyPrimaryCellNumber != reader["Cell No For Consumables Contact Person"].ToString())
                        rows += PerformUpdate("Cell No For Consumables Contact Person",
                                                reader["Cell No For Consumables Contact Person"].ToString(),
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
        public bool ValidateParty(ChangedPartyContactContract party, OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                string sql = "SELECT [Delivery Address Code] FROM [Consumables] WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
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
        public void EnterHistoryRecord(string updatedField, string oldValue, string newValue, string deliveryAddressCode, string userName, OdbcConnection connection, OdbcTransaction transaction)
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
                            + "	     14, "
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
                            + "	     'Consumables', "
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
