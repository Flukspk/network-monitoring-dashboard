using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    

    public class TargetRequest
    {
        public string Target { get; set; }
    }
}
