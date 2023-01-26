using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.OutgoingEngineMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;

namespace Backoffice.Utility.Repository
{
    public class BackofficeRepository : IBackofficeRepository
    {
        private readonly IConfiguration _configuration;
        public BackofficeRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task ProcessActivityLog(OnActivityLogMessage message)
        {
            try
            {
                Console.WriteLine(message.Activity);
                var optionsBuilder = new DbContextOptionsBuilder<BackofficeContext>().UseSqlServer(_configuration["ConnectionStrings:Backoffice"]);

                using (var context = new BackofficeContext(optionsBuilder.Options))
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        var timeStamp = message.TimeStamp;
                        if (message.TimeStamp == DateTime.MinValue)
                        {
                            timeStamp = DateTime.UtcNow;
                        }

                        // insert new activity logs record
                        var activityLog = new ActivityLog
                        {
                            Page = message.Page,
                            Payload = message.Payload,
                            From = message.From,
                            To = message.To,
                            Activity = message.Activity,
                            UserId = message.UserId,
                            UserName = message.UserName,
                            TimeStamp = timeStamp
                        };

                        context.ActivityLog.Add(activityLog);

                        await context.SaveChangesAsync();

                        scope.Complete();

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public async Task ProcessActivityLog(OnActivityLogListMessage message)
        {
            try
            {
                var activityLogs = new List<ActivityLog>();

                foreach (var item in message.OnActivityLogMessages)
                {
                    Console.WriteLine(item.Activity);
                    var timeStamp = item.TimeStamp;
                    if (item.TimeStamp == DateTime.MinValue)
                    {
                        timeStamp = DateTime.UtcNow;
                    }
                    activityLogs.Add(new ActivityLog()
                    {
                        Page = item.Page,
                        Payload = item.Payload,
                        From = item.From,
                        To = item.To,
                        Activity = item.Activity,
                        UserId = item.UserId,
                        UserName = item.UserName,
                        TimeStamp = timeStamp
                    });
                };

                var optionsBuilder = new DbContextOptionsBuilder<BackofficeContext>().UseSqlServer(_configuration["ConnectionStrings:Backoffice"]);

                using (var context = new BackofficeContext(optionsBuilder.Options))
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        context.ActivityLog.AddRange(activityLogs);
                        await context.SaveChangesAsync();

                        scope.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}