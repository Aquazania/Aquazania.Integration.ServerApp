using Aquazania.Telephony.Integration.Models;
using HTTPServer.Factory.MasterLinkedPartyContract;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Factory.MasterLinkedPartyContract.Impl
{
    public class LinkedSupplierParty : AbsLinkedParty
    {
        public override int DoInsert(ChangedLinkedContactContract party, string _COM_connectionString, int updatetype = 1)
        {
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "DECLARE @ContactID INT "
                                + "DECLARE @ExternalReferenceID INT "
                                + "SELECT @ExternalReferenceID = MAX(DocumentReferenceID) "
                                + "FROM DocumentReference "
                                + "WHERE DocumentReferenceTypeID = 4 AND "
                                + "      DocumentReferenceCode = '" + party.ParentPartyCode + "' "
                                //Insert into Contact Table
                                + "INSERT INTO [Contact] ([ContactName] "
                                + "					    ,[ContactLastName] "
                                + "					    ,[ExternalReferenceID] "
                                + "					    ,[HasMultipleExternalReferences] "
                                + "					    ,[Datestamp] "
                                + "					    ,[Hostname] "
                                + "					    ,[JobDescription] "
                                + "					    ,[SupplierOrdersContact]) "
                                + "SELECT '" + party.ContactFullName + "', "
                                + "	     NULL, "
                                + "	     @ExternalReferenceID, "
                                + "	     0, "
                                + "	     '" + DateTime.Now + "', "
                                + "	     NULL, "
                                + "	     NULL, "
                                + "	     0 "
                                //Gather ID From Contact
                                + "SELECT @ContactID = SCOPE_IDENTITY() "
                                //Insert into ContactPoint
                                + "INSERT INTO [ContactPoint] ([ContactPointTypeID] "
                                + "						     ,[ContactID] "
                                + "						     ,[ContactPointValue] "
                                + "						     ,[AllowSendFrom]) "
                                + "SELECT 2, "
                                + "	   @ContactID, "
                                + "	   '" + party.PhoneNumber + "', "
                                + "	   1 "
                                //Insert into ContactExternalReference
                                + "INSERT INTO [ContactExternalReference] ([ContactID] "
                                + "									     ,[ExternalReferenceID]) "
                                + "SELECT @ContactID, "
                                + "	     @ExternalReferenceID ";
                    var command = new OdbcCommand(sql, connection);
                    return command.ExecuteNonQuery();
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
            
        }
        public override int ValidateParty(ChangedLinkedContactContract party, string _COM_connectionString, string _DTS_connectionString)
        {
            using (var connectionDTS = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connectionDTS.Open();
                    string sql = "SELECT [Supplier No] FROM [Supplier] WHERE [Supplier No] = '" + party.ParentPartyCode + "'";
                    var command = new OdbcCommand(sql, connectionDTS);
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        using (var connection = new OdbcConnection(_COM_connectionString))
                        {
                            try
                            {
                                connection.Open();
                                string sql_COM = "SELECT DocumentReferenceCode "
                                                + "FROM DocumentReference "
                                                + "WHERE DocumentReferenceTypeID = 4 AND "
                                                + "		DocumentReferenceCode = '" + party.ParentPartyCode + "'";
                                var command_COM = new OdbcCommand(sql_COM, connection);
                                var reader_COM = command_COM.ExecuteReader();
                                if (reader_COM.HasRows)
                                {
                                    return 1;
                                }
                                else
                                {
                                    sql_COM = "INSERT INTO [DocumentReference] ([DocumentReferenceTypeID], "
                                            + "								    [DocumentReferenceCode], "
                                            + "								    [DateStamp], "
                                            + "								    [HostName]) "
                                            + "SELECT 4, "
                                            + "	      '" + party.ParentPartyCode + "', "
                                            + "	      '" + DateTime.Now + "', "
                                            + "	      NULL ";
                                    var command_COM1 = new OdbcCommand(sql_COM, connection);
                                    int rows = command_COM1.ExecuteNonQuery();
                                    return 1;
                                }
                            }
                            catch (OdbcException ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    else
                    {
                        return 3;
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
