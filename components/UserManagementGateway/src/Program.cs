using Microsoft.OpenApi.Models;
using Serilog;
using Shared.Common.Extensions;
using Shared.Common.Validators;
using Shared.Contracts.Requests;
using Shared.Logging.Extensions;
using UserManagementGateway.Services;
using UserManagementGateway.Validation;
using ManagementProto = Shared.Protos.UserManagement;

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

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
