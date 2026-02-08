using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using CARPOOLING_PROJECT.Helpers;
using CARPOOLING_PROJECT.Models;
using CARPOOLING_PROJECT.ViewModels;

namespace CARPOOLING_PROJECT.Controllers
{
    public class DriverController : Controller
    {
        private readonly CarpoolMGSEntities1 _db = new CarpoolMGSEntities1();
        public ActionResult Dashboard()
        {
            EnsureCoreStatuses();
            var driver = GetCurrentDriver(out var redirect);
            if (redirect != null)
            {
                return redirect;
            }

            var rides = _db.Rides
                .Include(r => r.Route)
                .Where(r => r.DriverID == driver.DriverID)
                .OrderByDescending(r => r.PostedAt)
                .ToList();

            var rideIds = rides.Select(r => r.RideID).ToList();

            var bookings = _db.Bookings
                .Include(b => b.Passenger.User)
                .Include(b => b.BookingStatu)
                .Include(b => b.Ride.Route)
                .Where(b => rideIds.Contains(b.RideID))
                .OrderByDescending(b => b.RequestedAt)
                .ToList();

            var bookingStatusLookup = _db.BookingStatus.ToDictionary(b => b.StatusID, b => b.StatusName, EqualityComparer<byte>.Default);

            var model = new DriverDashboardViewModel
            {
                Driver = driver,
                User = driver.User,
                ActiveRides = rides,
                PendingRequests = bookings.Where(b => IsStatus(bookingStatusLookup, b.StatusID, "Pending")),
                CurrentBookings = bookings.Where(b => !IsStatus(bookingStatusLookup, b.StatusID, "Pending")),
                CanPostRide = driver.IsApproved == true && (driver.User?.IsActive ?? true)
            };

            return View(model);
        }

        public ActionResult PostRide()
        {
            EnsureCoreStatuses();
            var driver = GetCurrentDriver(out var redirect);
            if (redirect != null)
            {
                return redirect;
            }

            if (driver.IsApproved != true)
            {
                TempData["Error"] = "Your profile is awaiting admin approval. You cannot post rides yet.";
                return RedirectToAction("Dashboard");
            }

            if (driver.User?.IsActive == false)
            {
                TempData["Error"] = "Your account is banned. Contact admin.";
                return RedirectToAction("Dashboard");
            }

            var vm = new PostRideViewModel
            {
                Seats = 1,
                StartTime = DateTime.UtcNow.AddHours(1),
                PricePerSeat = 250m
            };

            return View(vm);
        }

