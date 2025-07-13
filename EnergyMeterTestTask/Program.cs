// Program.cs
using EnergyMeterTestTask.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<IFieldService, FieldService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fields API",
        Version = "v1",
        Description = "API for working with agricultural fields and geospatial data",
        //License = new OpenApiLicense { Name = "Free License", Url = new Uri($"{builder.Configuration["ASPNETCORE_URLS"].Split(";")[1]}/swagger") },
        Contact = new OpenApiContact { Name = "Evgeniy Nikonov", Email = "johnybravo89@mail.ru" }
    });

    // Include XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EnergyMeterTestTask");
        c.InjectStylesheet("/swagger-ui/SwaggerDark.css");
    });
    app.MapGet("/swagger-ui/SwaggerDark.css", async (CancellationToken cancellationToken) =>
    {
        var css = await File.ReadAllBytesAsync("SwaggerDark.css", cancellationToken);
        return Results.File(css, "text/css");
    }).ExcludeFromDescription();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Exception handling
app.UseExceptionHandler(a => a.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    await context.Response.WriteAsJsonAsync(new { error = exception?.Message });
}));

app.Run();