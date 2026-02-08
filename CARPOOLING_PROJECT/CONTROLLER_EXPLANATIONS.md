# Detailed Controller Code Explanations

## 1. AccountController.cs - Authentication & Registration Controller

### Namespace & Imports (Lines 1-8)
```csharp
using System;                    // Basic .NET types (DateTime, Guid, etc.)
using System.Data.Entity;        // Entity Framework for database operations
using System.Linq;               // LINQ queries
using System.Security.Cryptography; // For password hashing (SHA256)
using System.Text;               // Encoding for string operations
using System.Web.Mvc;            // ASP.NET MVC framework
using CARPOOLING_PROJECT.Models; // Database models
using CARPOOLING_PROJECT.ViewModels; // View models for data transfer
```

### Class Declaration (Lines 10-16)
```csharp
namespace CARPOOLING_PROJECT.Controllers
{
    public class AccountController : Controller  // Inherits from MVC Controller base class
    {
        private readonly CarpoolMGSEntities1 _db = new CarpoolMGSEntities1();  // Database context (Entity Framework)
        private const string AdminEmail = "admin@carpool.com";  // Hardcoded admin email
        private const string AdminPassword = "Admin@123";        // Hardcoded admin password
```

**Line 14**: Creates a database context instance for all database operations in this controller.
**Lines 15-16**: Constants for default admin account credentials.

### Login GET Action (Lines 18-22)
```csharp
public ActionResult Login()
{
    EnsureSeedData();  // Ensures admin user and roles exist in database
    return View(new LoginViewModel());  // Returns empty login form
}
```
**Line 20**: Calls helper to create admin user if missing.
**Line 21**: Returns the login view with an empty view model.

### Login POST Action (Lines 24-102)
```csharp
[HttpPost]  // Only accepts HTTP POST requests
[ValidateAntiForgeryToken]  // Prevents CSRF attacks
public ActionResult Login(LoginViewModel model)
{
    EnsureSeedData();  // Ensure admin exists

    if (!ModelState.IsValid)  // Check if form validation passed
    {
        return View(model);  // Return view with validation errors
    }

    if (string.IsNullOrWhiteSpace(model.SelectedRole))  // Check if role was selected
    {
        ModelState.AddModelError("", "Please choose whether you are an Admin, Driver or Passenger.");
        return View(model);
    }

    var role = FindRole(model.SelectedRole);  // Find role in database
    if (role == null)  // If role doesn't exist
    {
        ModelState.AddModelError("", "Selected role is not configured yet.");
        return View(model);
    }

    var email = model.Email.Trim().ToLowerInvariant();  // Normalize email (lowercase, trimmed)
    var user = _db.Users
        .Include(u => u.Drivers)      // Eager load Driver navigation property
        .Include(u => u.Passengers)   // Eager load Passenger navigation property
        .FirstOrDefault(u => u.Email.ToLower() == email && u.RoleID == role.RoleID);  // Find user by email and role

    if (user == null || !VerifyPassword(model.Password, user.PasswordHash))  // Check if user exists and password matches
    {
        ModelState.AddModelError("", "Invalid email or password.");
        return View(model);
    }

    if (user.IsActive == false)  // Check if account is banned
    {
        ModelState.AddModelError("", "Your account is currently banned. Please contact support.");
        return View(model);
    }

    // Store user info in session
    Session["UserId"] = user.UserID;      // Store user ID
    Session["Role"] = role.RoleName;     // Store role name
    Session["DisplayName"] = user.Name;  // Store display name

    // Redirect based on role
    if (role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
    {
        return RedirectToAction("Dashboard", "Admin");  // Redirect to admin dashboard
    }

    if (role.RoleName.Equals("Driver", StringComparison.OrdinalIgnoreCase))
    {
        if (!user.Drivers.Any())  // Check if driver profile exists
        {
            ModelState.AddModelError("", "Driver profile is missing for this user.");
            Session.Clear();  // Clear session on error
            return View(model);
        }
        return RedirectToAction("Dashboard", "Driver");  // Redirect to driver dashboard
    }

    if (role.RoleName.Equals("Passenger", StringComparison.OrdinalIgnoreCase))
    {
        if (!user.Passengers.Any())  // Check if passenger profile exists
        {
            ModelState.AddModelError("", "Passenger profile is missing for this user.");
            Session.Clear();
            return View(model);
        }
        return RedirectToAction("Dashboard", "Passenger");  // Redirect to passenger dashboard
    }

    Session.Clear();  // Clear session if role not recognized
    ModelState.AddModelError("", "Role not recognized.");
    return View(model);
}
```

### RegisterPassenger GET Action (Lines 104-107)
```csharp
public ActionResult RegisterPassenger()
{
    return View(new RegisterPassengerViewModel());  // Return empty registration form
}
```

