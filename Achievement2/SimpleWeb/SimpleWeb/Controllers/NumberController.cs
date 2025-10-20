using NLog;

namespace SimpleWeb.Controllers;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// 
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NumberController : ControllerBase
{
    /// <summary>
    /// Максимальное значение N
    /// </summary>
    private const ulong N = ulong.MaxValue; // Максимальное значение для типа UInt64
    
    /// <summary>
    /// Протоколирование
    /// </summary>
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    /// <summary>
    /// Сервис redis
    /// </summary>
    private readonly RedisService _redisService;

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="redisService">Сервис Redis</param>
    public NumberController(RedisService redisService)
    {
        _redisService = redisService;
    }

    /// <summary>
    /// Обработка числа
    /// </summary>
    /// <param name="num"></param>
    /// <returns></returns>
    [HttpPost("process")]
    public async Task<IActionResult> ProcessNumber([FromBody]int num)
    {
        if (num <= 0)
        {
            return BadRequest($"Число должно находиться в интервале от 0 до {N}");
        }
        
        var result = await _redisService.ProcessNumberAsync(num);

        if (result.Success) 
            return Ok(result.Result + 1);
        
        Log.Error(result.Error);
        return BadRequest(result.Error);
    }
}