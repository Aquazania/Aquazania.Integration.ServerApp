using Aquazania.Telephony.Integration.Models;
using Microsoft.Extensions.Configuration;
using System.Data.Odbc;

namespace HTTPServer.Factory.MasterPartyContract.Impl
{
    public class ContactParty : IPartyConvertor
    {
        private string _DTS_connectionString;
        public ContactParty(IConfiguration configuration)
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
        public int DoInsert(ChangedPartyContactContract party, OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                string sql = "INSERT INTO [Contact] ([Contact Person] "
                            + "					    ,[Company] "
                            + "					    ,[Job Title] "
                            + "					    ,[Address Line 1] "
                            + "					    ,[Address Line 2] "
                            + "					    ,[Suburb] "
                            + "					    ,[Postal Code] "
                            + "					    ,[Telephone No] "
                            + "					    ,[Cell Phone No] "
                            + "					    ,[Fax No] "
                            + "					    ,[E-Mail Address] "
                            + "					    ,[Note] "
                            + "					    ,[Public Contact] "
                            + "					    ,[Date Created] "
                            + "					    ,[Created By]) "
                            + "SELECT '" + party.PartyPrimaryContactFullName + "', "
                            + "	     NULL, "
                            + "	     NULL, "
                            + "	     NULL, "
                            + "	     NULL, "
                            + "	     NULL, "
                            + "	     NULL, "
                            + "	     '" + party.PartyPrimaryTelephoneNumber + "', "
                            + "	     '" + party.PartyPrimaryCellNumber + "', "
                            + "	     NULL, "
                            + "	     NULL, "
                            + "	     NULL, "
                            + "	     NULL, "
                            + "	     '" + DateTime.Now + "', "
                            + "	     '" + party.User.UserName + "'";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                return command.ExecuteNonQuery();
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
                string sql = "UPDATE [Contact] "
                            + "	SET [" + updatedField + "] = '" + newValue + "' "
                            + "WHERE [Contact No] = '" + party.PartyCode + "'";
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
                string sql = "SELECT * FROM [Contact] WHERE [Contact No] = '" + party.PartyCode + "'";
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
        public bool ValidateParty(ChangedPartyContactContract party, OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                string sql = "SELECT [Contact No] FROM [Contact] WHERE [Contact No] = '" + party.PartyCode + "'";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    return true;
                }
                else
                {
                    return  DoInsert(party, connection, transaction) > 0;
                }
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
    }
}