### RegisterPassenger POST Action (Lines 109-152)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult RegisterPassenger(RegisterPassengerViewModel model)
{
    if (!ModelState.IsValid)  // Validate form data
    {
        return View(model);
    }

    var normalizedEmail = model.Email.Trim().ToLowerInvariant();  // Normalize email
    if (_db.Users.Any(u => u.Email.ToLower() == normalizedEmail))  // Check if email already exists
    {
        ModelState.AddModelError("Email", "An account with this email already exists.");
        return View(model);
    }

    var role = EnsureRole("Passenger");  // Get or create Passenger role
    var user = new User  // Create new User entity
    {
        UserID = Guid.NewGuid(),              // Generate unique ID
        Name = model.Name.Trim(),             // User's name
        Email = normalizedEmail,              // Normalized email
        Phone = model.Phone,                  // Phone number
        PasswordHash = HashPassword(model.Password),  // Hash password
        RoleID = role.RoleID,                 // Assign Passenger role
        CreatedAt = DateTime.UtcNow,         // Timestamp
        IsActive = true                       // Account is active
    };

    var passenger = new Passenger  // Create Passenger profile
    {
        PassengerID = Guid.NewGuid(),  // Generate unique ID
        UserID = user.UserID,          // Link to User
        CreatedAt = DateTime.UtcNow,   // Timestamp
        RatingAvg = 0                   // Initial rating
    };

    _db.Users.Add(user);           // Add user to database context
    _db.Passengers.Add(passenger);  // Add passenger to database context
    _db.SaveChanges();              // Save changes to database

    TempData["Message"] = "Passenger registered successfully. You can now log in.";
    return RedirectToAction("Login");  // Redirect to login page
}
```

### RegisterDriver GET Action (Lines 154-157)
```csharp
public ActionResult RegisterDriver()
{
    return View(new RegisterDriverViewModel());  // Return empty driver registration form
}
```

### RegisterDriver POST Action (Lines 159-204)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult RegisterDriver(RegisterDriverViewModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var normalizedEmail = model.Email.Trim().ToLowerInvariant();
    if (_db.Users.Any(u => u.Email.ToLower() == normalizedEmail))  // Check email uniqueness
    {
        ModelState.AddModelError("Email", "An account with this email already exists.");
        return View(model);
    }

    var role = EnsureRole("Driver");  // Get or create Driver role
    var user = new User
    {
        UserID = Guid.NewGuid(),
        Name = model.Name.Trim(),
        Email = normalizedEmail,
        Phone = model.Phone,
        PasswordHash = HashPassword(model.Password),
        RoleID = role.RoleID,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    var driver = new Driver  // Create Driver profile
    {
        DriverID = Guid.NewGuid(),
        UserID = user.UserID,
        LicenseNo = model.LicenseNo,    // Driver's license number
        IsApproved = false,             // Requires admin approval
        CreatedAt = DateTime.UtcNow,
        RatingAvg = 0
    };

    _db.Users.Add(user);
    _db.Drivers.Add(driver);
    _db.SaveChanges();

    TempData["Message"] = "Driver registered successfully. Please wait for admin approval.";
    return RedirectToAction("Login");
}
```

### Logout Action (Lines 206-210)
```csharp
public ActionResult Logout()
{
    Session.Clear();  // Clear all session data
    return RedirectToAction("Login");  // Redirect to login page
}
```

### Dispose Method (Lines 212-219)
```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        _db.Dispose();  // Release database context resources
    }
    base.Dispose(disposing);  // Call base class dispose
}
```
**Purpose**: Properly dispose of database context to prevent memory leaks.

### Helper Methods (Lines 221-294)

#### EnsureSeedData (Lines 223-248)
```csharp
private void EnsureSeedData()
{
    var adminRole = EnsureRole("Admin");  // Ensure Admin role exists
    EnsureRole("Driver");                 // Ensure Driver role exists
    EnsureRole("Passenger");             // Ensure Passenger role exists

    var normalizedAdminEmail = AdminEmail.ToLower();
    var adminUser = _db.Users.FirstOrDefault(u => u.Email.ToLower() == normalizedAdminEmail);
    if (adminUser == null)  // If admin user doesn't exist, create it
    {
        adminUser = new User
        {
            UserID = Guid.NewGuid(),
            Name = "System Admin",
            Email = AdminEmail,
            Phone = "000-0000000",
            PasswordHash = HashPassword(AdminPassword),  // Hash the admin password
            RoleID = adminRole.RoleID,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Users.Add(adminUser);
        _db.SaveChanges();
    }
}
```
**Purpose**: Ensures default admin user and all roles exist in the database.

#### EnsureRole (Lines 250-267)
```csharp
private Role EnsureRole(string roleName)
{
    var role = FindRole(roleName);  // Try to find existing role
    if (role != null)
    {
        return role;  // Return if exists
    }

    // Create new role if it doesn't exist
    role = new Role
    {
        RoleID = (byte)(_db.Roles.Any() ? _db.Roles.Max(r => r.RoleID) + 1 : 1),  // Auto-increment ID
        RoleName = roleName
    };

    _db.Roles.Add(role);
    _db.SaveChanges();
    return role;
}
```
**Purpose**: Gets existing role or creates it if missing.

