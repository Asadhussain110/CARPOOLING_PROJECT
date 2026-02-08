using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using CARPOOLING_PROJECT.Helpers;
using CARPOOLING_PROJECT.Models;
using CARPOOLING_PROJECT.ViewModels;

namespace CARPOOLING_PROJECT.Controllers
{
    public class PassengerController : Controller
    {
        private readonly CarpoolMGSEntities1 _db = new CarpoolMGSEntities1();
        public ActionResult Dashboard()
        {
            EnsureCoreStatuses();
            var passenger = GetCurrentPassenger(out var redirect);
            if (redirect != null)
            {
                return redirect;
            }

            var activeStatusId = StatusHelper.EnsureRideStatus(_db, "Active");
            var availableRides = _db.Rides
                .Include(r => r.Driver.User)
                .Include(r => r.Route)
                .Where(r => r.AvailableSeats > 0 && r.StatusID == activeStatusId)
                .OrderBy(r => r.StartTime)
                .ToList()
                .Where(r => r.Driver.IsApproved == true
                            && (r.Driver.User?.IsActive ?? true))
                .ToList();

            var routeIds = availableRides.Select(r => r.RouteID).Distinct().ToList();
            var stops = _db.Stops.Where(s => routeIds.Contains(s.RouteID)).OrderBy(s => s.OrderNo).ToList();
            var stopLookup = stops.GroupBy(s => s.RouteID).ToDictionary(g => g.Key, g => g.AsEnumerable());

            var myBookings = _db.Bookings
                .Include(b => b.Ride.Driver.User)
                .Include(b => b.Ride.Route)
                .Include(b => b.BookingStatu)
                .Where(b => b.PassengerID == passenger.PassengerID)
                .OrderByDescending(b => b.RequestedAt)
                .ToList();

            var model = new PassengerDashboardViewModel
            {
                Passenger = passenger,
                User = passenger.User,
                AvailableRides = availableRides,
                MyBookings = myBookings,
                StopsByRoute = stopLookup
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult BookRide(BookRideViewModel model)
        {
            EnsureCoreStatuses();
            var passenger = GetCurrentPassenger(out var redirect);
            if (redirect != null)
            {
                return redirect;
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please provide valid booking details.";
                return RedirectToAction("Dashboard");
            }

            var ride = _db.Rides
                .Include(r => r.Route)
                .Include(r => r.Driver)
                .FirstOrDefault(r => r.RideID == model.RideId);

            if (ride == null)
            {
                TempData["Error"] = "Ride not found.";
                return RedirectToAction("Dashboard");
            }

            var activeStatusId = StatusHelper.EnsureRideStatus(_db, "Active");
            if (ride.StatusID != activeStatusId)
            {
                TempData["Error"] = "Ride is no longer active.";
                return RedirectToAction("Dashboard");
            }

            if (ride.AvailableSeats < model.Seats)
            {
                TempData["Error"] = "Not enough seats available.";
                return RedirectToAction("Dashboard");
            }

            var pickupStop = ResolveStop(ride.RouteID, model.PickupStopId, model.PickupStopName);
            var dropStop = ResolveStop(ride.RouteID, model.DropStopId, model.DropStopName);

            var booking = new Booking
            {
                BookingID = Guid.NewGuid(),
                RideID = ride.RideID,
                PassengerID = passenger.PassengerID,
                PickupStopID = pickupStop?.StopID,
                DropStopID = dropStop?.StopID,
                SeatsRequested = model.Seats,
                Fare = (ride.PricePerSeat * model.Seats),
                StatusID = StatusHelper.EnsureBookingStatus(_db, "Pending"),
                PaymentConfirmed = false,
                RequestedAt = DateTime.UtcNow
            };

            _db.Bookings.Add(booking);
            _db.SaveChanges();

            TempData["Message"] = "Ride requested successfully. Await driver response.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public ActionResult CancelBooking(Guid id)
        {
            EnsureCoreStatuses();
            var passenger = GetCurrentPassenger(out var redirect);
            if (redirect != null)
            {
                return redirect;
            }

            var booking = _db.Bookings.Include(b => b.Ride)
                .FirstOrDefault(b => b.BookingID == id && b.PassengerID == passenger.PassengerID);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("Dashboard");
            }

            var pendingId = StatusHelper.EnsureBookingStatus(_db, "Pending");
            var acceptedId = StatusHelper.EnsureBookingStatus(_db, "Accepted");

            if (booking.StatusID != pendingId && booking.StatusID != acceptedId)
            {
                TempData["Error"] = "Only pending or accepted bookings can be cancelled.";
                return RedirectToAction("Dashboard");
            }

            var releaseSeats = booking.StatusID == acceptedId;
            booking.StatusID = StatusHelper.EnsureBookingStatus(_db, "Cancelled");
            if (releaseSeats)
            {
                booking.Ride.AvailableSeats = Math.Min(booking.Ride.TotalSeats, booking.Ride.AvailableSeats + booking.SeatsRequested);
            }
            _db.SaveChanges();

            TempData["Message"] = "Booking cancelled.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public ActionResult RateDriver(RatingViewModel model)
        {
            EnsureCoreStatuses();
            var passenger = GetCurrentPassenger(out var redirect);
            if (redirect != null)
            {
                return redirect;
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please provide a star rating between 1 and 5.";
                return RedirectToAction("Dashboard");
            }

            var booking = _db.Bookings
                .Include(b => b.Ride.Driver)
                .FirstOrDefault(b => b.BookingID == model.BookingId && b.PassengerID == passenger.PassengerID);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("Dashboard");
            }

            var completedId = StatusHelper.EnsureBookingStatus(_db, "Completed");
            if (booking.StatusID != completedId || booking.PaymentConfirmed != true)
            {
                TempData["Error"] = "You can rate only after the ride is completed and payment is marked paid.";
                return RedirectToAction("Dashboard");
            }

            var rating = new Rating
            {
                RatingID = Guid.NewGuid(),
                RideID = booking.RideID,
                DriverID = booking.Ride.DriverID,
                PassengerID = passenger.PassengerID,
                Stars = model.Stars,
                Feedback = model.Feedback,
                CreatedAt = DateTime.UtcNow
            };

            _db.Ratings.Add(rating);
            UpdateDriverAverage(booking.Ride.DriverID);
            _db.SaveChanges();

            TempData["Message"] = "Thanks for rating your driver!";
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

        #region Helpers

        private Passenger GetCurrentPassenger(out ActionResult redirect)
        {
            redirect = null;
            if (!(Session["UserId"] is Guid userId) || !string.Equals(Session["Role"] as string, "Passenger", StringComparison.OrdinalIgnoreCase))
            {
                redirect = RedirectToAction("Login", "Account");
                return null;
            }

            var passenger = _db.Passengers.Include(p => p.User).FirstOrDefault(p => p.UserID == userId);
            if (passenger == null)
            {
                Session.Clear();
                redirect = RedirectToAction("Login", "Account");
            }

            return passenger;
        }

        private Stop ResolveStop(Guid routeId, Guid? stopId, string stopName)
        {
            Stop stop = null;
            if (stopId.HasValue)
            {
                stop = _db.Stops.FirstOrDefault(s => s.StopID == stopId.Value && s.RouteID == routeId);
            }

            if (stop != null)
            {
                return stop;
            }

            if (string.IsNullOrWhiteSpace(stopName))
            {
                return null;
            }

            stop = _db.Stops.FirstOrDefault(s => s.RouteID == routeId && s.LocationName.Equals(stopName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (stop != null)
            {
                return stop;
            }

            stop = new Stop
            {
                StopID = Guid.NewGuid(),
                RouteID = routeId,
                LocationName = stopName.Trim(),
                Latitude = 0,
                Longitude = 0,
                OrderNo = _db.Stops.Count(s => s.RouteID == routeId) + 1
            };

            _db.Stops.Add(stop);
            _db.SaveChanges();
            return stop;
        }

        private void UpdateDriverAverage(Guid driverId)
        {
            var ratings = _db.Ratings.Where(r => r.DriverID == driverId).Select(r => r.Stars).ToList();
            var driver = _db.Drivers.FirstOrDefault(d => d.DriverID == driverId);
            if (driver == null)
            {
                return;
            }

            if (ratings.Count == 0)
            {
                driver.RatingAvg = 0;
            }
            else
            {
                var avg = ratings.Select(r => (decimal)r).Average();
                driver.RatingAvg = Math.Round(avg, 2);
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

