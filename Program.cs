using BookLibrary.Configuration;
using BookLibrary.Data;
using BookLibrary.Services;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IDatabase, Database>();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });


            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.Configure<ExternalApisOptions>(
                builder.Configuration.GetSection(ExternalApisOptions.SectionName));

            builder.Services.Configure<LocalAiOptions>(
                builder.Configuration.GetSection(LocalAiOptions.SectionName));

            builder.Services.AddHttpClient<GeminiAiService>(client =>
            {
                client.BaseAddress = new Uri(GeminiAiService.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(120);
            });

            builder.Services.AddHttpClient<LocalAiService>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<LocalAiOptions>>().Value;
                var baseUrl = options.BaseUrl?.Trim();
                if (!string.IsNullOrEmpty(baseUrl) && Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var uri))
                    client.BaseAddress = uri;
                client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
            });

            builder.Services.AddKeyedSingleton(ResilientBookAiService.LocalAiCircuit, (sp, _) => new CircuitBreaker(
                ResilientBookAiService.LocalAiCircuit,
                sp.GetRequiredService<IOptions<LocalAiOptions>>().Value.CircuitBreaker,
                TimeProvider.System,
                sp.GetRequiredService<ILogger<CircuitBreaker>>()));

            builder.Services.AddScoped<IBookAiService, ResilientBookAiService>();

            builder.Services.AddMemoryCache();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated();

                DbInitializer.Initialize(dbContext);
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");


            app.Run();
        }
    }
}