#### FindRole (Lines 269-273)
```csharp
private Role FindRole(string roleName)
{
    var normalized = roleName?.Trim();  // Normalize role name
    return _db.Roles.FirstOrDefault(r => r.RoleName.Equals(normalized, StringComparison.OrdinalIgnoreCase));  // Case-insensitive search
}
```
**Purpose**: Finds a role by name (case-insensitive).

#### HashPassword (Lines 275-281)
```csharp
private static byte[] HashPassword(string password)
{
    using (var sha = SHA256.Create())  // Create SHA256 hasher
    {
        return sha.ComputeHash(Encoding.UTF8.GetBytes(password));  // Hash password and return byte array
    }
}
```
**Purpose**: Hashes password using SHA256 algorithm for secure storage.

#### VerifyPassword (Lines 283-292)
```csharp
private static bool VerifyPassword(string password, byte[] storedHash)
{
    if (storedHash == null)  // If no stored hash, password is invalid
    {
        return false;
    }

    var attempted = HashPassword(password);  // Hash the attempted password
    return attempted.SequenceEqual(storedHash);  // Compare byte arrays
}
```
**Purpose**: Verifies if provided password matches stored hash.

---

## 2. AdminController.cs - Admin Management Controller

### Class Declaration (Lines 8-12)
```csharp
namespace CARPOOLING_PROJECT.Controllers
{
    public class AdminController : Controller
    {
        private readonly CarpoolMGSEntities1 _db = new CarpoolMGSEntities1();  // Database context
```

### Dashboard Action (Lines 14-37)
```csharp
public ActionResult Dashboard()
{
    var redirect = EnsureAdminAccess();  // Check if user is admin
    if (redirect != null)  // If not admin, redirect to login
    {
        return redirect;
    }

    var model = new AdminDashboardViewModel
    {
        Drivers = _db.Drivers.Include(d => d.User)  // Load drivers with user info
            .OrderByDescending(d => d.CreatedAt)     // Order by newest first
            .ToList()
            .Select(d => new DriverListItem { Driver = d, User = d.User })  // Project to view model
            .ToList(),
        Passengers = _db.Passengers.Include(p => p.User)  // Load passengers with user info
            .OrderByDescending(p => p.CreatedAt)
            .ToList()
            .Select(p => new PassengerListItem { Passenger = p, User = p.User })
            .ToList()
    };

    return View(model);  // Return dashboard view with data
}
```
**Purpose**: Displays all drivers and passengers for admin management.

### ApproveDriver Action (Lines 39-60)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult ApproveDriver(Guid id)  // id = DriverID
{
    var redirect = EnsureAdminAccess();
    if (redirect != null)
    {
        return redirect;
    }

    var driver = _db.Drivers.Include(d => d.User).FirstOrDefault(d => d.DriverID == id);
    if (driver == null)  // Check if driver exists
    {
        TempData["Error"] = "Driver not found.";
        return RedirectToAction("Dashboard");
    }

    driver.IsApproved = true;  // Approve the driver
    _db.SaveChanges();         // Save changes
    TempData["Message"] = $"Driver {driver.User?.Name} approved.";
    return RedirectToAction("Dashboard");
}
```
**Purpose**: Approves a driver, allowing them to post rides.

### RejectDriver Action (Lines 62-83)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
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

    driver.IsApproved = false;  // Reject the driver
    _db.SaveChanges();
    TempData["Message"] = $"Driver {driver.User?.Name} rejected.";
    return RedirectToAction("Dashboard");
}
```
**Purpose**: Rejects a driver, preventing them from posting rides.

### ToggleDriverBan Action (Lines 85-106)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
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

    driver.User.IsActive = !(driver.User.IsActive ?? true);  // Toggle ban status (invert current state)
    _db.SaveChanges();
    TempData["Message"] = $"Driver {driver.User.Name} {(driver.User.IsActive == true ? "unbanned" : "banned")}.";
    return RedirectToAction("Dashboard");
}
```
**Purpose**: Toggles ban status of a driver account.

### TogglePassengerBan Action (Lines 108-129)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
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

    passenger.User.IsActive = !(passenger.User.IsActive ?? true);  // Toggle ban status
    _db.SaveChanges();
    TempData["Message"] = $"Passenger {passenger.User.Name} {(passenger.User.IsActive == true ? "unbanned" : "banned")}.";
    return RedirectToAction("Dashboard");
}
```
**Purpose**: Toggles ban status of a passenger account.

### Dispose Method (Lines 131-138)
```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        _db.Dispose();
    }
    base.Dispose(disposing);
}
```

