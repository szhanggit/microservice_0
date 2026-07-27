using Microsoft.OpenApi.Models;
using Serilog;
using Shared.Common.Extensions;
using Shared.Common.Validators;
using Shared.Contracts.Requests;
using Shared.Logging.Extensions;
using UserManagementGateway.Services;
using UserManagementGateway.Validation;
using ManagementProto = Shared.Protos.UserManagement;

// Enables calling gRPC servers over unencrypted HTTP/2 (h2c), which is how services
// talk to each other inside the docker-compose/Kubernetes network - TLS terminates at the edge.
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedSerilogLogging("UserManagementGateway");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "User Management Gateway API", Version = "v1" });
});

var managementServiceAddress = builder.Configuration["GrpcClients:UserManagementService"]
    ?? throw new InvalidOperationException("Configuration 'GrpcClients:UserManagementService' was not found.");

builder.Services.AddGrpcClient<ManagementProto.UserManagementGrpcService.UserManagementGrpcServiceClient>(options =>
{
    options.Address = new Uri(managementServiceAddress);
});

builder.Services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddSharedExceptionHandling();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Containers bind plain HTTP only (ASPNETCORE_URLS=http://+:8080) - TLS terminates at the
// ingress/reverse proxy, not in-process. Only redirect when an HTTPS URL is actually bound,
// which is the case for local `dotnet run` via launchSettings.json.
var boundUrls = builder.Configuration["ASPNETCORE_URLS"] ?? string.Empty;
if (boundUrls.Contains("https", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();
app.MapSharedHealthChecks();

app.Run();
