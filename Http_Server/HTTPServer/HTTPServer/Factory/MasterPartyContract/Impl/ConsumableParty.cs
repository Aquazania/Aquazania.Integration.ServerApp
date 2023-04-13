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
        public int Convert(ChangedPartyContactContract party)
        {
            int rows = 0;
            if (ValidateParty(party))
            {
                rows += UpdateRequired(party);
            }
            else
            {
                throw new KeyNotFoundException($"Code : {party.PartyCode} was not found within the database");
            }
            return rows;
        }

        public int PerformUpdate(string updatedField, string oldValue, string newValue, ChangedPartyContactContract party)
        {
            EnterHistoryRecord(updatedField, oldValue, newValue, party.PartyCode);

            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "UPDATE [Customer] "
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
                    string sql = "SELECT * FROM [Consumables] WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
                    var command = new OdbcCommand(sql, connection);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (party.PartyPrimaryContactFullName != reader["Consumables Contact Person"].ToString())
                            rows += PerformUpdate("Consumables Contact Person",
                                                  reader["Consumables Contact Person"].ToString(),
                                                  party.PartyPrimaryContactFullName,
                                                  party);
                        if (party.PartyPrimaryTelephoneNumber != reader["Tel No For Consumables Contact Person"].ToString())
                            rows += PerformUpdate("Tel No For Consumables Contact Person",
                                                  reader["Tel No For Consumables Contact Person"].ToString(),
                                                  party.PartyPrimaryTelephoneNumber,
                                                  party);
                        if (party.PartyPrimaryCellNumber != reader["Cell No For Consumables Contact Person"].ToString())
                            rows += PerformUpdate("Cell No For Consumables Contact Person",
                                                  reader["Cell No For Consumables Contact Person"].ToString(),
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
                    string sql = "SELECT [Delivery Address Code] FROM [Consumables] WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
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
        public void EnterHistoryRecord(string updatedField, string oldValue, string newValue, string deliveryAddressCode)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    //Swap this around. take the update no from history and put this into one query and execute both 
                    connection.Open();
                    string sql = "DECLARE @UpdateNo INT "
                               + "INSERT INTO [Update History] ([User Name] "
                               + "							   ,[Requested By] "
                               + "							   ,[Reference Type] "
                               + "							   ,[Key Value] "
                               + "							   ,[Date Stamp]) "
                               + "SELECT 'Dariel', "
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
                    command.ExecuteNonQuery();
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
    }
}
