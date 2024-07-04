var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins("http://localhost:3000") // Укажите источник вашего фронтенда
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
        });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging(); // Добавляем логирование
builder.Services.AddSignalR(); // Добавляем SignalR

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles(); // Добавлено для обслуживания статических файлов

app.UseCors("AllowSpecificOrigin");

app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chathub"); // Маршрут для SignalR хаба

app.Run();

public partial class Program { }