namespace Techies.Web.Domain;

// Stored as UPPER_SNAKE strings (ACTIVE, SUPER_ADMIN, ...) to match the BRD values.
public enum AccountStatus { Active, Locked }

public enum StaffRole { SuperAdmin, CatalogStaff, OrderStaff }
