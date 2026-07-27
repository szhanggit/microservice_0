using System.Globalization;
using MySqlConnector;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] (JobMonitorService) {Message:lj}{NewLine}{Exception}",
        formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MySql")
    ?? throw new InvalidOperationException("Environment variable 'ConnectionStrings__MySql' was not found.");

var minDelaySeconds = ParseIntOrDefault(Environment.GetEnvironmentVariable("JobDelay__MinSeconds"), 0);
var maxDelaySeconds = ParseIntOrDefault(Environment.GetEnvironmentVariable("JobDelay__MaxSeconds"), 90);

var jobId = Guid.NewGuid();
var startedAt = DateTime.UtcNow;

await using var connection = new MySqlConnection(connectionString);
await connection.OpenAsync();

await using (var insertCommand = connection.CreateCommand())
{
    insertCommand.CommandText =
        "INSERT INTO JobMonitor (Id, JobStartDateTime, JobEndDateTime, Status) " +
        "VALUES (@id, @startedAt, NULL, 'Running');";
    insertCommand.Parameters.AddWithValue("@id", jobId.ToString());
    insertCommand.Parameters.AddWithValue("@startedAt", startedAt);
    await insertCommand.ExecuteNonQueryAsync();
}

Log.Information("Job {JobId} started at {StartedAt:O}", jobId, startedAt);

try
{
    var delaySeconds = Random.Shared.Next(minDelaySeconds, maxDelaySeconds + 1);
    Log.Information("Job {JobId} sleeping for {DelaySeconds}s", jobId, delaySeconds);
    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

    await using var updateCommand = connection.CreateCommand();
    updateCommand.CommandText =
        "UPDATE JobMonitor SET JobEndDateTime = @endedAt, Status = 'Succeeded' WHERE Id = @id;";
    updateCommand.Parameters.AddWithValue("@endedAt", DateTime.UtcNow);
    updateCommand.Parameters.AddWithValue("@id", jobId.ToString());
    await updateCommand.ExecuteNonQueryAsync();

    Log.Information("Job {JobId} succeeded", jobId);
}
catch (Exception ex)
{
    Log.Error(ex, "Job {JobId} failed", jobId);

    await using var failCommand = connection.CreateCommand();
    failCommand.CommandText =
        "UPDATE JobMonitor SET JobEndDateTime = @endedAt, Status = 'Failed' WHERE Id = @id;";
    failCommand.Parameters.AddWithValue("@endedAt", DateTime.UtcNow);
    failCommand.Parameters.AddWithValue("@id", jobId.ToString());
    await failCommand.ExecuteNonQueryAsync();

    Log.CloseAndFlush();
    throw;
}

Log.CloseAndFlush();
return;

static int ParseIntOrDefault(string? value, int fallback) =>
    int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
