namespace WasteAccountingClient.Services;

/// <summary>
/// Вкладки показываются только если роль имеет доступ к API на сервере (проверено по RBAC).
/// </summary>
public static class RolePermissions
{
    public static string NormalizeRole(string? role) =>
        string.IsNullOrWhiteSpace(role) ? "" : role.Trim().ToLowerInvariant();

    /// <summary>GET /accounting/batches</summary>
    public static bool CanViewHistory(string role) =>
        role is "operator" or "chief" or "ecologist" or "admin";

    /// <summary>POST /accounting/batches (оператор)</summary>
    public static bool CanRegisterBatch(string role) => role == "operator";

    /// <summary>GET /accounting/operations + журнал на вкладке</summary>
    public static bool CanViewOperationsTab(string role) =>
        role is "chief" or "ecologist" or "admin";

    /// <summary>GET /reporting/dashboard и отчёты</summary>
    public static bool CanViewReporting(string role) =>
        role is "chief" or "ecologist" or "admin";

    /// <summary>Очередь accepted; GET /accounting/batches?status=accepted</summary>
    public static bool CanViewControlQueue(string role) =>
        role is "chief" or "ecologist" or "admin";

    /// <summary>PATCH classify / reject</summary>
    public static bool CanClassifyOrReject(string role) =>
        role is "ecologist" or "admin";

    /// <summary>GET /core/waste-types</summary>
    public static bool CanViewFkko(string role) =>
        role is "chief" or "ecologist" or "admin";
}
