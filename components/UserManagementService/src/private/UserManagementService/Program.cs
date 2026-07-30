using Amazon.XRay.Recorder.Handlers.System.Net;
using OpenTelemetry.Trace;
using Serilog;
using Shared.Common.Extensions;
using Shared.Common.Validators;
using Shared.Logging.Extensions;
using Shared.Protos.UserManagement;
using UserManagementService.Interceptors;
using UserManagementService.Services;
using UserManagementService.Validation;
using RepoProto = Shared.Protos.UserRepository;

// Enables calling gRPC servers over unencrypted HTTP/2 (h2c), which is how services
// talk to each other inside the docker-compose/Kubernetes network - TLS terminates at the edge.
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureContainerGrpcAndHealthEndpoints();
builder.AddSharedSerilogLogging("UserManagementService");
builder.AddSharedOpenTelemetry("UserManagementService", t => t.AddGrpcClientInstrumentation());

var repositoryServiceAddress = builder.Configuration["GrpcClients:UserRepositoryService"]
    ?? throw new InvalidOperationException("Configuration 'GrpcClients:UserRepositoryService' was not found.");

builder.Services.AddGrpcClient<RepoProto.UserRepositoryGrpcService.UserRepositoryGrpcServiceClient>(options =>
{
    options.Address = new Uri(repositoryServiceAddress);
}).AddHttpMessageHandler(() => new HttpClientXRayTracingHandler());

builder.Services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();

builder.Services.AddGrpc(options => options.Interceptors.Add<ExceptionHandlingInterceptor>());
builder.Services.AddGrpcReflection();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSharedXRayTracing("UserManagementService");
app.UseSerilogRequestLogging();

app.MapGrpcService<UserGrpcService>();
app.MapSharedHealthChecks();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
