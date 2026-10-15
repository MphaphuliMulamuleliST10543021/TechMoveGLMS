using TechMoveGLMS.Web.Models;

namespace TechMoveGLMS.Web.Services;

public static class ContractRules
{
    public static bool HasValidDates(Contract contract) =>
        contract.EndDate.Date >= contract.StartDate.Date;

    public static bool CanCreateServiceRequest(
        Contract contract,
        DateTime currentDate)
    {
        return string.Equals(
                   contract.Status,
                   "Active",
                   StringComparison.OrdinalIgnoreCase)
               && contract.StartDate.Date <= currentDate.Date
               && contract.EndDate.Date >= currentDate.Date;
    }
}