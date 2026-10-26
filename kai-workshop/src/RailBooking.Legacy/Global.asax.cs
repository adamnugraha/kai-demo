using System.Web;using System.Web.Mvc;using System.Web.Routing;
namespace RailBooking.Legacy {public class MvcApplication:HttpApplication {protected void Application_Start(){RouteTable.Routes.MapRoute("trains","api/trains",new{controller="Rail",action="Trains"});RouteTable.Routes.MapRoute("bookings","api/bookings/{id}",new{controller="Rail",action="Bookings",id=UrlParameter.Optional});}}}
