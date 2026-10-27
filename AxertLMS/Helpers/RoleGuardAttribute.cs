using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System;

namespace AxertLMS.Helpers
{
    public class RoleGuardAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public RoleGuardAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;
            var userRole = session.GetString("UserRole");

            if (string.IsNullOrEmpty(userRole))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            bool isAuthorized = false;
            foreach (var role in _allowedRoles)
            {
                if (userRole == role)
                {
                    isAuthorized = true;
                    break; // Secured inside the matching bracket
                }
            }

            if (!isAuthorized)
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { error = "Access Denied." });
            }
        }
    }
}