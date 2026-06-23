using CryptoMarketAnalysis.Cli.Helpers;
using FluentAssertions;

namespace CryptoMarketAnalysis.Cli.Tests.Helpers;

public sealed class DateRangeBatchSplitterTests
{
    [Fact]
    public void Split_ShouldReturnOneBatch_WhenPeriodIsShorterThanBatchSize()
    {
        IReadOnlyCollection<DateRangeBatch> result = DateRangeBatchSplitter.Split(
            new DateTime(2026, 6, 1),
            new DateTime(2026, 6, 10),
            batchDays: 30);

        result.Should().ContainSingle();

        DateRangeBatch batch = result.Single();

        batch.FromUtc.Should().Be(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        batch.ToUtc.Should().Be(new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Split_ShouldReturnMultipleBatches_WhenPeriodIsLongerThanBatchSize()
    {
        IReadOnlyCollection<DateRangeBatch> result = DateRangeBatchSplitter.Split(
            new DateTime(2026, 6, 1),
            new DateTime(2026, 6, 10),
            batchDays: 5);

        result.Should().BeEquivalentTo(
            new[]
            {
                new DateRangeBatch(
                    new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)),
                new DateRangeBatch(
                    new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc)),
            });
    }

    [Fact]
    public void Split_ShouldReturnLastShortBatch_WhenPeriodDoesNotDivideEvenly()
    {
        IReadOnlyCollection<DateRangeBatch> result = DateRangeBatchSplitter.Split(
            new DateTime(2026, 6, 1),
            new DateTime(2026, 6, 12),
            batchDays: 5);

        result.Should().BeEquivalentTo(
            new[]
            {
                new DateRangeBatch(
                    new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)),
                new DateRangeBatch(
                    new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc)),
                new DateRangeBatch(
                    new DateTime(2026, 6, 11, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc)),
            });
    }

    [Fact]
    public void Split_ShouldReturnOneBatch_WhenFromEqualsTo()
    {
        IReadOnlyCollection<DateRangeBatch> result = DateRangeBatchSplitter.Split(
            new DateTime(2026, 6, 1),
            new DateTime(2026, 6, 1),
            batchDays: 30);

        result.Should().ContainSingle();

        DateRangeBatch batch = result.Single();

        batch.FromUtc.Should().Be(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        batch.ToUtc.Should().Be(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Split_ShouldThrow_WhenBatchDaysIsInvalid(
        int batchDays)
    {
        Action action = () => DateRangeBatchSplitter.Split(
            new DateTime(2026, 6, 1),
            new DateTime(2026, 6, 10),
            batchDays);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Split_ShouldThrow_WhenFromIsGreaterThanTo()
    {
        Action action = () => DateRangeBatchSplitter.Split(
            new DateTime(2026, 6, 10),
            new DateTime(2026, 6, 1),
            batchDays: 5);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Split_ShouldNormalizeDateTimeToUtcDate()
    {
        IReadOnlyCollection<DateRangeBatch> result = DateRangeBatchSplitter.Split(
            new DateTime(2026, 6, 1, 12, 30, 0),
            new DateTime(2026, 6, 2, 18, 45, 0),
            batchDays: 1);

        result.Should().BeEquivalentTo(
            new[]
            {
                new DateRangeBatch(
                    new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)),
                new DateRangeBatch(
                    new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc)),
            });
    }
}