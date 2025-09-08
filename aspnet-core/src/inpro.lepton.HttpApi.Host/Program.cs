﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Grpc.AspNetCore.Web; // для GrpcWebOptions / EnableGrpcWeb
using Serilog;
using Serilog.Events;

namespace inpro.lepton;

public class Program
{
    public async static Task<int> Main(string[] args)
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

            builder.Host
                .AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog();

            // ---------------- gRPC: services ----------------
            builder.Services.AddGrpc();              // сервер gRPC
            builder.Services.AddGrpcReflection();    // опціонально для Dev (grpcurl list)

            // CORS для браузера (gRPC-Web)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("GrpcCors", policy =>
                {
                    policy
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .SetIsOriginAllowed(_ => true); // TODO: у проді вкажи дозволені origin-и
                });
            });
            // --------------- /gRPC: services ----------------

            await builder.AddApplicationAsync<leptonHttpApiHostModule>();

            var app = builder.Build();

            // ---------------- gRPC: middleware & endpoints ----------------
            app.UseCors("GrpcCors");  // CORS має стояти до MapGrpcService

            // Саме ЦЕ (middleware) вмикає підтримку gRPC-Web
            app.UseGrpcWeb(new GrpcWebOptions
            {
                // Можеш увімкнути за замовчуванням (не обов’язково):
                // DefaultEnabled = true
            });

            // Підключаємо наш gRPC-сервіс (+gRPC-Web + CORS)
            app.MapGrpcService<ProductGrpcService>()
               .EnableGrpcWeb()          // дозволити gRPC-Web для цього сервісу
               .RequireCors("GrpcCors");  // дозволити крос-домен
            
            app.MapGrpcService<AccountGrpcService>()
                .EnableGrpcWeb()
                .RequireCors("GrpcCors");
            
            app.MapGrpcService<LoginGrpcService>()
                .EnableGrpcWeb()
                .RequireCors("GrpcCors");

            // Кореневий чек (не обов’язково)
            app.MapGet("/", () => "HTTP/2 gRPC server is running. Try a gRPC client.");

            if (app.Environment.IsDevelopment())
            {
                app.MapGrpcReflectionService();
            }
            // --------------- /gRPC: middleware & endpoints ----------------

            await app.InitializeApplicationAsync();
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
