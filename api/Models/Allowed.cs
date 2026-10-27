namespace Testify.Api.Models;

// The values the brief allows for each status-like field. Every write checks the incoming value against these lists.
public static class Allowed
{
    public static readonly string[] Priorities = ["Low", "Medium", "High", "Critical"];
    public static readonly string[] Severities = ["Low", "Medium", "High", "Critical"];
    public static readonly string[] RunStatuses = ["In Progress", "Complete"];
    public static readonly string[] DefectStatuses = ["Open", "In Progress", "Resolved", "Closed"];
}
