using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using NAudio.Wave;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using System.Transactions;

namespace Aquazania.Integration.ServerApp.Client.CallRecordings
{
    public class CallRecordingRequest
    {
        public async Task SendCallRequest(ITimed_Client _httpClient, string _DTS_connectionString, string darielURL)
        {
            using var connection = new OdbcConnection(_DTS_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                List<CallRecordingRequestContract> callsToFetch = BuildCallRequestList(connection, transaction);
                foreach (var call in callsToFetch)
                {
                    var response = await _httpClient.SendAsync(call, darielURL);
                    if (response.IsSuccessStatusCode)
                    {
                        string message = await response.Content.ReadAsStringAsync();
                        CallRecordingContract result = JsonConvert.DeserializeObject<CallRecordingContract>(message);
                        if (result.Data != null && result.SipCallId != null)
                            if (ConvertStringToWAV(result, connection, transaction))
                                UpdateSyncMasterTable(connection, transaction, result);
                            else
                                UpdateSyncMasterTableFailure(connection, transaction, result);
                    }
                }
                transaction.Commit();
            }
            catch (Exception ex)
            {
                if (Transaction.Current != null)
                    transaction.Rollback();
                throw;
            }
        }
        public void UpdateSyncMasterTableFailure(OdbcConnection connection, OdbcTransaction transaction, CallRecordingContract result)
        {
            try
            {
                string sql = "UPDATE [Temp Call Recording Request] "
                           + "	SET [Attempted] = 1 "
                           + "      [Date Attempted] = '" + DateTime.Now + "'"
                           + "WHERE [Sip Call ID] = " + result.SipCallId;
                var command = new OdbcCommand(sql, connection)
                {
                    Transaction = transaction
                };
                _ = command.ExecuteNonQuery();
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
        public void UpdateSyncMasterTable(OdbcConnection connection, OdbcTransaction transaction, CallRecordingContract result)
        {
            try
            {
                string sql = "UPDATE [Temp Call Recording Request] "
                           + "	SET [Synced] = 1 "
                           + "      [Attempted] = 1 "
                           + "      [Date Attempted] = '" + DateTime.Now + "'"
                           + "WHERE [Sip Call ID] = " + result.SipCallId;
                var command = new OdbcCommand(sql, connection)
                {
                    Transaction = transaction
                };
                _ = command.ExecuteNonQuery();
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
        public List<CallRecordingRequestContract> BuildCallRequestList(OdbcConnection connection, OdbcTransaction transaction)
        {
            List<CallRecordingRequestContract> CallRecordings = new();
            try
            {
                string sql = "SELECT [Sip Call ID] "
                            + "FROM [Temp Call Recording Request] "
                            + "WHERE [Synced] = 0 AND "
                            + "	     [Attempted] = 0 "
                            + "GROUP BY PartyCode ";
                var command = new OdbcCommand(sql, connection)
                {
                    Transaction = transaction
                };
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        CallRecordingRequestContract callRecordingRequest = new()
                        {
                            SipCallId = reader["Sip Call ID"].ToString()
                        };
                        string filePath = @"C:\Tracking Folder\MasterPartyCallRecordingRequests.txt";
                        using (StreamWriter writer = new(filePath, true))
                        {
                            writer.WriteLine();
                        }
                        File.AppendAllText(filePath, JsonConvert.SerializeObject(callRecordingRequest, Formatting.Indented) + ",");
                        CallRecordings.Add(callRecordingRequest);
                    }
                    return CallRecordings;
                }
                else
                {
                    return new List<CallRecordingRequestContract>();
                }
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
        public bool ConvertStringToWAV(CallRecordingContract response, OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                string folderPath = String.Empty;
                string sql = "SELECT Value " +
                             "FROM [Default Value] " +
                             "WHERE [Description] = 'Euphoria Call Recordings File Location'";
                var command = new OdbcCommand(sql, connection)
                {
                    Transaction = transaction
                };
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    folderPath = reader["Value"].ToString() + @"\";
                else
                    return false;
                sql = "SELECT [Start Time] "  
                    + "FROM [Call Result Log] " 
                    + "WHERE [PBX Unique ID] = " + response.SipCallId;
                command = new OdbcCommand(sql, connection)
                {
                    Transaction = transaction
                };
                reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    DateTime startTime = DateTime.Parse(reader["Value"].ToString());
                    folderPath += startTime.Year + @"\" + startTime.Month.ToString("MMMM");
                }
                else
                    return false;
                string fileName = response.SipCallId + ".wav";
                string filePath = Path.Combine(folderPath, fileName);
                byte[] wavBytes = Convert.FromBase64String(response.Data);
                if (ValidateWavFile(wavBytes))
                {
                    using FileStream fileStream = new(filePath, FileMode.Create);
                    fileStream.Write(wavBytes, 0, wavBytes.Length);
                    return true;
                } return false;
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
        private static bool ValidateWavFile(byte[] wavBytes)
        {
            // Check the length to ensure it contains the necessary header information
            if (wavBytes.Length < 44)
                return false;
            // Check the RIFF header signature
            if (wavBytes[0] != 0x52 || wavBytes[1] != 0x49 || wavBytes[2] != 0x46 || wavBytes[3] != 0x46)
                return false;
            // Check the file size
            int fileSize = BitConverter.ToInt32(wavBytes, 4) + 8;  // File size + 8 bytes for the RIFF header
            if (fileSize != wavBytes.Length)
                return false;
            // Check the WAV format
            if (wavBytes[20] != 0x66 || wavBytes[21] != 0x6D || wavBytes[22] != 0x74 || wavBytes[23] != 0x20)
                return false;
            // Check the audio format is PCM
            if (wavBytes[34] != 0x01 || wavBytes[35] != 0x00)
                return false;

            return true;
        }
    }
}
