# PakiCarpool - Comprehensive Carpooling Management System

![PakiCarpool Logo](https://img.shields.io/badge/ASP.NET-MVC%205-blue.svg)
![Entity Framework](https://img.shields.io/badge/EF-Database%20First-green.svg)
![Status](https://img.shields.io/badge/Status-Development-orange.svg)

**PakiCarpool** is a robust, multi-role web application designed to facilitate ride-sharing and carpooling. Built using ASP.NET MVC 5 and Entity Framework, the system connects drivers with passengers, optimizing travel costs and reducing traffic congestion.

---

## 🚀 Key Features

### 👤 Role-Based Access Control
The system supports three distinct user roles, each with a dedicated dashboard and specific permissions:
- **Admin**: Oversees the entire system, manages user accounts (banning/unbanning), and approves or rejects driver applications.
- **Driver**: Can post ride offers, manage routes/stops, accept or reject booking requests, confirm payments, and track their active rides.
- **Passenger**: Can search for available rides, book seats with custom pickup/drop-off points, rate drivers, and manage their trip history.

### 🚗 Ride Lifecycle Management
- **Dynamic Routing & Stops**: Drivers define routes with multiple stops. Passengers can select existing stops or suggest custom locations.
- **State-based Booking**: Requests follow a lifecycle: `Pending` → `Accepted` → `InProgress` → `Completed`.
- **Seat Management**: Real-time tracking ensures seats are released back into the pool upon cancellation or completion (after payment).
- **Ride Cancellation**: Drivers can cancel rides, automatically notifying and cancelling all associated passenger bookings.

### 💰 Payment & Ratings
- **Cash Payment Flow**: Supports cash-on-delivery style payments with driver confirmation.
- **Feedback Loop**: Passengers can rate drivers (1-5 stars) and provide feedback after completed trips, which updates the driver's public average rating.

### 🛡️ Security & Administration
- **Secure Authentication**: Password hashing using SHA256 ensures user data protection.
- **Driver Verification**: Drivers must be approved by an administrator before they can post rides.
- **Account Moderation**: Admins have the authority to ban/unban users to maintain community standards.
- **CSRF Protection**: Implemented across all critical forms using Anti-Forgery Tokens.

---

## 🛠️ Tech Stack

- **Framework**: ASP.NET MVC 5 (C#)
- **Data Access**: Entity Framework 6 (Database First / EDMX)
- **Database**: SQL Server
- **Frontend**: Razor View Engine, HTML5, Vanilla CSS, JavaScript/jQuery
- **Security**: SHA256 Hashing, Session-based Authentication

---

## 📂 Project Structure

```text
CARPOOLING_PROJECT/
├── App_Data/               # Database files (if local)
├── App_Start/              # Route & Bundle configurations
├── Controllers/            # Business logic (Account, Admin, Driver, Passenger)
├── Models/                 # EF Data Models (.edmx) and POCO classes
├── ViewModels/             # Data Transfer Objects for Views
├── Views/                  # UI Components (Razor .cshtml files)
├── Content/                # CSS and Static Assets
├── Scripts/                # Client-side JavaScript
└── Web.config              # Application configuration & connection strings
```

---

## ⚙️ Installation & Setup

### Prerequisites
- Visual Studio 2019/2022 (with ASP.NET and web development workload)
- SQL Server (LocalDB or Express)
- .NET Framework 4.7.2+

### Step-by-Step Guide

1. **Clone the Repository**
   ```bash
   git clone <repository-url>
   ```

2. **Database Configuration**
   - Open `Web.config`.
   - Update the `connectionStrings` section to point to your local SQL Server instance:
     ```xml
     <add name="CarpoolMGSEntities1" connectionString="data source=YOUR_SERVER;initial catalog=CarpoolMGS;integrated security=True;..." />
     ```

3. **Initialize Database**
   - The system uses **Seed Data** logic. On the first run, it will automatically create:
     - Admin Role
     - Driver Role
     - Passenger Role
     - A default Admin account (`admin@carpool.com` / `Admin@123`)

4. **Run the Project**
   - Open the `.sln` file in Visual Studio.
   - Press `F5` to build and run the application.

---

## 📖 Usage Guide

### Getting Started
- **Admin**: Log in using the default credentials to access the admin dashboard.
- **Driver**: Register as a driver and wait for admin approval. Once approved, click "Post Ride" to offer a trip.
- **Passenger**: Register as a passenger to browse the dashboard and "Quick Book" available rides.

### Common Operations
- **Booking a Ride**: Passengers request seats; Drivers must manually **Accept** the booking from their dashboard to confirm the seat.
- **Managing Rides**: Drivers can view their "Active Rides" and "Pending Requests" separately for better organization.

---

## ⚠️ Important Note (Developer Context)
This project contains certain "intentional" patterns in the `PassengerController` designed for educational purposes (e.g., God Objects, Static state tracking). When contributing or refactoring, refer to the `CONTROLLER_EXPLANATIONS.md` for a detailed breakdown of the logic and identified anti-patterns.

---

## 📄 License
This project is licensed under the MIT License - see the LICENSE file for details.

---
*Developed as part of the Project Portfolio.*
