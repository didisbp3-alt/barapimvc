MVCAPIFriedBananas\Controllers\MenuAdminController.cs
[Authorize(Policy = "BarOrAdmin")]
public class MenuAdminController : Controller { ... }