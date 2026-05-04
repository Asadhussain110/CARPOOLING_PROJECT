using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CARPOOLING_PROJECT.Models;

namespace CARPOOLING_PROJECT.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string SelectedRole { get; set; }
    }

    public class RegisterPassengerViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
    }

    public class RegisterDriverViewModel : RegisterPassengerViewModel
    {
        [Required]
        [Display(Name = "License Number")]
        public string LicenseNo { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public List<DriverListItem> Drivers { get; set; } = new List<DriverListItem>();
        public List<PassengerListItem> Passengers { get; set; } = new List<PassengerListItem>();
    }

    public static class ViewModelDisplayHelper
    {
        private const string ProxyPrefix = "System.Data.Entity.DynamicProxies";

        public static string CleanName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return name.StartsWith(ProxyPrefix, StringComparison.OrdinalIgnoreCase) ? null : name;
        }

        public static string FormatNameOrFallback(string value, string fallback = "(no name)")
        {
            return CleanName(value) ?? fallback;
        }
    }

    public class DriverListItem
    {
        public Driver Driver { get; set; }
        public User User { get; set; }
        public bool IsBanned => !(User?.IsActive ?? true);
        public string DisplayName => ViewModelDisplayHelper.CleanName(User?.Name) ?? User?.Email ?? "(no name)";
        public string DisplayEmail => User?.Email ?? "(no email)";
        public string DisplayPhone => User?.Phone ?? "(no phone)";
        public string ApprovalStatus => Driver?.IsApproved == true ? "Approved" : "Pending/Rejected";
    }

    public class PassengerListItem
    {
        public Passenger Passenger { get; set; }
        public User User { get; set; }
        public bool IsBanned => !(User?.IsActive ?? true);
        public string DisplayName => ViewModelDisplayHelper.CleanName(User?.Name) ?? User?.Email ?? "(no name)";
        public string DisplayEmail => User?.Email ?? "(no email)";
        public string DisplayPhone => User?.Phone ?? "(no phone)";
    }

    public class DriverDashboardViewModel
    {
        public Driver Driver { get; set; }
        public User User { get; set; }
        public IEnumerable<Ride> ActiveRides { get; set; } = new List<Ride>();
        public IEnumerable<Ride> CancelledRides { get; set; } = new List<Ride>();
        public IEnumerable<Booking> PendingRequests { get; set; } = new List<Booking>();
        public IEnumerable<Booking> CurrentBookings { get; set; } = new List<Booking>();
        public bool CanPostRide { get; set; }
    }

    public class PassengerDashboardViewModel
    {
        public Passenger Passenger { get; set; }
        public User User { get; set; }
        public IEnumerable<Ride> AvailableRides { get; set; } = new List<Ride>();
        public IEnumerable<Booking> MyBookings { get; set; } = new List<Booking>();
        public Dictionary<Guid, IEnumerable<Stop>> StopsByRoute { get; set; } = new Dictionary<Guid, IEnumerable<Stop>>();
        public Dictionary<Guid, string> StopLookupById { get; set; } = new Dictionary<Guid, string>();
    }

    public class PostRideViewModel
    {
        [Required]
        [Range(1, 6, ErrorMessage = "Seats must be between 1 and 6")]
        public int Seats { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }

        [Required]
        [Display(Name = "Route Name")]
        public string RouteName { get; set; }

        [Display(Name = "Stops (one per line, optional lat,lng)")]
        public string StopsText { get; set; }

        [Required]
        [Display(Name = "Price Per Seat (PKR)")]
        [Range(1, 100000, ErrorMessage = "Price must be at least 1 PKR")]
        public decimal PricePerSeat { get; set; }
    }

    public class BookRideViewModel
    {
        [Required]
        public Guid RideId { get; set; }

        [Required]
        [Range(1, 6)]
        public int Seats { get; set; }

        public Guid? PickupStopId { get; set; }
        public Guid? DropStopId { get; set; }

        [Display(Name = "Pickup Stop")]
        public string PickupStopName { get; set; }

        [Display(Name = "Drop Stop")]
        public string DropStopName { get; set; }
    }

    public class RatingViewModel
    {
        [Required]
        public Guid BookingId { get; set; }

        [Required]
        [Range(1, 5)]
        public byte Stars { get; set; }

        [StringLength(500)]
        public string Feedback { get; set; }
    }

    public class EditRideViewModel : PostRideViewModel
    {
        [Required]
        public Guid RideID { get; set; }
    }
}

