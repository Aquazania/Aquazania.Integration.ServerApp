namespace Aquazania.Integration.ServerApp.Factory
{
    public class Error
    {
        public Error(string PartyCode, string PartyType, string[] ErrorMessages) 
        {
            this.PartyCode = PartyCode;
            this.PartyType = PartyType;
            this.ErrorMessages = ErrorMessages; 
        }
        public string PartyCode { get; set; }
        public string PartyType { get; set; }
        public string[] ErrorMessages { get; set; }
    }
}
