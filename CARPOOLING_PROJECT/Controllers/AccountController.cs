using System;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using CARPOOLING_PROJECT.Models;
using CARPOOLING_PROJECT.ViewModels;

namespace CARPOOLING_PROJECT.Controllers
{
    public class AccountController : Controller
    {
        private readonly CarpoolMGSEntities1 _db = new CarpoolMGSEntities1();
        private const string AdminEmail = "admin@carpool.com";
        private const string AdminPassword = "Admin@123";

        public ActionResult Login()
        {
            EnsureSeedData();
            return View(new LoginViewModel());
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            EnsureSeedData();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.SelectedRole))
            {
                ModelState.AddModelError("", "Please choose whether you are an Admin, Driver or Passenger.");
                return View(model);
            }

            var role = FindRole(model.SelectedRole);
            if (role == null)
            {
                ModelState.AddModelError("", "Selected role is not configured yet.");
                return View(model);
            }

            var email = model.Email.Trim().ToLowerInvariant();
            var user = _db.Users
                .Include(u => u.Drivers)
                .Include(u => u.Passengers)
                .FirstOrDefault(u => u.Email.ToLower() == email && u.RoleID == role.RoleID);

            if (user == null || !ComparePassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (user.IsActive == false)
            {
                ModelState.AddModelError("", "Your account is currently banned. Please contact support.");
                return View(model);
            }

            Session["UserId"] = user.UserID;
            Session["Role"] = role.RoleName;
            Session["DisplayName"] = user.Name;

            if (role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            if (role.RoleName.Equals("Driver", StringComparison.OrdinalIgnoreCase))
            {
                if (!user.Drivers.Any())
                {
                    ModelState.AddModelError("", "Driver profile is missing for this user.");
                    Session.Clear();
                    return View(model);
                }

                return RedirectToAction("Dashboard", "Driver");
            }

            if (role.RoleName.Equals("Passenger", StringComparison.OrdinalIgnoreCase))
            {
                if (!user.Passengers.Any())
                {
                    ModelState.AddModelError("", "Passenger profile is missing for this user.");
                    Session.Clear();
                    return View(model);
                }

                return RedirectToAction("Dashboard", "Passenger");
            }

            Session.Clear();
            ModelState.AddModelError("", "Role not recognized.");
            return View(model);
        }

        public ActionResult RegisterPassenger()
        {
            return View(new RegisterPassengerViewModel());
        }

        [HttpPost]
        public ActionResult RegisterPassenger(RegisterPassengerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedEmail = model.Email.Trim().ToLowerInvariant();
            if (_db.Users.Any(u => u.Email.ToLower() == normalizedEmail))
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            var role = EnsureRole("Passenger");
            var user = new User
            {
                UserID = Guid.NewGuid(),
                Name = model.Name.Trim(),
                Email = normalizedEmail,
                Phone = model.Phone,
                PasswordHash = StorePasswordAsBytes(model.Password),
                RoleID = role.RoleID,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var passenger = new Passenger
            {
                PassengerID = Guid.NewGuid(),
                UserID = user.UserID,
                CreatedAt = DateTime.UtcNow,
                RatingAvg = 0
            };

            _db.Users.Add(user);
            _db.Passengers.Add(passenger);
            _db.SaveChanges();

            TempData["Message"] = "Passenger registered successfully. You can now log in.";
            return RedirectToAction("Login");
        }

        public ActionResult RegisterDriver()
        {
            return View(new RegisterDriverViewModel());
        }

        [HttpPost]
        public ActionResult RegisterDriver(RegisterDriverViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedEmail = model.Email.Trim().ToLowerInvariant();
            if (_db.Users.Any(u => u.Email.ToLower() == normalizedEmail))
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            var role = EnsureRole("Driver");
            var user = new User
            {
                UserID = Guid.NewGuid(),
                Name = model.Name.Trim(),
                Email = normalizedEmail,
                Phone = model.Phone,
                PasswordHash = StorePasswordAsBytes(model.Password),
                RoleID = role.RoleID,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var driver = new Driver
            {
                DriverID = Guid.NewGuid(),
                UserID = user.UserID,
                LicenseNo = model.LicenseNo,
                IsApproved = false,
                CreatedAt = DateTime.UtcNow,
                RatingAvg = 0
            };

            _db.Users.Add(user);
            _db.Drivers.Add(driver);
            _db.SaveChanges();

            TempData["Message"] = "Driver registered successfully. Please wait for admin approval.";
            return RedirectToAction("Login");
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
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

        private void EnsureSeedData()
        {
            var adminRole = EnsureRole("Admin");
            EnsureRole("Driver");
            EnsureRole("Passenger");

            var normalizedAdminEmail = AdminEmail.ToLower();
            var adminUser = _db.Users.FirstOrDefault(u => u.Email.ToLower() == normalizedAdminEmail);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserID = Guid.NewGuid(),
                    Name = "System Admin",
                    Email = AdminEmail,
                    Phone = "000-0000000",
                    PasswordHash = StorePasswordAsBytes(AdminPassword),
                    RoleID = adminRole.RoleID,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _db.Users.Add(adminUser);
                _db.SaveChanges();
            }
            else
            {
                adminUser.PasswordHash = StorePasswordAsBytes(AdminPassword);
                adminUser.RoleID = adminRole.RoleID;
                adminUser.IsActive = true;
                _db.SaveChanges();
            }
        }

        private Role EnsureRole(string roleName)
        {
            var role = FindRole(roleName);
            if (role != null)
            {
                return role;
            }

            role = new Role
            {
                RoleID = (byte)(_db.Roles.Any() ? _db.Roles.Max(r => r.RoleID) + 1 : 1),
                RoleName = roleName
            };

            _db.Roles.Add(role);
            _db.SaveChanges();
            return role;
        }

        private Role FindRole(string roleName)
        {
            var normalized = roleName?.Trim();
            return _db.Roles.FirstOrDefault(r => r.RoleName.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        }

        private static byte[] StorePasswordAsBytes(string password)
        {
            // Store password as plain text bytes (no hashing)
            return Encoding.UTF8.GetBytes(password ?? string.Empty);
        }


        private static bool ComparePassword(string password, byte[] storedPasswordBytes)
        {
            if (storedPasswordBytes == null)
            {
                return false;
            }

            // Convert stored bytes back to string and compare directly (plain text comparison)
            var storedPassword = Encoding.UTF8.GetString(storedPasswordBytes);
            return storedPassword == password;
        }

        #endregion
    }
}

