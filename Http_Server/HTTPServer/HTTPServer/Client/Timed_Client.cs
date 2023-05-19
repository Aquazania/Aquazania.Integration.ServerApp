using Aquazania.Integration.ServerApp.Client.Consumable;
using Aquazania.Integration.ServerApp.Client.Contact;
using Aquazania.Integration.ServerApp.Client.Contract;
using Aquazania.Integration.ServerApp.Client.DeliveryAddress;
using Aquazania.Integration.ServerApp.Client.Interfaces;
using Aquazania.Integration.ServerApp.Client.MasterParties;
using Aquazania.Integration.ServerApp.Client.MasterParties.Contract;
using Aquazania.Integration.ServerApp.Client.Supplier;
using Aquazania.Integration.ServerApp.Client.SupplierDeliveryAddress;
using Aquazania.Integration.ServerApp.Client.UnlinkingContacts;
using Aquazania.Integration.ServerApp.Client.User;
using Aquazania.Integration.ServerApp.Client.UserExtension;
using HTTPServer.Client.Customer;


namespace HTTPServer.Client
{
    public class Timed_Client 
    {
        private Timer _timer;

        private readonly ITimed_Client _httpClient;
        private string _DTS_connectionString;
        private string _COM_connectionString;
        private string _darielURL;
        private string _darielURLContact;
        private string _darielURLUsers;
        private static bool isRunning = false;
        public Timed_Client(ITimed_Client timed_client)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            _httpClient = timed_client;
            _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
            _COM_connectionString = configuration.GetConnectionString("Communicator_Connection");
            _darielURL = configuration.GetSection("darielURL").Value;
            _darielURLContact = configuration.GetSection("darielURLContact").Value;
            _darielURLUsers = configuration.GetSection("darielUsers").Value; 
        }
        public void StartTimer()
        {
            _timer = new Timer(CallBackFunctions, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
        }
        private async void CallBackFunctions(object state)
        {
            if (isRunning)
            {
                return;
            }

            isRunning = true;

            string filePath = @"C:\Tracking Folder\TimesRan.txt";
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine();
            }
            File.AppendAllText(filePath, "I ran at : " + DateTime.Now);

            List<IMasterParty> masterParties = new List<IMasterParty>()
            {
                new MasterContactParty(),
                new MasterContractParty(),
                new MasterCustomerParty(),
                new MasterDeliveryAddressParty(),
                new MasterSupplierParty(),
                new MasterSupplierDeliveryAddressParty(),
                new MasterUserParty(),
                new MasterConsumableParty(),
                new MasterConsumableContractParty(),
                new MasterDeliveryAddressContractParty()
            };

            List<IMasterLinkedParty> masterLinkedParties = new List<IMasterLinkedParty>()
            {
                new MasterContactLinkedParty(),
                new MasterContractLinkedParty(),
                new MasterCustomerLinkedParty(),
                new MasterDeliveryAddressLinkedParty(),
                new MasterSupplierLinkedParty(),
                new MasterSupplierDeliveryAddressLinkedParty(),
                new MasterUserLinkedParty()
            };

            List<IMasterUnlinkParty> masterUnlinkParties = new List<IMasterUnlinkParty>()
            {
                new UnlinkContactLinkedParty(),
                new UnlinkContractLinkedParty(),
                new UnlinkCustomerLinkedParty(),
                new UnlinkDeliveryAddressLinkedParty(),
                new UnlinkSupplierLinkedParty(),
                new UnlinkSupplierDeliveryAddressLinkedParty(),
                new UnlinkUserLinkedParty()
            };

            UserExtensionContract users = new UserExtensionContract(_darielURLUsers);
            await users.SendMasterParty(_httpClient, _DTS_connectionString);

            foreach (IMasterParty party in masterParties)
            {
                await party.SendMasterParty(_httpClient, _DTS_connectionString, _darielURL);
            }

            foreach (IMasterLinkedParty linkedParty in masterLinkedParties)
            {
                await linkedParty.SendMasterLinkedParty(_httpClient, _COM_connectionString, _DTS_connectionString, _darielURLContact);
            }

            foreach (IMasterUnlinkParty unlinkedParty in masterUnlinkParties)
            {
                await unlinkedParty.SendMasterLinkedParty(_httpClient, _COM_connectionString, _DTS_connectionString, _darielURLContact);
            }

            isRunning = false;
        }
    }
}
