using Aquazania.Integration.ServerApp.Factory.MasterPartyContract.Impl;
using Aquazania.Telephony.Integration.Models;
using HTTPServer.Factory.MasterPartyContract.Impl;

namespace HTTPServer.Factory.MasterPartyContract
{
    public class PartyFactory
    {
        enum PartyTypes { Contract, Customer, DeliveryAddress, Supplier, User, Contact, Consumable }

        public static IPartyConvertor Create(ChangedPartyContactContract party)
        {
            if (!Enum.TryParse(party.PartyType, out PartyTypes partyType))
            {
                throw new NotSupportedException($"Party type : {party.PartyType} is not supported.");
            }

            switch (partyType)
            {
                case PartyTypes.Contract:
                    return new ContractParty();
                case PartyTypes.Customer:
                    return new CustomerParty();
                case PartyTypes.DeliveryAddress:
                    return new DeliveryAddressParty();
                case PartyTypes.Supplier:
                    return new SupplierParty();
                case PartyTypes.User:
                    return new UserParty();
                case PartyTypes.Contact:
                    return new ContactParty();
                case PartyTypes.Consumable:
                    return new ConsumableParty();
                default:
                    throw new NotSupportedException($"Party type {partyType} is not supported.");
            }
        }
    }
}