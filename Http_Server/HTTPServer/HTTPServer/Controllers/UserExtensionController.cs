using Aquazania.Integration.ServerApp.UserExtensionContract;
using Aquazania.Telephony.Integration.Models;
using HTTPServer.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Aquazania.Integration.ServerApp.Controllers
{
    public class UserExtensionController : ApiControllerBase
    {
        [HttpPost(nameof(PostUserExtention))]
        public IActionResult PostUserExtention([FromBody] List<UserContract> Users)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            int rows = 0;
            foreach (var User in Users)
            {
                rows += 1;
                new UserExtension(configuration, User);
            }
            return Content("Successful Update Of " + rows + " Users", "application/json");
        }
    }
}
