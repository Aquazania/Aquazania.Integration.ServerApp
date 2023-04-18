using Aquazania.Telephony.Integration.Models;
using System.Data.Odbc;
using System.IO;

namespace Aquazania.Integration.ServerApp.UserExtensionContract
{
    public class UserExtension
    {
        private string _DTS_connectionString;
        public UserExtension(IConfiguration configuration, UserContract User) 
        {
            _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
            UpdateUser(User);
        }
        public void UpdateUser(UserContract user)
        {
            if (ValidateUser(user))
            {
                int rows = UpdateRequired(user);
            }
            else
                throw new KeyNotFoundException($"Code : {user.UserName} was not found within the database");
        }

        private bool ValidateUser(UserContract user)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT [User Name] FROM [User] WHERE [User Name] = '" + user.UserName + "'";
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

        public int UpdateRequired(UserContract user)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    int rows = 0;
                    connection.Open();
                    string sql = "SELECT * FROM [User] WHERE [User Name] = '" + user.UserName + "'";
                    var command = new OdbcCommand(sql, connection);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (user.Extension != reader["PBX Extension"].ToString())
                            rows += PerformUpdate("PBX Extension",
                                                  user.Extension,
                                                  user);
                    }
                    return rows;
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }

        private int PerformUpdate(string updatedField, string newValue, UserContract user)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "UPDATE [User] "
                               + "	SET [" + updatedField + "] = '" + newValue + "' "
                               + "WHERE [User Name] = '" + user.UserName + "'";
                    var command = new OdbcCommand(sql, connection);
                    return command.ExecuteNonQuery();
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
    }
}
