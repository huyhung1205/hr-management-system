using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace human_resource_management.Filters
{
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        public string[] AllowedRoles { get; set; }

        public RoleAuthorizeAttribute(params string[] roles)
        {
            AllowedRoles = roles;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (!httpContext.Request.IsAuthenticated)
            {
                return false;
            }

            // Get role from session and trim whitespace
            var userRole = httpContext.Session["UserRole"]?.ToString()?.Trim();

            if (string.IsNullOrEmpty(userRole))
            {
                return false;
            }

            // Check if user's role is in the allowed roles list (case-insensitive comparison)
            return AllowedRoles.Any(role => role.Trim().Equals(userRole, System.StringComparison.OrdinalIgnoreCase));
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (!filterContext.HttpContext.Request.IsAuthenticated)
            {
                // User is not authenticated, redirect to login
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Account", action = "Login", area = "" })
                );
            }
            else
            {
                // User is authenticated but doesn't have the right role
                // Redirect to access denied or their appropriate area
                var userRole = filterContext.HttpContext.Session["UserRole"]?.ToString()?.Trim();

                switch (userRole)
                {
                    case "Admin":
                        filterContext.Result = new RedirectToRouteResult(
                            new RouteValueDictionary(new { controller = "Home", action = "Index", area = "Admin" })
                        );
                        break;
                    case "Nhân sự":
                        filterContext.Result = new RedirectToRouteResult(
                            new RouteValueDictionary(new { controller = "Home", action = "Index", area = "HumanResource" })
                        );
                        break;
                    case "Nhân viên":
                        filterContext.Result = new RedirectToRouteResult(
                            new RouteValueDictionary(new { controller = "Home", action = "Index", area = "Employee" })
                        );
                        break;
                    default:
                        filterContext.Result = new RedirectToRouteResult(
                            new RouteValueDictionary(new { controller = "Account", action = "Login", area = "" })
                        );
                        break;
                }
            }
        }
    }
}