### EnsureAdminAccess Helper (Lines 140-150)
```csharp
private ActionResult EnsureAdminAccess()
{
    var role = Session["Role"] as string;  // Get role from session
    if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))  // Check if admin
    {
        Session.Clear();  // Clear session if not admin
        return RedirectToAction("Login", "Account");  // Redirect to login
    }

    return null;  // Return null if access is granted
}
```
**Purpose**: Security check to ensure only admins can access admin actions.

---

## 3. DriverController.cs - Driver Operations Controller

### Class Declaration (Lines 11-15)
```csharp
namespace CARPOOLING_PROJECT.Controllers
{
    public class DriverController : Controller
    {
        private readonly CarpoolMGSEntities1 _db = new CarpoolMGSEntities1();
```

### Dashboard Action (Lines 16-54)
```csharp
public ActionResult Dashboard()
{
    EnsureCoreStatuses();  // Ensure all status types exist
    var driver = GetCurrentDriver(out var redirect);  // Get logged-in driver
    if (redirect != null)
    {
        return redirect;
    }

    // Get all rides posted by this driver
    var rides = _db.Rides
        .Include(r => r.Route)  // Include route information
        .Where(r => r.DriverID == driver.DriverID)  // Filter by driver
        .OrderByDescending(r => r.PostedAt)  // Newest first
        .ToList();

    var rideIds = rides.Select(r => r.RideID).ToList();  // Get list of ride IDs

    // Get all bookings for this driver's rides
    var bookings = _db.Bookings
        .Include(b => b.Passenger.User)      // Include passenger and user info
        .Include(b => b.BookingStatu)         // Include booking status
        .Include(b => b.Ride.Route)           // Include ride and route info
        .Where(b => rideIds.Contains(b.RideID))  // Filter by driver's rides
        .OrderByDescending(b => b.RequestedAt)    // Newest first
        .ToList();

    var bookingStatusLookup = _db.BookingStatus.ToDictionary(b => b.StatusID, b => b.StatusName, EqualityComparer<byte>.Default);  // Create lookup dictionary

    var model = new DriverDashboardViewModel
    {
        Driver = driver,
        User = driver.User,
        ActiveRides = rides,
        PendingRequests = bookings.Where(b => IsStatus(bookingStatusLookup, b.StatusID, "Pending")),  // Filter pending bookings
        CurrentBookings = bookings.Where(b => !IsStatus(bookingStatusLookup, b.StatusID, "Pending")),   // Filter non-pending bookings
        CanPostRide = driver.IsApproved == true && (driver.User?.IsActive ?? true)  // Check if driver can post rides
    };

    return View(model);
}
```
**Purpose**: Shows driver's rides, pending requests, and current bookings.

### PostRide GET Action (Lines 56-85)
```csharp
public ActionResult PostRide()
{
    EnsureCoreStatuses();
    var driver = GetCurrentDriver(out var redirect);
    if (redirect != null)
    {
        return redirect;
    }

    if (driver.IsApproved != true)  // Check if driver is approved
    {
        TempData["Error"] = "Your profile is awaiting admin approval. You cannot post rides yet.";
        return RedirectToAction("Dashboard");
    }

    if (driver.User?.IsActive == false)  // Check if account is banned
    {
        TempData["Error"] = "Your account is banned. Contact admin.";
        return RedirectToAction("Dashboard");
    }

    var vm = new PostRideViewModel
    {
        Seats = 1,                              // Default seats
        StartTime = DateTime.UtcNow.AddHours(1), // Default: 1 hour from now
        PricePerSeat = 250m                      // Default price
    };

    return View(vm);
}
```
**Purpose**: Displays form to post a new ride.

