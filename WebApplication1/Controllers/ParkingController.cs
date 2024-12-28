using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Parkingmap.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Parkingmap.Controllers
{

    public class ParkingController : Controller
    {
            public async Task<IActionResult> Index()
            {
                await UpdateFromApi();  // 自動更新
                return View();
            }

        private readonly string _connectionString = "Server=127.0.0.1;Database=parkingmap;User=root;Password=a150731071;Port=3306;";
        private readonly string _apiUrl = "https://motoretag.taichung.gov.tw/DataAPI/api/ParkingSpotListAPIV2/";
        private readonly string _logPath;

        public ParkingController()
        {
            _logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ParkingMap", "logs");
            if (!Directory.Exists(_logPath))
            {
                Directory.CreateDirectory(_logPath);
            }
        }

        private void LogMessage(string message)
        {
            var logFile = Path.Combine(_logPath, $"log_{DateTime.Now:yyyyMMdd}.txt");
            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}{Environment.NewLine}";
            System.IO.File.AppendAllText(logFile, logMessage);
        }

        public async Task<IActionResult> UpdateFromApi()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(_apiUrl);
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();
                    LogMessage($"API Response received: {content.Length} characters");

                    var events = JsonConvert.DeserializeObject<List<Event>>(content);
                    if (events == null || events.Count == 0)
                    {
                        throw new Exception("No events data received from API");
                    }

                    foreach (var eventData in events)
                    {
                        await UpdateDatabase(eventData);
                    }
                    LogMessage($"Database update completed successfully for {events.Count} events");
                    return Ok($"資料更新成功: {events.Count} 筆資料");
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error occurred: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nInner Exception: {ex.InnerException.Message}";
                }
                LogMessage(errorMessage);
                return BadRequest($"更新失敗: {ex.Message}");
            }
        }
        private async Task UpdateDatabase(Event eventData)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = @"
                                INSERT INTO Events (ID, Name, EnName, X, Y)
                                VALUES (@ID, @Name, @EnName, @X, @Y)
                                ON DUPLICATE KEY UPDATE 
                                Name = VALUES(Name),
                                EnName = VALUES(EnName),
                                X = VALUES(X),
                                Y = VALUES(Y)";

                            command.Parameters.AddWithValue("@ID", eventData.ID);
                            command.Parameters.AddWithValue("@Name", eventData.Name);
                            command.Parameters.AddWithValue("@EnName", eventData.EnName);
                            command.Parameters.AddWithValue("@X", eventData.X);
                            command.Parameters.AddWithValue("@Y", eventData.Y);

                            await command.ExecuteNonQueryAsync();
                        }

                        if (eventData.ParkingLots != null)
                        {
                            foreach (var lot in eventData.ParkingLots)
                            {
                                using (var command = connection.CreateCommand())
                                {
                                    command.Transaction = transaction;
                                    command.CommandText = @"
                                        INSERT INTO ParkingLots (ID, Position, EnPosition, X, Y, 
                                            TotalCar, AvailableCar, AvailableCarRGB,
                                            TotalMotor, AvailableMotor, AvailableMotorRGB,
                                            ShowVauleCar, ShowVauleMotor, Notes, EnNotes,
                                            Type, Updatetime)
                                        VALUES (@ID, @Position, @EnPosition, @X, @Y,
                                            @TotalCar, @AvailableCar, @AvailableCarRGB,
                                            @TotalMotor, @AvailableMotor, @AvailableMotorRGB,
                                            @ShowVauleCar, @ShowVauleMotor, @Notes, @EnNotes,
                                            @Type, @Updatetime)
                                        ON DUPLICATE KEY UPDATE 
                                            Position = VALUES(Position),
                                            EnPosition = VALUES(EnPosition),
                                            X = VALUES(X),
                                            Y = VALUES(Y),
                                            TotalCar = VALUES(TotalCar),
                                            AvailableCar = VALUES(AvailableCar),
                                            AvailableCarRGB = VALUES(AvailableCarRGB),
                                            TotalMotor = VALUES(TotalMotor),
                                            AvailableMotor = VALUES(AvailableMotor),
                                            AvailableMotorRGB = VALUES(AvailableMotorRGB),
                                            ShowVauleCar = VALUES(ShowVauleCar),
                                            ShowVauleMotor = VALUES(ShowVauleMotor),
                                            Notes = VALUES(Notes),
                                            EnNotes = VALUES(EnNotes),
                                            Type = VALUES(Type),
                                            Updatetime = VALUES(Updatetime)";

                                    command.Parameters.AddWithValue("@ID", lot.ID);
                                    command.Parameters.AddWithValue("@Position", lot.Position);
                                    command.Parameters.AddWithValue("@EnPosition", lot.EnPosition);
                                    command.Parameters.AddWithValue("@X", lot.X);
                                    command.Parameters.AddWithValue("@Y", lot.Y);
                                    command.Parameters.AddWithValue("@TotalCar", lot.TotalCar);
                                    command.Parameters.AddWithValue("@AvailableCar", lot.AvailableCar);
                                    command.Parameters.AddWithValue("@AvailableCarRGB", lot.AvailableCarRGB);
                                    command.Parameters.AddWithValue("@TotalMotor", lot.TotalMotor);
                                    command.Parameters.AddWithValue("@AvailableMotor", lot.AvailableMotor);
                                    command.Parameters.AddWithValue("@AvailableMotorRGB", lot.AvailableMotorRGB);
                                    command.Parameters.AddWithValue("@ShowVauleCar", lot.ShowVauleCar);
                                    command.Parameters.AddWithValue("@ShowVauleMotor", lot.ShowVauleMotor);
                                    command.Parameters.AddWithValue("@Notes", lot.Notes);
                                    command.Parameters.AddWithValue("@EnNotes", lot.EnNotes);
                                    command.Parameters.AddWithValue("@Type", lot.Type);
                                    command.Parameters.AddWithValue("@Updatetime", lot.Updatetime);

                                    await command.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        LogMessage($"Database Error for Event {eventData.ID}: {ex.Message}");
                        throw;
                    }
                }
            }

        }
        // ParkingController.cs
        public IActionResult Map()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Search(string keyword)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Connection Error: {ex.Message}");
                    return Json(new { error = $"Database connection failed: {ex.Message}" });
                }
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM ParkingLots WHERE Position LIKE @keyword";
                command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

                var results = new List<ParkingLot>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(new ParkingLot
                        {
                            ID = reader.GetString("ID"),
                            Position = reader.GetString("Position"),
                            AvailableCar = reader.GetInt32("AvailableCar"),
                            TotalCar = reader.GetInt32("TotalCar"),
                            Notes = reader.GetString("Notes")
                            // 添加其他需要的欄位
                        });
                    }
                }

                return Json(results);
            }
        }
    }
}