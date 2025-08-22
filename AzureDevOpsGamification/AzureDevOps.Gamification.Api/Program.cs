using AzureDevOps.Gamification.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .Select(x => new
                {
                    Field = x.Key,
                    Errors = x.Value!.Errors.Select(e => e.ErrorMessage)
                });

            return new BadRequestObjectResult(new { Message = "Dados inválidos", Errors = errors });
        };
    });

// HttpClient + Cache
builder.Services.AddHttpClient<IAzureDevOpsService, AzureDevOpsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.Add("User-Agent", "AzureDevOps-Gamification/1.0");
});
builder.Services.AddMemoryCache(opt => opt.SizeLimit = 100);

// CORS
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Azure DevOps Gamification API",
        Version = "v1",
        Description = """
            API para métricas e gamificação do Azure DevOps.

            *Como usar:*
            1. Obtenha um PAT (Personal Access Token) do Azure DevOps com permissões de Code, Build e Project.
            2. Use o PAT no parâmetro token ou no header X-Azure-DevOps-Token.
            3. Teste com o endpoint /api/gamification/test/connection.
            """,
        Contact = new OpenApiContact
        {
            Name = "Azure DevOps Gamification",
            Url = new Uri("https://github.com/your-repo")
        }
    });

    // Segurança
    c.AddSecurityDefinition("AzureDevOpsToken", new OpenApiSecurityScheme
    {
        Name = "X-Azure-DevOps-Token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Personal Access Token do Azure DevOps"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        Description = "Token do Azure DevOps no formato Bearer"
    });
});

// HealthChecks
builder.Services.AddHealthChecks();

// Serviços customizados
builder.Services.AddScoped<IAzureDevOpsService, AzureDevOpsService>();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VelarIQ API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.DefaultModelExpandDepth(2);
        c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
        c.EnableValidator();
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();

// HealthCheck
app.MapHealthChecks("/health");

// Controllers
app.MapControllers();

// Redirect raiz -> Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();