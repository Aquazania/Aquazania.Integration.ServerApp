using Aquazania.Telephony.Integration.Models;
using System.Data.Common;
using System.Data.Odbc;
using System.Net;
using System.Net.Mail;
using System.Security.Principal;
using System.Xml.Linq;

namespace HTTPServer.Factory.MasterPartyContract.Impl
{
    public class CustomerParty : IPartyConvertor
    {
        public CustomerParty(IConfiguration configuration)
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
                string sql = "SELECT [Account No] FROM [Customer] WHERE [Account No] = '" + party.PartyCode + "'";
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
                string sql = "SELECT * FROM [Customer] WHERE [Account No] = '" + party.PartyCode + "'";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (party.PartyPrimaryContactFullName != reader["Creditors Clerk"].ToString())
                        rows += PerformUpdate("Creditors Clerk",
                                                reader["Creditors Clerk"].ToString(),
                                                party.PartyPrimaryContactFullName,
                                                party, connection, transaction);
                    if (party.PartyPrimaryTelephoneNumber != reader["Telephone No"].ToString())
                        rows += PerformUpdate("Telephone No",
                                                reader["Telephone No"].ToString(),
                                                party.PartyPrimaryTelephoneNumber,
                                                party, connection, transaction);
                    if (party.PartyPrimaryCellNumber != reader["Cell Phone No"].ToString())
                        rows += PerformUpdate("Cell Phone No",
                                                reader["Cell Phone No"].ToString(),
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
            EnterHistoryRecord(updatedField, oldValue, newValue, party.PartyCode, party.User.UserName, connection, transaction);
            try
            {
                string sql = "UPDATE [Customer] "
                            + "	SET [" + updatedField + "] = '" + newValue + "' "
                            + "WHERE [Account No] = '" + party.PartyCode + "'";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                return command.ExecuteNonQuery();
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
        public void EnterHistoryRecord(string updatedField, string oldValue, string newValue, string accountNo, string userName, OdbcConnection connection, OdbcTransaction transaction)
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
                            + "	     1, "
                            + "	     '" + accountNo + "', "
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
                            + "	     'Customer', "
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
