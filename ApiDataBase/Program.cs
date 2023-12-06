using System;
using System.Data;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

public class Coord
{
    public double? lat { get; set; }
    public double? lon { get; set; }
}

public class MainInfo
{
    public double? temp { get; set; }
}

public class Weather
{
    public string? description { get; set; }
}

public class WeatherForecast
{
    public string? name { get; set; }
    public Coord? coord { get; set; }
    public MainInfo? main { get; set; }
    public Weather[]? weather { get; set; }
}

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private static string url = "https://api.openweathermap.org/data/2.5/weather?lat=49&lon=32&units=metric&appid=e1973e67babf35775926f02c97a1c364";

    // SQLite connection string
    private static string connectionString = "Data Source=WeatherData.db";

    static async Task Main(string[] args)
    {
        await GetWeather();
        Console.WriteLine("Hello, World!");
    }

    static async Task GetWeather()
    {
        Console.WriteLine("Getting JSON...");
        var responseString = await client.GetStringAsync(url);
        Console.WriteLine("Parsing JSON...");
        WeatherForecast? weatherForecast = JsonSerializer.Deserialize<WeatherForecast>(responseString);

        // Save data to SQLite
        SaveWeatherData(weatherForecast);

        Console.WriteLine($"City: {weatherForecast?.name}");
        Console.WriteLine($"Temperature: {weatherForecast?.main?.temp}°C");
        Console.WriteLine($"Description: {weatherForecast.weather[0]?.description}");
    }

    static void SaveWeatherData(WeatherForecast? weatherForecast)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    // Create a table if it doesn't exist
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS WeatherData (
                                City TEXT,
                                Temperature INTEGER,
                                Description TEXT
                            );";
                        command.ExecuteNonQuery();
                    }

                    // Insert data into the table
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO WeatherData (City, Temperature, Description) VALUES (@City, @Temperature, @Description)";
                        command.Parameters.AddWithValue("@City", weatherForecast?.name ?? "");
                        command.Parameters.AddWithValue("@Temperature", weatherForecast?.main?.temp);
                        command.Parameters.AddWithValue("@Description", weatherForecast?.weather?[0]?.description ?? "");
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving data to SQLite: {ex.Message}");
                    transaction.Rollback();
                }
            }
        }
    }
}
