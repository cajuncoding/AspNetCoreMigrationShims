using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using AspNetCoreMigrationShims.NewtonsoftJson.SampleWebApp.Models;

namespace AspNetCoreMigrationShims.NewtonsoftJson.SampleWebApp.Controllers
{
    [Route("api")]
    public class EchoController : Controller
    {

        [HttpPost]
        [Route("echo")]
        public EchoRequest Echo([FromBody] EchoRequest echoRequest)
        {
            //The Echo Model Binding should populate the model with high compatibility to legacy NET Framework MVC
            //  (e.g. without Exceptions being thrown when invalid payloads are provided).
            Debug.WriteLine(JsonConvert.SerializeObject(ModelState, Formatting.Indented));

            return echoRequest;
        }
    }
}
