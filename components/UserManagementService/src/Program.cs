using Serilog;
using Shared.Common.Validators;
using Shared.Logging.Extensions;
using Shared.Protos.UserManagement;
using UserManagementService.Interceptors;
using UserManagementService.Services;
using UserManagementService.Validation;
using RepoProto = Shared.Protos.UserRepository;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedSerilogLogging("UserManagementService");

var repositoryServiceAddress = builder.Configuration["GrpcClients:UserRepositoryService"]
    ?? throw new InvalidOperationException("Configuration 'GrpcClients:UserRepositoryService' was not found.");

builder.Services.AddGrpcClient<RepoProto.UserRepositoryGrpcService.UserRepositoryGrpcServiceClient>(options =>
{
    options.Address = new Uri(repositoryServiceAddress);
});

builder.Services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();

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
