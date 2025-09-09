using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

using Inpro.Lepton.Grpc;            // де лежить AbpHttpProxyService (з option csharp_namespace у .proto)
using Grpc.AspNetCore.Web;          // для UseGrpcWeb

namespace inpro.lepton;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            .CreateLogger();

        try
        {
            Log.Information("Starting inpro.lepton.HttpApi.Host.");

            var builder = WebApplication.CreateBuilder(args);

            // ---- ABP host config ----
            builder.Host
                .AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog();

            // ---- gRPC / gRPC-Web ----
            builder.Services.AddGrpc();                // сервер gRPC
            builder.Services.AddHttpClient();          // для self-call усередині проксі
            builder.Services.AddHttpContextAccessor(); // щоб визначати базову адресу

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("GrpcCors", policy =>
                {
                    policy
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .WithOrigins(
                            "http://localhost:4200",
                            "https://your-frontend-domain"
                        );
                });
            });

            // Піднімаємо ABP-модуль хоста
            await builder.AddApplicationAsync<leptonHttpApiHostModule>();
            builder.Services
                .AddHttpClient("AbpSelf", client => {
                    client.BaseAddress = new Uri("http://localhost:5000");
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { 
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator 
                });
            
            builder.Services.AddGrpc();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();


            // Увімкнути CORS для gRPC-Web
            app.UseCors("GrpcCors");

            // gRPC-Web (дає можливість викликів з браузера через HTTP/1.1)
            app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

            // ВАЖЛИВО: ініціалізація всього ABP-пайплайна
            await app.InitializeApplicationAsync();

            // Маршрут gRPC-сервісу (після ініціалізації, але до Run)
            app.MapGrpcService<AbpHttpProxyService>()
               .EnableGrpcWeb()
               .RequireCors("GrpcCors");

            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException) throw;

            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
