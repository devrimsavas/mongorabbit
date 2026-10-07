
using MongoDB.Driver;
using MongoRabbitA.Models;
using MongoRabbitA.Services;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;




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
//Rabbit Consumer 
builder.Services.AddHostedService<OrderProcessingConsumer>();


//controllers added 

//very important note Controllers options allows us to see status instead of status no 
// useful 
builder.Services.AddControllers()
.AddJsonOptions(options=>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
}
);

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
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();





app.Run();


