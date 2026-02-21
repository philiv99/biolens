using biolens.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Application services
builder.Services.AddSingleton<IStorageService, FlatFileStorageService>();
builder.Services.AddTransient<IDocumentParserService, DocumentParserService>();
builder.Services.AddTransient<IExtractionService, AiExtractionService>();

// CORS — environment-specific origins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy
                .WithOrigins(
                    "http://localhost:5173",
                    "http://localhost:5174",
                    "http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? Array.Empty<string>();
            policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// ---------- Middleware ----------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

app.Run();
