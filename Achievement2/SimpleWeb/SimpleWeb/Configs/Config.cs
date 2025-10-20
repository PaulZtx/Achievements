using System.ComponentModel.DataAnnotations;
using NLog;

namespace SimpleWeb.Configs;

public static class Config
{
    /// <summary>
    /// Имя конфигурационного файла
    /// </summary>
    private const string ConfigurationFileName = "appsettings.json";

    /// <summary>
    /// Протоколирование
    /// </summary>
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Конфигурация
    /// </summary>
    private static readonly IConfigurationRoot config;

    /// <summary>
    /// Чтение конфигурации
    /// </summary>
    static Config()
    {
        string name = Path.Combine(AppContext.BaseDirectory, ConfigurationFileName);
        if (!File.Exists(name))
        {
            Log.Fatal($"Файл конфигурации не найден: {name}");
            Environment.Exit(1);
        }

        config = new ConfigurationBuilder().AddJsonFile(name, optional: false).Build();

        Log.Info($"Загружен файл конфигурации: {name}");
    }

    /// <summary>
    /// Чтение секции в виде объекта
    /// <para>Имя секции должно соответствовать имени класса</para>
    /// </summary>
    /// <typeparam name="T">Раздел конфигурации</typeparam>
    /// <returns>Экземпляр раздела конфигурации (пустой, если в файле нет требуемой секции)</returns>
    public static T GetSection<T>() where T : new()
    {
        string name = typeof(T).Name;

        T? result = config.GetSection(name).Get<T>();

        if (result == null)
        {
            Log.Trace($"В файле конфигурации отсутствует или пустая секция '{name}'");
            result = new T();
        }

        // Если секция конфигурации поддерживает валидацию, провести валидацию и запротоколировать все ошибки
        if (result is IValidatableObject validatable)
        {
            var validationContext = new ValidationContext(validatable);

            IEnumerable<ValidationResult> validationResult = validatable.Validate(validationContext);

            foreach (ValidationResult item in validationResult)
            {
                Log.Error($"{item.ErrorMessage}");
            }
        }

        return result;
    }
}