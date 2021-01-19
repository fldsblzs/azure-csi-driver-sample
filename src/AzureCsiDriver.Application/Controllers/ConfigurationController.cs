using AzureCsiDriver.Application.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AzureCsiDriver.Application.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly NotificationOptions _notificationOptions;

        public ConfigurationController(IOptions<NotificationOptions> notificationOptions)
        {
            _notificationOptions = notificationOptions.Value;
        }
        
        [HttpGet]
        public IActionResult Get() => Ok(_notificationOptions);
    }
}