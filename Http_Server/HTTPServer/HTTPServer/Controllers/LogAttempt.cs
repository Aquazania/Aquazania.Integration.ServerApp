using Aquazania.Integration.ServerApp.Factory;
using Aquazania.Telephony.Integration.Models;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.IO;

namespace Aquazania.Integration.ServerApp.Controllers
{
    public static class LogAttempt
    {
        public static bool LogAttemptAtEndPointLinkedContract(List<ChangedLinkedContactContract> parties, Response response, IConfiguration configuration)
        {
            string _COM_connectionString = configuration.GetConnectionString("Communicator_Connection");
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "INSERT INTO [Attempts At Our Endpoints] ([EndPoint Name] "
                               + "							    		  ,[Attempted Payload] "
                               + "							    		  ,[Payload Sent Back] "
                               + "							    		  ,[Time Attempted] "
                               + "							    		  ,[Amount Of Errors] "
                               + "							    		  ,[Amount Of Successes]) "
                               + "SELECT 'PostLinkedParty', "
                               + "       '" + JsonConvert.SerializeObject(parties) + "', "
                               + "	     '" + JsonConvert.SerializeObject(response) + "', "
                               + "	     '" + DateTime.Now + "', "
                               + "	     " + response.NumberOfFailures + ", "
                               + "	     " + response.NumberOfSuccesses + " "; 
                    var command = new OdbcCommand(sql, connection);
                    return command.ExecuteNonQuery() > 0;
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        public static bool LogAttemptAtEndPointContract(List<ChangedPartyContactContract> parties, Response response, IConfiguration configuration)
        {
            string _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "INSERT INTO [Attempts At Our Endpoints] ([EndPoint Name] "
                               + "							    		  ,[Attempted Payload] "
                               + "							    		  ,[Payload Sent Back] "
                               + "							    		  ,[Time Attempted] "
                               + "							    		  ,[Amount Of Errors] "
                               + "							    		  ,[Amount Of Successes]) "
                               + "SELECT 'PostParty', "
                               + "       '" + JsonConvert.SerializeObject(parties) + "', "
                               + "	     '" + JsonConvert.SerializeObject(response) + "', "
                               + "	     '" + DateTime.Now + "', "
                               + "	     " + response.NumberOfFailures + ", "
                               + "	     " + response.NumberOfSuccesses + " ";
                    var command = new OdbcCommand(sql, connection);
                    return command.ExecuteNonQuery() > 0;
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        public static bool LogAttemptAtEndPointCallHistory(List<CallHistoryEntryContract> callHistories, Response response, IConfiguration configuration)
        {
            string _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "INSERT INTO [Attempts At Our Endpoints] ([EndPoint Name] "
                               + "							    		  ,[Attempted Payload] "
                               + "							    		  ,[Payload Sent Back] "
                               + "							    		  ,[Time Attempted] "
                               + "							    		  ,[Amount Of Errors] "
                               + "							    		  ,[Amount Of Successes]) "
                               + "SELECT 'PostCallHistoryEntry', "
                               + "       '" + JsonConvert.SerializeObject(callHistories) + "', "
                               + "	     '" + JsonConvert.SerializeObject(response) + "', "
                               + "	     '" + DateTime.Now + "', "
                               + "	     " + response.NumberOfFailures + ", "
                               + "	     " + response.NumberOfSuccesses + " ";
                    var command = new OdbcCommand(sql, connection);
                    return command.ExecuteNonQuery() > 0;
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
    }
}
