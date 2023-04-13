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

        public int Convert(ChangedPartyContactContract party)
        {
            int rows = 0;
            if (ValidateParty(party))
            {
                rows += UpdateRequired(party);
            }
            else
            {
                rows += DoInsert(party);
            }
            return rows;
        }

        public int DoInsert(ChangedPartyContactContract party)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
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
                               + "	     'Dariel'";
                    var command = new OdbcCommand(sql, connection);
                    return command.ExecuteNonQuery();
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        public int PerformUpdate(string updatedField, string oldValue, string newValue, ChangedPartyContactContract party)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "UPDATE [Contact] "
                               + "	SET [" + updatedField + "] = '" + newValue + "' "
                               + "WHERE [Account No] = '" + party.PartyCode + "'";
                    var command = new OdbcCommand(sql, connection);
                    return command.ExecuteNonQuery();
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }

        public int UpdateRequired(ChangedPartyContactContract party)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    int rows = 0;
                    connection.Open();
                    string sql = "SELECT * FROM [Contact] WHERE [Contact No] = '" + party.PartyCode + "'";
                    var command = new OdbcCommand(sql, connection);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (party.PartyPrimaryContactFullName != reader["Contact Person"].ToString())
                            rows += PerformUpdate("Contact Person",
                                                  reader["Contact Person"].ToString(),
                                                  party.PartyPrimaryContactFullName,
                                                  party);
                        if (party.PartyPrimaryTelephoneNumber != reader["Telephone No"].ToString())
                            rows += PerformUpdate("Telephone No",
                                                  reader["Telephone No"].ToString(),
                                                  party.PartyPrimaryTelephoneNumber,
                                                  party);
                        if (party.PartyPrimaryCellNumber != reader["Cell Phone No"].ToString())
                            rows += PerformUpdate("Cell Phone No",
                                                  reader["Cell Phone No"].ToString(),
                                                  party.PartyPrimaryCellNumber,
                                                  party);
                    }
                    return rows;
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }

        public bool ValidateParty(ChangedPartyContactContract party)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT [Contact No] FROM [Contact] WHERE [Contact No] = '" + party.PartyCode + "'";
                    var command = new OdbcCommand(sql, connection);
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
}
