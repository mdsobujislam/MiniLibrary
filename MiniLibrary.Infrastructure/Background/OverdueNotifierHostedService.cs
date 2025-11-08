using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniLibrary.Application.Interfaces;
using MiniLibrary.Domain.Entities;
using MiniLibrary.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniLibrary.Infrastructure.Background
{
    public class OverdueNotifierHostedService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<OverdueNotifierHostedService> _logger;
        private readonly int _intervalSeconds;

        public OverdueNotifierHostedService(IServiceProvider sp, IConfiguration cfg, ILogger<OverdueNotifierHostedService> logger)
        {
            _sp = sp; 
            _logger = logger;
            _intervalSeconds = cfg.GetSection("BackgroundJob").GetValue<int>("CheckIntervalSeconds", 60);
            //_intervalSeconds = cfg.GetValue<int>("BackgroundJob:CheckIntervalSeconds", 60);
            //_intervalSeconds = int.TryParse(cfg["BackgroundJob:CheckIntervalSeconds"], out var interval) ? interval : 60;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OverdueNotifierHostedService started");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<ILibraryRepository>();
                    using var conn = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>().CreateConnection();
                    // find borrows where DueDate < now AND ReturnDate IS NULL AND OverdueNotified = 0
                    var overdueSql = "SELECT * FROM Borrows WHERE DueDate < @Now AND ReturnDate IS NULL AND OverdueNotified = 0";
                    var borrows = (await conn.QueryAsync<Borrow>(overdueSql, new { Now = DateTime.UtcNow })).ToList();
                    if (borrows.Any())
                    {
                        foreach (var b in borrows)
                        {
                            var member = await conn.QueryFirstOrDefaultAsync<Member>("SELECT * FROM Members WHERE MemberId=@Id", new { Id = b.MemberId });
                            if (member == null) continue;
                            // create email log
                            var subject = "Library overdue notice";
                            var body = $"Dear {member.FullName}, your borrow (id {b.BorrowId}) was due on {b.DueDate:yyyy-MM-dd}, please return.";
                            await conn.ExecuteAsync("INSERT INTO EmailLogs (MemberId,Email,Subject,Body,SentAt) VALUES (@MemberId,@Email,@Subject,@Body,@SentAt)",
                                new { MemberId = member.MemberId, Email = member.Email, Subject = subject, Body = body, SentAt = DateTime.UtcNow });

                            // mark OverdueNotified
                            await conn.ExecuteAsync("UPDATE Borrows SET OverdueNotified = 1 WHERE BorrowId = @BorrowId", new { BorrowId = b.BorrowId });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in overdue notifier");
                }

                await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
            }
        }
    }
}
