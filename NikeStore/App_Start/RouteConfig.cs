using System.Web.Mvc;
using System.Web.Routing;

namespace NikeStore
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Customer - Trang mặc định
            routes.MapRoute(
                name: "Customer",
                url: "",
                defaults: new
                {
                    controller = "Home",
                    action = "Index"
                },
                namespaces: new[] { "NikeStore.Controllers.Customer" }
            );

            // Customer
            routes.MapRoute(
                name: "CustomerDefault",
                url: "Customer/{controller}/{action}/{id}",
                defaults: new
                {
                    action = "Index",
                    id = UrlParameter.Optional
                },
                namespaces: new[] { "NikeStore.Controllers.Customer" }
            );

            // Employee
            routes.MapRoute(
                name: "Employee",
                url: "Employee/{controller}/{action}/{id}",
                defaults: new
                {
                    action = "Index",
                    id = UrlParameter.Optional
                },
                namespaces: new[] { "NikeStore.Controllers.Employee" }
            );

            // Admin
            routes.MapRoute(
                name: "Admin",
                url: "Admin/{controller}/{action}/{id}",
                defaults: new
                {
                    action = "Index",
                    id = UrlParameter.Optional
                },
                namespaces: new[] { "NikeStore.Controllers.Admin" }
            );
        }
    }
}