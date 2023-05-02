using Aquazania.Integration.ServerApp.Factory;

namespace Aquazania.Integration.ServerApp.Client
{
    public class DarielResponse
    {
        public DarielResponse(int NumberOfSuccesses, int NumberOfFailures, string[] errors)
        {
            this.NumberOfSuccesses = NumberOfSuccesses;
            this.NumberOfFailures = NumberOfFailures;
            this.errors = errors;
        }
        public int NumberOfSuccesses { get; set; }
        public int NumberOfFailures { get; set; }
        public string[] errors { get; set; }
    }
}
