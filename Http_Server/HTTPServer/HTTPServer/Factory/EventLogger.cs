using System.Diagnostics;

namespace Aquazania.Integration.ServerApp.Factory
{
    public static class EventLogger
    {
        public static void logerror(string message)
        {
            try 
            {
                EventLog.WriteEntry("Integration.API", message, EventLogEntryType.Error);
            } catch (Exception e) 
            { }
            
        }
    }
}
