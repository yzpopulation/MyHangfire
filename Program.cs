using Hangfire;
using Hangfire.LiteDB;
using Hangfire.Server;

using Microsoft.Extensions.Hosting;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddAuthorization();
            builder.Services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                 .UseSimpleAssemblyNameTypeSerializer()
                    .UseDefaultTypeSerializer()
                    .UseLiteDbStorage("./hf0330.db");
            });
            builder.Services.AddHangfireServer();
            builder.Services.AddTransient<Job>();
            var app = builder.Build();
            app.UseHangfireDashboard();
            app.MapGet("/schedule", (IBackgroundJobClient backgroundJobs) => backgroundJobs.Enqueue<Job>(_ => _.Execute(null!, CancellationToken.None)));
            app.MapGet("/cancel/{jobId}", (string jobId, IBackgroundJobClient backgroundJobs) => backgroundJobs.Delete(jobId));
            var recurringJobManager = app.Services.GetService<IRecurringJobManager>();
            recurringJobManager.AddOrUpdate(
           "run every minutes",
           () => Console.WriteLine("hello recurring job!"),
           "* * * * *"
       );
            app.Run();
        }
    }
    public class Job
    {
        private readonly ILogger<Job> _logger;

        public Job(ILogger<Job> logger)
        {
            _logger = logger;
        }

        public async Task Execute(PerformContext context, CancellationToken cancellationToken)
        {
            try
            {
                for (var i = 0; i < 15; i++)
                {
                    _logger.LogInformation("Running job {JobId} for {Seconds}s", context.BackgroundJob.Id, i);
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
            catch (TaskCanceledException e)
            {
                _logger.LogWarning(e, "job was cancelled");
            }
        }
    }
}