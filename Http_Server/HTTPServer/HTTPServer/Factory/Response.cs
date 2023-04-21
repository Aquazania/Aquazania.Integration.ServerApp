namespace Aquazania.Integration.ServerApp.Factory
{
    public class Response
    {
        public Response(int NumberOfSuccesses, int NumberOfFailures,Error[] errors) 
        {
            this.NumberOfSuccesses = NumberOfSuccesses;
            this.NumberOfFailures = NumberOfFailures;   
            this.errors = errors;
        }
        public int NumberOfSuccesses { get; set; }
        public int NumberOfFailures { get; set;}
        public Error[] errors { get; set; }
    }
}
