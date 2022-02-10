using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ADAPH.TxSubmit.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Blockfrost.Api.Extensions;
using ADAPH.TxSubmit.Workers;
using MudBlazor.Services;
using Blazored.LocalStorage;
using ADAPH.TxSubmit.Services;
using Blockfrost.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlockfrost(builder.Configuration["CardanoNetwork"], builder.Configuration["BlockfrostProjectId"]);
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddApiVersioning(config =>
{
    config.DefaultApiVersion = new ApiVersion(1, 0);
    config.AssumeDefaultVersionWhenUnspecified = true;
    config.ReportApiVersions = true;
});
builder.Services.AddHttpClient();
builder.Services.AddDbContext<TxSubmitDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("TxSubmitDb"));
});
builder.Services.AddSingleton<TransactionService>();
builder.Services.AddHostedService<TransactionWorker>();
builder.Services.AddHostedService<DbWorker>();
builder.Services.AddScoped<TimeZoneService>();
builder.Services.AddSingleton<GlobalStateService>();

builder.Services.AddCors(option =>
{
    option.AddPolicy("AllowCors", builder =>
    {
        builder
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()
        .SetIsOriginAllowedToAllowWildcardSubdomains();
    });
});

builder.Services.AddHttpClient("tx-inspector", c =>
{
    c.BaseAddress = new Uri($"https://tx.adaph.io/");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowCors");
app.UseEndpoints(endpoints => endpoints.MapControllers());

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
