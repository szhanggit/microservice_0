using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Logging.Extensions;
using UserRepositoryService.Interceptors;
using UserRepositoryService.Persistence;
using UserRepositoryService.Repositories;
using UserRepositoryService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedSerilogLogging("UserRepositoryService");

var connectionString = builder.Configuration.GetConnectionString("MySql")
    ?? throw new InvalidOperationException("Connection string 'MySql' was not found.");

builder.Services.AddDbContext<UserRepositoryDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddGrpc(options => options.Interceptors.Add<ExceptionHandlingInterceptor>());
builder.Services.AddGrpcReflection();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapGrpcService<UserGrpcService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
