using Aquazania.Telephony.Integration.Models;

namespace Aquazania.Integration.ServerApp.PostCallHistoryEntryContract
{
    public class CallHistoryEntry
    {
        private static string _DTS_connectionString;
        public static int RecordHistory(CallHistoryEntryContract callresult)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");

            return 0;
        }
    }
}
