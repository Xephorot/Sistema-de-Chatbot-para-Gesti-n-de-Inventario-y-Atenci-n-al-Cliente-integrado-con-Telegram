using chatbot_api.Services;
using chatbot_api.Configurations;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ===================
// Configuración de MongoDB
// ===================
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<MongoService>();
builder.Services.AddSingleton<TelegramService>();

// ===================
// Configuración de HttpClient para OpenAI
// ===================
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["OpenAI:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {builder.Configuration["OpenAI:ApiKey"]}");
});

/// =====================
/// Configuracion CompanyAPI
/// =====================
builder.Services.AddHttpClient("CompanyAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CompanyApi:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});


// ===================
// CORS: Permitir cualquier origen
// ===================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
