using System.Threading.Tasks;
using Glados.Discord.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace GLaDOS.Scheduler.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IHelloWorld _helloWorld;
    public WeatherForecastController(IHelloWorld helloWorld)
    {
        _helloWorld = helloWorld;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task Get()
    {
      await  _helloWorld.SayHelloAsync();
    }
}