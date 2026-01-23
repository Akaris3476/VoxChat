using VoxChat.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = builder.Configuration.GetConnectionString("Redis");
	options.InstanceName = "VoxChat";
});

builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy => 
		policy
			.WithOrigins("http://localhost:5173", "https://localhost:5173")
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials()
		);
});

builder.Services.AddControllers();

builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.


app.UseHttpsRedirection();

app.UseCors();

app.MapHub<ChatHub>("/chat");

// app.MapControllers();

app.Run();