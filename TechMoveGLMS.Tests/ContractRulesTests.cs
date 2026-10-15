using TechMoveGLMS.Web.Models;
using TechMoveGLMS.Web.Services;

namespace TechMoveGLMS.Tests;

public class ContractRulesTests
{
    [Fact]
    public void HasValidDates_WhenEndDateIsAfterStartDate_ReturnsTrue()
    {
        var contract = new Contract
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };

        Assert.True(ContractRules.HasValidDates(contract));
    }

    [Fact]
    public void HasValidDates_WhenEndDateIsBeforeStartDate_ReturnsFalse()
    {
        var contract = new Contract
        {
            StartDate = new DateTime(2026, 12, 31),
            EndDate = new DateTime(2026, 1, 1)
        };

        Assert.False(ContractRules.HasValidDates(contract));
    }

    [Fact]
    public void CanCreateServiceRequest_WhenContractIsActiveAndCurrent_ReturnsTrue()
    {
        var today = new DateTime(2026, 6, 15);

        var contract = new Contract
        {
            Status = "Active",
            StartDate = today.AddMonths(-1),
            EndDate = today.AddMonths(1)
        };

        Assert.True(ContractRules.CanCreateServiceRequest(contract, today));
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Expired")]
    [InlineData("On Hold")]
    public void CanCreateServiceRequest_WhenContractIsNotActive_ReturnsFalse(
        string status)
    {
        var today = new DateTime(2026, 6, 15);

        var contract = new Contract
        {
            Status = status,
            StartDate = today.AddMonths(-1),
            EndDate = today.AddMonths(1)
        };

        Assert.False(ContractRules.CanCreateServiceRequest(contract, today));
    }

    [Fact]
    public void CanCreateServiceRequest_WhenContractStartsInFuture_ReturnsFalse()
    {
        var today = new DateTime(2026, 6, 15);

        var contract = new Contract
        {
            Status = "Active",
            StartDate = today.AddDays(1),
            EndDate = today.AddMonths(1)
        };

        Assert.False(ContractRules.CanCreateServiceRequest(contract, today));
    }

    [Fact]
    public void CanCreateServiceRequest_WhenContractHasExpired_ReturnsFalse()
    {
        var today = new DateTime(2026, 6, 15);

        var contract = new Contract
        {
            Status = "Active",
            StartDate = today.AddMonths(-2),
            EndDate = today.AddDays(-1)
        };

        Assert.False(ContractRules.CanCreateServiceRequest(contract, today));
    }
}