        [HttpPost]
        public ActionResult PostRide(PostRideViewModel model)
        {
            EnsureCoreStatuses();
            var driver = GetCurrentDriver(out var redirect);
            if (redirect != null)
            {
                return redirect;
            }

            if (driver.IsApproved != true)
            {
                ModelState.AddModelError("", "You must be approved by the admin before posting rides.");
            }

            if (driver.User?.IsActive == false)
            {
                ModelState.AddModelError("", "Your account is banned.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var route = new Route
            {
                RouteID = Guid.NewGuid(),
                Name = model.RouteName.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.Routes.Add(route);
            _db.SaveChanges();

            AddStopsForRoute(route.RouteID, model.StopsText);

            var ride = new Ride
            {
                RideID = Guid.NewGuid(),
                DriverID = driver.DriverID,
                RouteID = route.RouteID,
                TotalSeats = model.Seats,
                AvailableSeats = model.Seats,
                StartTime = model.StartTime,
                PricePerSeat = model.PricePerSeat,
                StatusID = StatusHelper.EnsureRideStatus(_db, "Active"),
                PostedAt = DateTime.UtcNow
            };

            _db.Rides.Add(ride);
            _db.SaveChanges();

            TempData["Message"] = "Ride posted successfully.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public ActionResult CancelRide(Guid id)
        {
            EnsureCoreStatuses();
            var driver = GetCurrentDriver(out var redirect);
            if (redirect != null)
            {
                return redirect;
            }

            var ride = _db.Rides.Include(r => r.Bookings).FirstOrDefault(r => r.RideID == id && r.DriverID == driver.DriverID);
            if (ride == null)
            {
                TempData["Error"] = "Ride not found.";
                return RedirectToAction("Dashboard");
            }

            ride.StatusID = StatusHelper.EnsureRideStatus(_db, "Cancelled");
            ride.AvailableSeats = 0;

            var cancelledStatus = StatusHelper.EnsureBookingStatus(_db, "Cancelled");
            foreach (var booking in ride.Bookings)
            {
                booking.StatusID = cancelledStatus;
                booking.PaymentConfirmed = false;
            }

            _db.SaveChanges();
            TempData["Message"] = "Ride cancelled.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public ActionResult AcceptBooking(Guid id)
        {
            EnsureCoreStatuses();
            var driver = GetCurrentDriver(out var redirect);
            if (redirect != null)
            {
                return redirect;
            }

            var booking = _db.Bookings
                .Include(b => b.Ride)
                .FirstOrDefault(b => b.BookingID == id && b.Ride.DriverID == driver.DriverID);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("Dashboard");
            }

            var pendingId = StatusHelper.EnsureBookingStatus(_db, "Pending");
            if (booking.StatusID != pendingId)
            {
                TempData["Error"] = "Only pending bookings can be accepted.";
                return RedirectToAction("Dashboard");
            }

            if (booking.Ride.AvailableSeats < booking.SeatsRequested)
            {
                TempData["Error"] = "Not enough seats remaining for this ride.";
                return RedirectToAction("Dashboard");
            }

            booking.StatusID = StatusHelper.EnsureBookingStatus(_db, "Accepted");
            booking.ConfirmedAt = DateTime.UtcNow;
            booking.Ride.AvailableSeats -= booking.SeatsRequested;
            _db.SaveChanges();
            TempData["Message"] = "Booking accepted.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public ActionResult RejectBooking(Guid id)
        {
            EnsureCoreStatuses();
            return UpdateBookingStatus(id, "Pending", booking =>
            {
                booking.StatusID = StatusHelper.EnsureBookingStatus(_db, "Rejected");
            }, "Booking rejected.");
        }

        [HttpPost]
        public ActionResult MarkReached(Guid id)
        {
            EnsureCoreStatuses();
            return UpdateBookingStatus(id, "Accepted", booking =>
            {
                booking.StatusID = StatusHelper.EnsureBookingStatus(_db, "InProgress");

                var pickupStopId = booking.PickupStopID;
                RemoveStopFromRouteIfUnused(pickupStopId, booking);
                booking.PickupStopID = null;
            }, "Passenger notified that you have reached.");
        }

        [HttpPost]
        public ActionResult FinishBooking(Guid id)
        {
            EnsureCoreStatuses();
            return UpdateBookingStatus(id, "InProgress", booking =>
            {
                booking.StatusID = StatusHelper.EnsureBookingStatus(_db, "Completed");
                booking.CompletedAt = DateTime.UtcNow;

                if (booking.Ride != null)
                {
                    booking.Ride.AvailableSeats = Math.Min(booking.Ride.TotalSeats, booking.Ride.AvailableSeats + booking.SeatsRequested);
                }

                var dropStopId = booking.DropStopID;
                RemoveStopFromRouteIfUnused(dropStopId, booking);
                booking.DropStopID = null;
            }, "Ride finished. Awaiting cash payment.");
        }

        [HttpPost]
        public ActionResult ConfirmPayment(Guid id)
        {
            EnsureCoreStatuses();
            return UpdateBookingStatus(id, "Completed", booking =>
            {
                if (booking.PaymentConfirmed == true)
                {
                    return;
                }

                booking.PaymentConfirmed = true;
                booking.Ride.AvailableSeats = Math.Min(booking.Ride.TotalSeats, booking.Ride.AvailableSeats + booking.SeatsRequested);
            }, "Payment confirmed.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Helpers

        private ActionResult UpdateBookingStatus(Guid bookingId, string requiredStatus, Action<Booking> updateAction, string successMessage)
        {
            var driver = GetCurrentDriver(out var redirect);
            if (redirect != null)
            {
                return redirect;
            }

            var booking = _db.Bookings
                .Include(b => b.Ride)
                .Include(b => b.Passenger.User)
                .Include(b => b.Stop)
                .Include(b => b.Stop1)
                .FirstOrDefault(b => b.BookingID == bookingId && b.Ride.DriverID == driver.DriverID);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("Dashboard");
            }

            var requiredId = StatusHelper.EnsureBookingStatus(_db, requiredStatus);
            if (booking.StatusID != requiredId)
            {
                TempData["Error"] = $"Booking must be in {requiredStatus} status to perform this action.";
                return RedirectToAction("Dashboard");
            }

            updateAction(booking);
            _db.SaveChanges();
            TempData["Message"] = successMessage;
            return RedirectToAction("Dashboard");
        }

        private Driver GetCurrentDriver(out ActionResult redirect)
        {
            redirect = null;
            if (!(Session["UserId"] is Guid userId) || !string.Equals(Session["Role"] as string, "Driver", StringComparison.OrdinalIgnoreCase))
            {
                redirect = RedirectToAction("Login", "Account");
                return null;
            }

            var driver = _db.Drivers.Include(d => d.User).FirstOrDefault(d => d.UserID == userId);
            if (driver == null)
            {
                Session.Clear();
                redirect = RedirectToAction("Login", "Account");
            }

            return driver;
        }

        private static bool IsStatus(Dictionary<byte, string> lookup, byte statusId, string expected)
        {
            return lookup.TryGetValue(statusId, out var name) && name.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        private void AddStopsForRoute(Guid routeId, string stopsText)
        {
            if (string.IsNullOrWhiteSpace(stopsText))
            {
                return;
            }

            var order = 1;
            var lines = stopsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split('|');
                var name = parts[0].Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                decimal lat = 0;
                decimal lng = 0;
                if (parts.Length >= 3)
                {
                    decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out lat);
                    decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out lng);
                }

                var stop = new Stop
                {
                    StopID = Guid.NewGuid(),
                    RouteID = routeId,
                    LocationName = name,
                    Latitude = lat,
                    Longitude = lng,
                    OrderNo = order++
                };
                _db.Stops.Add(stop);
            }

            _db.SaveChanges();
        }

        private void RemoveStopFromRouteIfUnused(Guid? stopId, Booking booking)
        {
            if (!stopId.HasValue || booking?.Ride == null)
            {
                return;
            }

            var id = stopId.Value;
            var stillReferenced = _db.Bookings.Any(b => b.BookingID != booking.BookingID && (b.PickupStopID == id || b.DropStopID == id));
            if (stillReferenced)
            {
                return;
            }

            var stop = _db.Stops.FirstOrDefault(s => s.StopID == id && s.RouteID == booking.Ride.RouteID);
            if (stop != null)
            {
                _db.Stops.Remove(stop);
            }
        }

        private void EnsureCoreStatuses()
        {
            StatusHelper.EnsureRideStatus(_db, "Active");
            StatusHelper.EnsureRideStatus(_db, "Cancelled");
            StatusHelper.EnsureRideStatus(_db, "Completed");

            StatusHelper.EnsureBookingStatus(_db, "Pending");
            StatusHelper.EnsureBookingStatus(_db, "Accepted");
            StatusHelper.EnsureBookingStatus(_db, "Rejected");
            StatusHelper.EnsureBookingStatus(_db, "Cancelled");
            StatusHelper.EnsureBookingStatus(_db, "InProgress");
            StatusHelper.EnsureBookingStatus(_db, "Completed");
        }

        #endregion
    }
}

