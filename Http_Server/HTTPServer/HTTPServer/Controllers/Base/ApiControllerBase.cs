using Microsoft.AspNetCore.Mvc;

namespace HTTPServer.Controllers.Base
{
    [ApiController]
    [Route("[controller]")]
    public abstract class ApiControllerBase : ControllerBase
    {
    }
}