### PostRide POST Action (Lines 87-143)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult PostRide(PostRideViewModel model)
{
    EnsureCoreStatuses();
    var driver = GetCurrentDriver(out var redirect);
    if (redirect != null)
    {
        return redirect;
    }

    if (driver.IsApproved != true)  // Validate approval status
    {
        ModelState.AddModelError("", "You must be approved by the admin before posting rides.");
    }

    if (driver.User?.IsActive == false)  // Validate account status
    {
        ModelState.AddModelError("", "Your account is banned.");
    }

    if (!ModelState.IsValid)  // Check validation
    {
        return View(model);
    }

    // Create new route
    var route = new Route
    {
        RouteID = Guid.NewGuid(),
        Name = model.RouteName.Trim(),
        CreatedAt = DateTime.UtcNow
    };

    _db.Routes.Add(route);
    _db.SaveChanges();

    AddStopsForRoute(route.RouteID, model.StopsText);  // Add stops to route

    // Create new ride
    var ride = new Ride
    {
        RideID = Guid.NewGuid(),
        DriverID = driver.DriverID,
        RouteID = route.RouteID,
        TotalSeats = model.Seats,
        AvailableSeats = model.Seats,  // Initially all seats available
        StartTime = model.StartTime,
        PricePerSeat = model.PricePerSeat,
        StatusID = StatusHelper.EnsureRideStatus(_db, "Active"),  // Set status to Active
        PostedAt = DateTime.UtcNow
    };

    _db.Rides.Add(ride);
    _db.SaveChanges();

    TempData["Message"] = "Ride posted successfully.";
    return RedirectToAction("Dashboard");
}
```
**Purpose**: Creates a new ride with route and stops.

### CancelRide Action (Lines 145-176)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult CancelRide(Guid id)  // id = RideID
{
    EnsureCoreStatuses();
    var driver = GetCurrentDriver(out var redirect);
    if (redirect != null)
    {
        return redirect;
    }

    var ride = _db.Rides.Include(r => r.Bookings).FirstOrDefault(r => r.RideID == id && r.DriverID == driver.DriverID);
    if (ride == null)  // Verify ride exists and belongs to driver
    {
        TempData["Error"] = "Ride not found.";
        return RedirectToAction("Dashboard");
    }

    ride.StatusID = StatusHelper.EnsureRideStatus(_db, "Cancelled");  // Mark ride as cancelled
    ride.AvailableSeats = 0;  // Set available seats to 0

    var cancelledStatus = StatusHelper.EnsureBookingStatus(_db, "Cancelled");
    foreach (var booking in ride.Bookings)  // Cancel all bookings for this ride
    {
        booking.StatusID = cancelledStatus;
        booking.PaymentConfirmed = false;
    }

    _db.SaveChanges();
    TempData["Message"] = "Ride cancelled.";
    return RedirectToAction("Dashboard");
}
```
**Purpose**: Cancels a ride and all associated bookings.

### AcceptBooking Action (Lines 178-218)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult AcceptBooking(Guid id)  // id = BookingID
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
    if (booking.StatusID != pendingId)  // Only accept pending bookings
    {
        TempData["Error"] = "Only pending bookings can be accepted.";
        return RedirectToAction("Dashboard");
    }

    if (booking.Ride.AvailableSeats < booking.SeatsRequested)  // Check seat availability
    {
        TempData["Error"] = "Not enough seats remaining for this ride.";
        return RedirectToAction("Dashboard");
    }

    booking.StatusID = StatusHelper.EnsureBookingStatus(_db, "Accepted");  // Accept booking
    booking.ConfirmedAt = DateTime.UtcNow;  // Set confirmation time
    booking.Ride.AvailableSeats -= booking.SeatsRequested;  // Reduce available seats
    _db.SaveChanges();
    TempData["Message"] = "Booking accepted.";
    return RedirectToAction("Dashboard");
}
```
**Purpose**: Accepts a passenger's booking request.

### RejectBooking Action (Lines 220-229)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult RejectBooking(Guid id)
{
    EnsureCoreStatuses();
    return UpdateBookingStatus(id, "Pending", booking =>  // Use helper method
    {
        booking.StatusID = StatusHelper.EnsureBookingStatus(_db, "Rejected");  // Set status to Rejected
    }, "Booking rejected.");
}
```
**Purpose**: Rejects a passenger's booking request.

### MarkReached Action (Lines 231-244)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult MarkReached(Guid id)
{
    EnsureCoreStatuses();
    return UpdateBookingStatus(id, "Accepted", booking =>  // Only for Accepted bookings
    {
        booking.StatusID = StatusHelper.EnsureBookingStatus(_db, "InProgress");  // Mark as InProgress

        var pickupStopId = booking.PickupStopID;
        RemoveStopFromRouteIfUnused(pickupStopId, booking);  // Clean up unused stop
        booking.PickupStopID = null;  // Clear pickup stop reference
    }, "Passenger notified that you have reached.");
}
```
**Purpose**: Marks that driver has reached pickup location.

### FinishBooking Action (Lines 246-265)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult FinishBooking(Guid id)
{
    EnsureCoreStatuses();
    return UpdateBookingStatus(id, "InProgress", booking =>  // Only for InProgress bookings
    {
        booking.StatusID = StatusHelper.EnsureBookingStatus(_db, "Completed");  // Mark as Completed
        booking.CompletedAt = DateTime.UtcNow;  // Set completion time

        if (booking.Ride != null)
        {
            // Release seats back (but don't exceed total seats)
            booking.Ride.AvailableSeats = Math.Min(booking.Ride.TotalSeats, booking.Ride.AvailableSeats + booking.SeatsRequested);
        }

        var dropStopId = booking.DropStopID;
        RemoveStopFromRouteIfUnused(dropStopId, booking);  // Clean up unused stop
        booking.DropStopID = null;  // Clear drop stop reference
    }, "Ride finished. Awaiting cash payment.");
}
```
**Purpose**: Marks ride as completed and releases seats.

