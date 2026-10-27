using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using OE.Shared.Data.Sportsbook;
using System.Collections.Generic;
using Xunit;

namespace Backoffice.API.Tests.ClientImport
{
    public class ClientImportEnvironmentTests
    {
        private static ClientImportRepository BuildSut(string backofficeConnectionString)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["ConnectionStrings:Backoffice"] = backofficeConnectionString })
                .Build();

            return new ClientImportRepository(
                new BackofficeContext(new DbContextOptionsBuilder<BackofficeContext>().UseInMemoryDatabase(nameof(ClientImportEnvironmentTests)).Options),
                new SportsbookContext(new DbContextOptionsBuilder<SportsbookContext>().UseInMemoryDatabase(nameof(ClientImportEnvironmentTests)).Options),
                new Mock<IBackofficeRepository>().Object,
                new Mock<IBackofficeActivityLogRepository>().Object,
                configuration,
                new Mock<IRabbitMqRepository>().Object);
        }

        [Theory]
        [InlineData("Data Source=stg-song88-mssql-server.cz8wmg0s28pd.ap-south-1.rds.amazonaws.com, 1433;Database=Backoffice;User Id=u;Password=p", "Staging")]
        [InlineData("Data Source=prd-song88-mssql-server.cdqkiuumgexn.ap-east-1.rds.amazonaws.com, 1433;Database=Backoffice;User Id=u;Password=p", "Production")]
        // Dev pods reach the in-cluster SQL Server by its Kubernetes service name...
        [InlineData("Data Source=mssql-mssqlserver-2022,1433;Database=Backoffice;User Id=SA;Password=p", "Dev")]
        [InlineData("Server=mssql-mssqlserver-2022.dev.svc.cluster.local;Database=Backoffice;User Id=SA;Password=p", "Dev")]
        // ...and outside clients (e.g. a local run) by its load balancer. Same database.
        [InlineData("Data Source=k8s-dev-database-57db2f90d4-4ab1951adb5d3794.elb.ap-south-1.amazonaws.com,1433;Database=Backoffice;User Id=SA;Password=p", "Dev")]
        public void KnownHosts_MapToTheirEnvironment(string connectionString, string expected)
        {
            var result = BuildSut(connectionString).GetEnvironment();

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(expected, result.Result);
        }

        [Fact]
        public void UnknownHost_IsBlockedAndNamesTheHostButNotThePassword()
        {
            var result = BuildSut("Data Source=10.1.2.3,1433;Database=Backoffice;User Id=SA;Password=SuperSecret123").GetEnvironment();

            Assert.Equal(StatusCode.Error, result.StatusCode);
            Assert.Contains("\"10.1.2.3\"", result.Message);
            Assert.DoesNotContain("SuperSecret123", result.Message);
            Assert.DoesNotContain("SA", result.Message);
        }
    }
}
