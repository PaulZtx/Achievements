using NLog;
using SimpleWeb.Configs;
using StackExchange.Redis;

namespace SimpleWeb;

/// <summary>
/// Сервис для работы с redis
/// </summary>
public class RedisService
{
    /// <summary>
    /// Redis API
    /// </summary>
    private readonly IConnectionMultiplexer _redis;
    
    /// <summary>
    /// Redis db
    /// </summary>
    private readonly IDatabase _db;
    
    /// <summary>
    /// .ctor
    /// </summary>
    public RedisService()
    {
        var connectionString = Config.GetSection<RedisConfig>();
        _redis = ConnectionMultiplexer.Connect(connectionString.ConnectionString);
        _db = _redis.GetDatabase();
    }
    
    /// <summary>
    /// Обработка числа
    /// </summary>
    /// <param name="number">Число</param>
    /// <returns></returns>
    public async Task<SequenceResponse> ProcessNumberAsync(int number)
    {
        try
        {
            string numberKey = $"number:{number}";
            string nextNumberKey = $"number:{number + 1}";

            
            if (await _db.KeyExistsAsync(numberKey))
            {
                return new SequenceResponse 
                { 
                    Success = false, 
                    Error = "Ошибка: число уже было обработано" 
                };
            }

            if (await _db.KeyExistsAsync(nextNumberKey))
            {
                return new SequenceResponse 
                { 
                    Success = false, 
                    Error = $"Ошибка: число {number} на единицу меньше уже обработанного числа {number + 1}" 
                };
            }
            
            await _db.StringSetAsync(numberKey, "processed"); 

            return new SequenceResponse
            {
                Success = true,
                Result = number
            };
        }
        catch (RedisConnectionException ex)
        {
            return new SequenceResponse { Success = false, Error = ex.Message };
        }
    }
}