using Aquazania.Telephony.Integration.Models;
using HTTPServer.Factory.MasterPartyContract.Impl;

namespace HTTPServer.Factory.MasterPartyContract
{
    public class PartyFactory
    {
        enum PartyTypes { Contract, Customer, DeliveryAddress, Supplier, User, Contact }

        public static IPartyConvertor Create(ChangedPartyContactContract party)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            if (!Enum.TryParse(party.PartyType, out PartyTypes partyType))
            {
                throw new NotSupportedException($"Party type {party.PartyType} is not supported.");
            }

            switch (partyType)
            {
                case PartyTypes.Contract:
                    return new ContractParty(configuration);
                case PartyTypes.Customer:
                    return new CustomerParty(configuration);
                case PartyTypes.DeliveryAddress:
                    return new DeliveryAddressParty(configuration);
                case PartyTypes.Supplier:
                    return new SupplierParty(configuration);
                case PartyTypes.User:
                    return new UserParty(configuration);
                case PartyTypes.Contact:
                    return new ContactParty(configuration);
                default:
                    throw new NotSupportedException($"Party type {partyType} is not supported.");
            }
        }
    }
}