using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client.Customer;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.Text;

namespace HTTPServer.Client
{
    public class Timed_Client 
    {
        private Timer _timer;

        private readonly ITimed_Client _httpClient;
        private string _DTS_connectionString;
        private string _COM_connectionString;
        public Timed_Client(ITimed_Client timed_client)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            _httpClient = timed_client;
            _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
            _COM_connectionString = configuration.GetConnectionString("Communicator_Connection");
        }

        public void StartTimer()
        {
            _timer = new Timer(CallBackFunctions, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        private void CallBackFunctions(object state)
        {
            MasterCustomerParty master = new MasterCustomerParty(); 
            master.SendMasterCustomerParty(_httpClient, _DTS_connectionString);
            MasterCustomerLinkedParty linkedParty = new MasterCustomerLinkedParty();
            linkedParty.SendMasterCustomerLinkedParty(_httpClient, _COM_connectionString);
        }
    }
}
