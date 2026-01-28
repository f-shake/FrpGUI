using FrpGUI.Configs;
using FrpGUI.Models;
using FrpGUI.Services;
using FrpGUI.WebAPI.Services;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.OpenApi;

namespace FrpGUI.WebAPI;

internal class Program
{
    private static bool swagger = true;

    private static WebApplication app;
    private static string cors = "cors";

    private static void Main(string[] args)
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            nameof(FrpGUI), "logs");
        Console.WriteLine($"日志保存位置：{dir}");

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console() // 控制台输出
            .WriteTo.File(Path.Combine(dir, "log.txt"), rollingInterval: RollingInterval.Day) // 按天滚动存储日志
            .CreateLogger();

        WebApplicationBuilder builder = CreateBuilder(args);

        app = builder.Build();

        SettingApp(app);

        app.Run();
    }

    private static WebApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
        {
            Args = args,
            ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default,
        });

        // Add services to the container.

        builder.Services.AddControllers(o =>
            {
                //不开这个，传入的参数有null（比如token）就会400 Bad Request
                o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                o.Filters.Add(app.Services.GetRequiredService<FrpGUIActionFilter>());
            })
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
                o.JsonSerializerOptions.Converters.Add(new FrpConfigJsonConverter());
            });

        builder.Services.AddTransient<FrpGUIActionFilter>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(p =>
        {
            var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var xmlPath = Path.Combine(basePath, "FrpGUI.WebAPI.xml");
            p.IncludeXmlComments(xmlPath);

            p.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme."
            });

            p.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearer", document)] = []
            });
        });


        builder.Services.AddSingleton<LoggerBase, Logger>();
        builder.Services.AddSingleton<FrpProcessCollection>();
        builder.Services.AddTransient<WebConfigService>();
        builder.Services.AddHostedService<WebAppLifetimeService>();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: cors,
                policy =>
                {
                    policy.AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowAnyOrigin();
                });
        });

        builder.Host.UseWindowsService(c => { c.ServiceName = "FrpGUI"; });
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        AppConfig config = AppConfig.Get();
        builder.Services.AddSingleton(config);

        builder.Services.AddSingleton<IEnvironmentConfig, WebEnvirementConfig>();

        return builder;
    }

    private static void SettingApp(WebApplication app)
    {
        app.Services.GetRequiredService<LoggerBase>().Info("服务启动");
        if (swagger || app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        //app.UseWebSockets();
        app.UseHttpsRedirection();
        app.UseCors(cors);
        //app.UseAuthorization();
        app.MapControllers();
    }
}