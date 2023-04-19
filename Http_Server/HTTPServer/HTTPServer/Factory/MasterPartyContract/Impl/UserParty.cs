using Aquazania.Telephony.Integration.Models;
using System.Data.Odbc;
using System.Net.Http;

namespace HTTPServer.Factory.MasterPartyContract.Impl
{
    public class UserParty : IPartyConvertor
    {
        private string _DTS_connectionString;
        public UserParty(IConfiguration configuration)
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
            try
            {
                string sql = "UPDATE [User] "
                            + "	SET [" + updatedField + "] = '" + newValue + "' "
                            + "WHERE [User Name] = '" + party.PartyCode + "'";
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
                string sql = "SELECT * FROM [User] WHERE [User Name] = '" + party.PartyCode + "'";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
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
                string sql = "SELECT [User Name] FROM [User] WHERE [User Name] = '" + party.PartyCode + "'";
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
    }
}
