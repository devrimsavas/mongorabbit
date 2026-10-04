
using MongoDB.Driver;
using MongoRabbitA.Models;
using MongoRabbitA.Services;
using Microsoft.OpenApi.Models;




var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<DbSeederHostedService>();

//CORS 
builder.Services.AddCors(options=>
{
    options.AddPolicy("AllowAll",policy=>
    {
        policy.AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin();
    });
});

builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddSingleton<MongoOrderService>();

//RabbitMQ 
builder.Services.AddSingleton<RabbitMQPublisher>();


//controllers added 
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options=>

{
    options.SwaggerDoc("v1",new OpenApiInfo {Title="Mongo Rabbit Stock",Version="v1"});

}

);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();





app.Run();


