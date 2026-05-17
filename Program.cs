using DotNetEnv;
using FinChain.Function;
using FinChain.Repository;
using Scalar;
using Scalar.AspNetCore;
using Supabase;
namespace FinChain
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Env.Load();

            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddEnvironmentVariables();

            builder.Services.AddScoped<IChatProcessor, ChatProcessor>();
            builder.Services.AddScoped<IConfigurationProcessor, ConfigurationProcessor>();
            builder.Services.AddScoped<ILogMessageRepository, LogMessageRepository>();
            builder.Services.AddScoped<ITopicMessageRepository, TopicMessageRepository>();
            builder.Services.AddScoped<IAiConfigRepository, AiConfigRepository>();
            builder.Services.AddScoped<IEmbeddingConfigRepository, EmbeddingConfigRepository>();
            builder.Services.AddScoped<IModelTemplateRepository, ModelTemplateRepository>();
            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var url = builder.Configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url is not configured");
            var key = builder.Configuration["Supabase:Key"] ?? throw new InvalidOperationException("Supabase:Key is not configured");
            var options = new SupabaseOptions
            {
                AutoConnectRealtime = true,
            };
            var supabase = new Client(url, key, options);
            await supabase.InitializeAsync();
            builder.Services.AddSingleton(supabase);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Lifetime.ApplicationStarted.Register(() =>
            {
                var urls = app.Urls.FirstOrDefault() ?? "http://localhost:5221";
                var scalarUrl = $"{urls}/scalar/v1";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = scalarUrl,
                    UseShellExecute = true
                });
            });

            app.Run();
        }
    }
}