### ConfirmPayment Action (Lines 267-282)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult ConfirmPayment(Guid id)
{
    EnsureCoreStatuses();
    return UpdateBookingStatus(id, "Completed", booking =>  // Only for Completed bookings
    {
        if (booking.PaymentConfirmed == true)  // Skip if already confirmed
        {
            return;
        }

        booking.PaymentConfirmed = true;  // Mark payment as confirmed
        booking.Ride.AvailableSeats = Math.Min(booking.Ride.TotalSeats, booking.Ride.AvailableSeats + booking.SeatsRequested);  // Release seats
    }, "Payment confirmed.");
}
```
**Purpose**: Confirms cash payment received from passenger.

### Helper Methods (Lines 293-429)

#### UpdateBookingStatus (Lines 295-327)
```csharp
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
        .Include(b => b.Stop)      // Pickup stop
        .Include(b => b.Stop1)      // Drop stop
        .FirstOrDefault(b => b.BookingID == bookingId && b.Ride.DriverID == driver.DriverID);

    if (booking == null)
    {
        TempData["Error"] = "Booking not found.";
        return RedirectToAction("Dashboard");
    }

    var requiredId = StatusHelper.EnsureBookingStatus(_db, requiredStatus);
    if (booking.StatusID != requiredId)  // Verify booking is in correct status
    {
        TempData["Error"] = $"Booking must be in {requiredStatus} status to perform this action.";
        return RedirectToAction("Dashboard");
    }

    updateAction(booking);  // Execute the update action
    _db.SaveChanges();
    TempData["Message"] = successMessage;
    return RedirectToAction("Dashboard");
}
```
**Purpose**: Generic helper to update booking status with validation.

#### GetCurrentDriver (Lines 329-346)
```csharp
private Driver GetCurrentDriver(out ActionResult redirect)
{
    redirect = null;
    if (!(Session["UserId"] is Guid userId) || !string.Equals(Session["Role"] as string, "Driver", StringComparison.OrdinalIgnoreCase))
    {
        redirect = RedirectToAction("Login", "Account");  // Redirect if not logged in as driver
        return null;
    }

    var driver = _db.Drivers.Include(d => d.User).FirstOrDefault(d => d.UserID == userId);
    if (driver == null)  // If driver profile not found
    {
        Session.Clear();
        redirect = RedirectToAction("Login", "Account");
    }

    return driver;
}
```
**Purpose**: Gets the currently logged-in driver with authentication check.

#### IsStatus (Lines 348-351)
```csharp
private static bool IsStatus(Dictionary<byte, string> lookup, byte statusId, string expected)
{
    return lookup.TryGetValue(statusId, out var name) && name.Equals(expected, StringComparison.OrdinalIgnoreCase);
}
```
**Purpose**: Checks if a status ID matches an expected status name.

#### AddStopsForRoute (Lines 353-392)
```csharp
private void AddStopsForRoute(Guid routeId, string stopsText)
{
    if (string.IsNullOrWhiteSpace(stopsText))  // If no stops provided
    {
        return;
    }

    var order = 1;
    var lines = stopsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);  // Split by newlines
    foreach (var line in lines)
    {
        var parts = line.Split('|');  // Format: "Name|Latitude|Longitude"
        var name = parts[0].Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            continue;  // Skip empty lines
        }

        decimal lat = 0;
        decimal lng = 0;
        if (parts.Length >= 3)  // If coordinates provided
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
            OrderNo = order++  // Increment order number
        };
        _db.Stops.Add(stop);
    }

    _db.SaveChanges();
}
```
**Purpose**: Parses stops text and creates Stop entities for a route.

#### RemoveStopFromRouteIfUnused (Lines 394-413)
```csharp
private void RemoveStopFromRouteIfUnused(Guid? stopId, Booking booking)
{
    if (!stopId.HasValue || booking?.Ride == null)  // If no stop ID or no booking
    {
        return;
    }

    var id = stopId.Value;
    var stillReferenced = _db.Bookings.Any(b => b.BookingID != booking.BookingID && (b.PickupStopID == id || b.DropStopID == id));  // Check if stop is used by other bookings
    if (stillReferenced)
    {
        return;  // Don't remove if still in use
    }

    var stop = _db.Stops.FirstOrDefault(s => s.StopID == id && s.RouteID == booking.Ride.RouteID);
    if (stop != null)
    {
        _db.Stops.Remove(stop);  // Remove unused stop
    }
}
```
**Purpose**: Removes a stop from route if no other bookings reference it.

#### EnsureCoreStatuses (Lines 415-427)
```csharp
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
```
**Purpose**: Ensures all required status types exist in database.

---

## 4. HomeController.cs - Home Page Controller

### Class Declaration (Lines 3-11)
```csharp
namespace CARPOOLING_PROJECT.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()  // Default action
        {
            return View();  // Returns the home page view
        }
    }
}
```
**Purpose**: Simple controller for the home/landing page. No database operations needed.

---

## 5. PassengerController.cs - Passenger Operations Controller

### Class Declaration (Lines 10-14)
```csharp
namespace CARPOOLING_PROJECT.Controllers
{
    public class PassengerController : Controller
    {
        private readonly CarpoolMGSEntities1 _db = new CarpoolMGSEntities1();
```

### Dashboard Action (Lines 15-57)
```csharp
public ActionResult Dashboard()
{
    EnsureCoreStatuses();
    var passenger = GetCurrentPassenger(out var redirect);
    if (redirect != null)
    {
        return redirect;
    }

    var activeStatusId = StatusHelper.EnsureRideStatus(_db, "Active");
    // Get all active rides with available seats
    var availableRides = _db.Rides
        .Include(r => r.Driver.User)      // Include driver and user info
        .Include(r => r.Route)            // Include route info
        .Where(r => r.AvailableSeats > 0 && r.StatusID == activeStatusId)  // Filter active rides with seats
        .OrderBy(r => r.StartTime)        // Order by start time
        .ToList()
        .Where(r => r.Driver.IsApproved == true && (r.Driver.User?.IsActive ?? true))  // Filter approved, active drivers
        .ToList();

    var routeIds = availableRides.Select(r => r.RouteID).Distinct().ToList();  // Get unique route IDs
    var stops = _db.Stops.Where(s => routeIds.Contains(s.RouteID)).OrderBy(s => s.OrderNo).ToList();  // Get all stops for these routes
    var stopLookup = stops.GroupBy(s => s.RouteID).ToDictionary(g => g.Key, g => g.AsEnumerable());  // Group stops by route

    // Get passenger's bookings
    var myBookings = _db.Bookings
        .Include(b => b.Ride.Driver.User)
        .Include(b => b.Ride.Route)
        .Include(b => b.BookingStatu)
        .Where(b => b.PassengerID == passenger.PassengerID)  // Filter by current passenger
        .OrderByDescending(b => b.RequestedAt)  // Newest first
        .ToList();

    var model = new PassengerDashboardViewModel
    {
        Passenger = passenger,
        User = passenger.User,
        AvailableRides = availableRides,
        MyBookings = myBookings,
        StopsByRoute = stopLookup  // Dictionary of stops grouped by route
    };

    return View(model);
}
```
**Purpose**: Shows available rides and passenger's booking history.

### BookRide Action (Lines 59-122)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult BookRide(BookRideViewModel model)
{
    EnsureCoreStatuses();
    var passenger = GetCurrentPassenger(out var redirect);
    if (redirect != null)
    {
        return redirect;
    }

    if (!ModelState.IsValid)  // Validate form
    {
        TempData["Error"] = "Please provide valid booking details.";
        return RedirectToAction("Dashboard");
    }

    var ride = _db.Rides
        .Include(r => r.Route)
        .Include(r => r.Driver)
        .FirstOrDefault(r => r.RideID == model.RideId);

    if (ride == null)  // Verify ride exists
    {
        TempData["Error"] = "Ride not found.";
        return RedirectToAction("Dashboard");
    }

    var activeStatusId = StatusHelper.EnsureRideStatus(_db, "Active");
    if (ride.StatusID != activeStatusId)  // Verify ride is active
    {
        TempData["Error"] = "Ride is no longer active.";
        return RedirectToAction("Dashboard");
    }

    if (ride.AvailableSeats < model.Seats)  // Check seat availability
    {
        TempData["Error"] = "Not enough seats available.";
        return RedirectToAction("Dashboard");
    }

    var pickupStop = ResolveStop(ride.RouteID, model.PickupStopId, model.PickupStopName);  // Resolve or create pickup stop
    var dropStop = ResolveStop(ride.RouteID, model.DropStopId, model.DropStopName);        // Resolve or create drop stop

    var booking = new Booking
    {
        BookingID = Guid.NewGuid(),
        RideID = ride.RideID,
        PassengerID = passenger.PassengerID,
        PickupStopID = pickupStop?.StopID,      // Optional pickup stop
        DropStopID = dropStop?.StopID,          // Optional drop stop
        SeatsRequested = model.Seats,
        Fare = (ride.PricePerSeat * model.Seats),  // Calculate total fare
        StatusID = StatusHelper.EnsureBookingStatus(_db, "Pending"),  // Initial status: Pending
        PaymentConfirmed = false,
        RequestedAt = DateTime.UtcNow
    };

    _db.Bookings.Add(booking);
    _db.SaveChanges();

    TempData["Message"] = "Ride requested successfully. Await driver response.";
    return RedirectToAction("Dashboard");
}
```
**Purpose**: Creates a booking request for a ride.

### CancelBooking Action (Lines 124-163)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult CancelBooking(Guid id)  // id = BookingID
{
    EnsureCoreStatuses();
    var passenger = GetCurrentPassenger(out var redirect);
    if (redirect != null)
    {
        return redirect;
    }

    var booking = _db.Bookings.Include(b => b.Ride)
        .FirstOrDefault(b => b.BookingID == id && b.PassengerID == passenger.PassengerID);

    if (booking == null)  // Verify booking exists and belongs to passenger
    {
        TempData["Error"] = "Booking not found.";
        return RedirectToAction("Dashboard");
    }

    var pendingId = StatusHelper.EnsureBookingStatus(_db, "Pending");
    var acceptedId = StatusHelper.EnsureBookingStatus(_db, "Accepted");

    if (booking.StatusID != pendingId && booking.StatusID != acceptedId)  // Only cancel pending or accepted bookings
    {
        TempData["Error"] = "Only pending or accepted bookings can be cancelled.";
        return RedirectToAction("Dashboard");
    }

    var releaseSeats = booking.StatusID == acceptedId;  // Only release seats if booking was accepted
    booking.StatusID = StatusHelper.EnsureBookingStatus(_db, "Cancelled");
    if (releaseSeats)
    {
        booking.Ride.AvailableSeats = Math.Min(booking.Ride.TotalSeats, booking.Ride.AvailableSeats + booking.SeatsRequested);  // Release seats
    }
    _db.SaveChanges();

    TempData["Message"] = "Booking cancelled.";
    return RedirectToAction("Dashboard");
}
```
**Purpose**: Cancels a passenger's booking.

### RateDriver Action (Lines 165-216)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult RateDriver(RatingViewModel model)
{
    EnsureCoreStatuses();
    var passenger = GetCurrentPassenger(out var redirect);
    if (redirect != null)
    {
        return redirect;
    }

    if (!ModelState.IsValid)  // Validate rating (1-5 stars)
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
    if (booking.StatusID != completedId || booking.PaymentConfirmed != true)  // Only rate completed, paid rides
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
        Stars = model.Stars,           // Rating (1-5)
        Feedback = model.Feedback,     // Optional feedback text
        CreatedAt = DateTime.UtcNow
    };

    _db.Ratings.Add(rating);
    UpdateDriverAverage(booking.Ride.DriverID);  // Recalculate driver's average rating
    _db.SaveChanges();

    TempData["Message"] = "Thanks for rating your driver!";
    return RedirectToAction("Dashboard");
}
```
**Purpose**: Allows passenger to rate a driver after completed ride.

### Helper Methods (Lines 227-321)

#### GetCurrentPassenger (Lines 229-246)
```csharp
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
```
**Purpose**: Gets currently logged-in passenger with authentication check.

#### ResolveStop (Lines 248-285)
```csharp
private Stop ResolveStop(Guid routeId, Guid? stopId, string stopName)
{
    Stop stop = null;
    if (stopId.HasValue)  // If stop ID provided, try to find it
    {
        stop = _db.Stops.FirstOrDefault(s => s.StopID == stopId.Value && s.RouteID == routeId);
    }

    if (stop != null)  // Return if found
    {
        return stop;
    }

    if (string.IsNullOrWhiteSpace(stopName))  // If no name provided, return null
    {
        return null;
    }

    // Try to find by name
    stop = _db.Stops.FirstOrDefault(s => s.RouteID == routeId && s.LocationName.Equals(stopName.Trim(), StringComparison.OrdinalIgnoreCase));
    if (stop != null)
    {
        return stop;
    }

    // Create new stop if not found
    stop = new Stop
    {
        StopID = Guid.NewGuid(),
        RouteID = routeId,
        LocationName = stopName.Trim(),
        Latitude = 0,
        Longitude = 0,
        OrderNo = _db.Stops.Count(s => s.RouteID == routeId) + 1  // Set order as last stop
    };

    _db.Stops.Add(stop);
    _db.SaveChanges();
    return stop;
}
```
**Purpose**: Finds existing stop or creates new one if passenger specifies custom location.

#### UpdateDriverAverage (Lines 287-305)
```csharp
private void UpdateDriverAverage(Guid driverId)
{
    var ratings = _db.Ratings.Where(r => r.DriverID == driverId).Select(r => r.Stars).ToList();  // Get all ratings for driver
    var driver = _db.Drivers.FirstOrDefault(d => d.DriverID == driverId);
    if (driver == null)
    {
        return;
    }

    if (ratings.Count == 0)  // If no ratings
    {
        driver.RatingAvg = 0;
    }
    else
    {
        var avg = ratings.Select(r => (decimal)r).Average();  // Calculate average
        driver.RatingAvg = Math.Round(avg, 2);  // Round to 2 decimal places
    }
}
```
**Purpose**: Recalculates and updates driver's average rating.

#### EnsureCoreStatuses (Lines 307-319)
```csharp
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
```
**Purpose**: Ensures all required status types exist in database.

---

## Summary

This carpooling application has 5 controllers:

1. **AccountController**: Handles authentication (login/logout) and user registration (passenger/driver)
2. **AdminController**: Manages driver approvals and user bans
3. **DriverController**: Allows drivers to post rides, manage bookings, and track ride status
4. **HomeController**: Simple home page controller
5. **PassengerController**: Allows passengers to browse rides, make bookings, and rate drivers

All controllers use Entity Framework for database access, session management for authentication, and follow MVC patterns with proper separation of concerns.

