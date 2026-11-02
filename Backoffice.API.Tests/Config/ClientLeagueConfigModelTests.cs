using Backoffice.Shared.Data.Trading;
using Backoffice.Shared.Data.Trading.SQLEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using Xunit;

namespace Backoffice.API.Tests.Config
{
    // BO-151: Trading.dbo.ClientLeagueConfig.IsActive is a smallint column (0 or 1) but the entity exposes a bool. The
    // in-memory provider used by the other tests never checks column types, so these check the model itself. Without the
    // conversion, reading any saved row from SQL Server fails with "Unable to cast object of type 'System.Int16' to type
    // 'System.Boolean'" (it showed on DEV as the Assigned and Unassigned lists and counts failing after the first assign).
    public class ClientLeagueConfigModelTests
    {
        private static IProperty IsActiveProperty()
        {
            using var context = new TradingContext(new DbContextOptionsBuilder<TradingContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

            return context.Model.FindEntityType(typeof(ClientLeagueConfig)).FindProperty(nameof(ClientLeagueConfig.IsActive));
        }

        [Fact]
        public void IsActive_IsStoredAsAShort_LikeTheSmallintColumn()
        {
            Assert.Equal(typeof(short), IsActiveProperty().GetProviderClrType());
        }

        [Theory]
        [InlineData(true, (short)1)]
        [InlineData(false, (short)0)]
        public void IsActive_IsSavedAsOneOrZero(bool isActive, short stored)
        {
            var saved = IsActiveProperty().GetTypeMapping().Converter.ConvertToProvider(isActive);

            Assert.Equal(stored, Assert.IsType<short>(saved));
        }

        [Theory]
        [InlineData((short)1, true)]
        [InlineData((short)0, false)]
        public void IsActive_IsReadFromOneOrZero(short stored, bool isActive)
        {
            var read = IsActiveProperty().GetTypeMapping().Converter.ConvertFromProvider(stored);

            Assert.Equal(isActive, Assert.IsType<bool>(read));
        }
    }
}
