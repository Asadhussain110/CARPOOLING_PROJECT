using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using CARPOOLING_PROJECT.Models;
using CARPOOLING_PROJECT.ViewModels;

namespace CARPOOLING_PROJECT.Controllers
{
    public class AdminController : Controller
    {
        private readonly CarpoolMGSEntities1 _db = new CarpoolMGSEntities1();

        public ActionResult Dashboard()
        {
            var redirect = EnsureAdminAccess();
            if (redirect != null)
            {
                return redirect;
            }

            var model = new AdminDashboardViewModel
            {
                Drivers = _db.Drivers.Include(d => d.User)
                    .OrderByDescending(d => d.CreatedAt)
                    .ToList()
                    .Select(d => new DriverListItem { Driver = d, User = d.User })
                    .ToList(),
                Passengers = _db.Passengers.Include(p => p.User)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToList()
                    .Select(p => new PassengerListItem { Passenger = p, User = p.User })
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult ApproveDriver(Guid id)
        {
            var redirect = EnsureAdminAccess();
            if (redirect != null)
            {
                return redirect;
            }

            var driver = _db.Drivers.Include(d => d.User).FirstOrDefault(d => d.DriverID == id);
            if (driver == null)
            {
                TempData["Error"] = "Driver not found.";
                return RedirectToAction("Dashboard");
            }

            driver.IsApproved = true;
            _db.SaveChanges();
            TempData["Message"] = $"Driver {driver.User?.Name} approved.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public ActionResult RejectDriver(Guid id)
        {
            var redirect = EnsureAdminAccess();
            if (redirect != null)
            {
                return redirect;
            }

            var driver = _db.Drivers.Include(d => d.User).FirstOrDefault(d => d.DriverID == id);
            if (driver == null)
            {
                TempData["Error"] = "Driver not found.";
                return RedirectToAction("Dashboard");
            }

            driver.IsApproved = false;
            _db.SaveChanges();
            TempData["Message"] = $"Driver {driver.User?.Name} rejected.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public ActionResult ToggleDriverBan(Guid id)
        {
            var redirect = EnsureAdminAccess();
            if (redirect != null)
            {
                return redirect;
            }

            var driver = _db.Drivers.Include(d => d.User).FirstOrDefault(d => d.DriverID == id);
            if (driver == null || driver.User == null)
            {
                TempData["Error"] = "Driver not found.";
                return RedirectToAction("Dashboard");
            }

            driver.User.IsActive = !(driver.User.IsActive ?? true);
            _db.SaveChanges();
            TempData["Message"] = $"Driver {driver.User.Name} {(driver.User.IsActive == true ? "unbanned" : "banned")}.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public ActionResult TogglePassengerBan(Guid id)
        {
            var redirect = EnsureAdminAccess();
            if (redirect != null)
            {
                return redirect;
            }

            var passenger = _db.Passengers.Include(p => p.User).FirstOrDefault(p => p.PassengerID == id);
            if (passenger == null || passenger.User == null)
            {
                TempData["Error"] = "Passenger not found.";
                return RedirectToAction("Dashboard");
            }

            passenger.User.IsActive = !(passenger.User.IsActive ?? true);
            _db.SaveChanges();
            TempData["Message"] = $"Passenger {passenger.User.Name} {(passenger.User.IsActive == true ? "unbanned" : "banned")}.";
            return RedirectToAction("Dashboard");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }

        private ActionResult EnsureAdminAccess()
        {
            var role = Session["Role"] as string;
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            return null;
        }
    }
}

