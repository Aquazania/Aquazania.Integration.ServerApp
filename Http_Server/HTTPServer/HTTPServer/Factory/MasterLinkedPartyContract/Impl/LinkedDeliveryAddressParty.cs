using Aquazania.Telephony.Integration.Models;
using HTTPServer.Factory.MasterLinkedPartyContract;
using System.Data.Odbc;
using System.Diagnostics.Eventing.Reader;

namespace Aquazania.Integration.ServerApp.Factory.MasterLinkedPartyContract.Impl
{
    public class LinkedDeliveryAddressParty : ILinkedPartyConvertor
    {
        private string _COM_connectionString;
        private string _DTS_connectionString;
        public LinkedDeliveryAddressParty(IConfiguration configuration)
        {
            _COM_connectionString = configuration.GetConnectionString("Communicator_Connection");
            _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
        }
        public async Task Convert(ChangedLinkedContactContract party)
        {
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int rows = 0;
                        if (ValidateParty(party, connection, transaction) == 1)
                        {
                            rows += UpdateRequired(party, connection, transaction);
                        }
                        else
                        {
                            throw new KeyNotFoundException($"Code : {party.ParentPartyCode} was not found within the database");
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
        public int DoInsert(ChangedLinkedContactContract party, OdbcConnection connection, OdbcTransaction transaction, int updatetype = 1)
        {
            int refenceTypeID = 0;
            if (updatetype == 1)
                refenceTypeID = 14;
            else
                refenceTypeID = 16;
            try
            {
                string sql = "DECLARE @ContactID INT "
                            + "DECLARE @ExternalReferenceID INT "
                            + "SELECT @ExternalReferenceID = MAX(DocumentReferenceID) "
                            + "FROM DocumentReference "
                            + "WHERE DocumentReferenceTypeID = " + refenceTypeID + " AND "
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
                command.Transaction = transaction;
                return command.ExecuteNonQuery();
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
            
        }
        public int PerformUpdate(ChangedLinkedContactContract party, int ContactID, OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                connection.Open();
                string sql = "UPDATE [ContactPoint] "
                            + "  SET [ContactPointValue] = '" + party.PhoneNumber + "' "
                            + "WHERE [ContactID] = " + ContactID;
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
                return command.ExecuteNonQuery();
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
        public int UpdateRequired(ChangedLinkedContactContract party , OdbcConnection connection, OdbcTransaction transaction, int updatetype = 1)
        {
            int refenceTypeID = 0;
            if (updatetype == 1)
                refenceTypeID = 14;
            else
                refenceTypeID = 16;
            try
            {
                string sql = "SELECT  v.ContactPointValue, "
                            + "		  v.ContactPointTypeID, "
                            + "		  cpt.[Description], "
                            + "		  max(v.ContactID) ContactID, "
                            + "		  v.ContactName, "
                            + "		  v.ContactLastName, "
                            + "		  v.JobDescription, "
                            + "		  v.SupplierOrdersContact "
                            + "FROM  viewContactDocumentReference V "
                            + "	INNER JOIN ContactPointType cpt on "
                            + "		cpt.ContactPointTypeID = v.ContactPointTypeID "
                            + "	INNER JOIN	DocumentReference dr ON "
                            + "		v.ExternalReferenceID = dr.DocumentReferenceID AND "
                            + "		dr.DocumentReferenceTypeID = " + refenceTypeID + " AND "
                            + $"		dr.DocumentReferenceCode = '{party.ParentPartyCode}' "
                            + "WHERE v.ContactPointTypeID = isnull(2, v.ContactPointTypeID) "
                            + "GROUP BY v.ContactPointValue, "
                            + "		   v.ContactPointTypeID, "
                            + "		   cpt.[Description], "
                            + "		   v.ContactName, "
                            + "		   v.ContactLastName, "
                            + "		   v.JobDescription, "
                            + "		   v.SupplierOrdersContact ";
                var commandContacts = new OdbcCommand(sql, connection);
                commandContacts.Transaction = transaction;
                var readerContacts = commandContacts.ExecuteReader();
                if (readerContacts.HasRows)
                {
                    while (readerContacts.Read())
                    {
                        if (party.ContactFullName == readerContacts["ContactName"].ToString())
                        {
                            if (party.PhoneNumber == readerContacts["ContactPointValue"].ToString())
                                return 0;
                            else
                                return PerformUpdate(party, System.Convert.ToInt32(readerContacts["ContactID"].ToString()), connection, transaction);
                        }
                        if (party.ContactFullName == readerContacts["ContactName"] + " " + readerContacts["ContactLastName"])
                        {
                            if (party.PhoneNumber == readerContacts["ContactPointValue"].ToString())
                                return 0;
                            else
                                return PerformUpdate(party, System.Convert.ToInt32(readerContacts["ContactID"].ToString()), connection, transaction);
                        }
                    }
                    return DoInsert(party, connection, transaction, updatetype);
                }
                else
                {
                    return DoInsert(party, connection, transaction, updatetype);
                }
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
        public int ValidateParty(ChangedLinkedContactContract party, OdbcConnection connection, OdbcTransaction transaction)
        {
            using (var connectionDTS = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connectionDTS.Open();
                    string sql = "SELECT [Delivery Address Code] FROM [Delivery Address] WHERE [Delivery Address Code] = '" + party.ParentPartyCode + "'";
                    var command = new OdbcCommand(sql, connectionDTS);
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        try
                        {
                            string sql_COM = "SELECT DocumentReferenceCode "
                                            + "FROM DocumentReference "
                                            + "WHERE DocumentReferenceTypeID = 14 AND "
                                            + "		DocumentReferenceCode = '" + party.ParentPartyCode + "'";
                            var command_COM = new OdbcCommand(sql_COM, connection);
                            command_COM.Transaction = transaction;
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
                                        + "SELECT 14, "
                                        + "	      '" + party.ParentPartyCode + "', "
                                        + "	      '" + DateTime.Now + "', "
                                        + "	      NULL ";
                                var command_COM1 = new OdbcCommand(sql_COM, connection);
                                command_COM1.Transaction = transaction;
                                int rows = command_COM1.ExecuteNonQuery();
                                return 1;
                            }
                        }
                        catch (OdbcException ex)
                        {
                            throw ex;
                        }
                    }
                    else
                    {
                        using (var connection_Supplier = new OdbcConnection(_DTS_connectionString))
                        {
                            try
                            {
                                connection_Supplier.Open();
                                string sql_Supplier = "SELECT [Delivery Address Code] FROM [Supplier Delivery Address] WHERE [Delivery Address Code] = '" + party.ParentPartyCode + "'";
                                var command_Supplier = new OdbcCommand(sql_Supplier, connection_Supplier);
                                var reader_Supplier = command_Supplier.ExecuteReader();
                                if (reader_Supplier.HasRows)
                                {
                                    try
                                    {
                                        string sql_COM = "SELECT DocumentReferenceCode "
                                                        + "FROM DocumentReference "
                                                        + "WHERE DocumentReferenceTypeID = 16 AND "
                                                        + "		DocumentReferenceCode = '" + party.ParentPartyCode + "'";
                                        var command_COM = new OdbcCommand(sql_COM, connection);
                                        command_COM.Transaction = transaction;
                                        var reader_COM = command_COM.ExecuteReader();
                                        if (reader_COM.HasRows)
                                        {
                                            return 2;
                                        }
                                        else
                                        {
                                            sql_COM = "INSERT INTO [DocumentReference] ([DocumentReferenceTypeID], "
                                                    + "								    [DocumentReferenceCode], "
                                                    + "								    [DateStamp], "
                                                    + "								    [HostName]) "
                                                    + "SELECT 16, "
                                                    + "	      '" + party.ParentPartyCode + "', "
                                                    + "	      '" + DateTime.Now + "', "
                                                    + "	      NULL ";
                                            var command_COM1 = new OdbcCommand(sql_COM, connection);
                                            command_COM1.Transaction = transaction;
                                            int rows = command_COM1.ExecuteNonQuery();
                                            return 2;
                                        }
                                    }
                                    catch (OdbcException ex)
                                    {
                                        throw ex;
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
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
    }
}
