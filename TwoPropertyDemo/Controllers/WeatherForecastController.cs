using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TwoPropertyDemo.Domain;

namespace TwoPropertyDemo.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly MyDbContext _myDbContext;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,MyDbContext myDbContext)
        {
            _logger = logger;
            _myDbContext = myDbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            await _myDbContext.AddAsync(new Deal()
            {
                Id = Guid.NewGuid().ToString("n"),
                Market = "A", //±ØÐë´æÔÚÓÚDealRoute
                Time = new DateTimeOffset(new DateTime(2020,1,1))
            });
           await  _myDbContext.SaveChangesAsync();
           return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> Select()
        {
            Console.WriteLine("-------------------------------Select Start------------------------");
            var list= await _myDbContext.Set<Deal>().ToListAsync();
            Console.WriteLine("-------------------------------Select End------------------------");
            return Ok(list);
        }
        [HttpGet]
        public async Task<IActionResult> Select1()
        {
            Console.WriteLine("-------------------------------Select1 Start------------------------");
            var dateTimeOffset = new DateTimeOffset(new DateTime(2020,1,1));
            var list = await _myDbContext.Set<Deal>().Where(o => o.Time == dateTimeOffset).ToListAsync();
            Console.WriteLine("-------------------------------Select1 End------------------------");
            return Ok(list);
        }
        [HttpGet]
        public async Task<IActionResult> Select2()
        {
            Console.WriteLine("-------------------------------Select2 Start------------------------");
            var list = await _myDbContext.Set<Deal>().Where(o => o.Market == "A").ToListAsync();
            Console.WriteLine("-------------------------------Select2 End------------------------");
            return Ok(list);
        }
        [HttpGet]
        public async Task<IActionResult> Select3()
        {
            Console.WriteLine("-------------------------------Select3 Start------------------------");
            var dateTimeOffset = new DateTimeOffset(new DateTime(2020,1,1));
            var list = await _myDbContext.Set<Deal>().Where(o => o.Market == "A"&& o.Time == dateTimeOffset).ToListAsync();
            Console.WriteLine("-------------------------------Select3 End------------------------");
            return Ok(list);
        }
    }
}