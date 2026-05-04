# Software Requirements Specification (SRS) Report
## Project: PakiCarpool System

---

## 1. Introduction

### 1.1 Purpose
The purpose of this document is to provide a detailed description of the PakiCarpool system. It will outline the system's purpose, scope, functional and non-functional requirements, and design constraints. This document is intended for project stakeholders, developers, and testers to ensure a common understanding of the system's capabilities.

### 1.2 Scope
PakiCarpool is a web-based application designed to facilitate carpooling services. The system allows drivers to post rides and passengers to book them. Key features include:
- User registration and role-based login (Admin, Driver, Passenger).
- Secure authentication using SHA256 password hashing.
- Ride management (Posting, Canceling, Status Tracking).
- Booking management (Requesting, Accepting, Rejecting, Tracking).
- Admin oversight for user verification and system health.
- Automated audit logging for security and accountability.

### 1.3 Definitions, Acronyms, and Abbreviations
- **SRS**: Software Requirements Specification
- **MVC**: Model-View-Controller
- **EF**: Entity Framework
- **CSRF**: Cross-Site Request Forgery
- **ID**: Identifier
- **CRUD**: Create, Read, Update, Delete

### 1.4 References
- IEEE Std 830-1998, IEEE Recommended Practice for Software Requirements Specifications.
- Project Source Code and Database Schema (Entity Framework Models).

### 1.5 Overview
The rest of this document contains an overall description of the PakiCarpool system and a set of specific requirements for its implementation.

---

## 2. Overall Description

### 2.1 Product Perspective
PakiCarpool is a standalone web application built using the ASP.NET MVC framework. It interacts with a SQL Server database via Entity Framework for data persistence. The system provides a centralized platform for commuters to share rides, reducing travel costs and traffic congestion.

### 2.2 Product Functions
- **User Management**: Registration, Login, Profile Management, and Account Security.
- **Ride Posting**: Drivers can create rides by specifying routes, stops, start times, and available seats.
- **Ride Discovery**: Passengers can search and view available rides based on their requirements.
- **Booking System**: Passengers can request seats on available rides; drivers can accept or reject these requests.
- **Ride Tracking**: Tracking the status of rides from 'Active' to 'InProgress' and finally 'Completed'.
- **Admin Dashboard**: Monitoring all system activity, approving new drivers, and managing user access (banning/unbanning).

### 2.3 User Characteristics
- **Passengers**: Users looking for rides. They need a simple interface to find and book rides.
- **Drivers**: Users offering rides. They require tools to manage their schedules and passengers.
- **Administrators**: System overseers who manage the platform's integrity and user base.

### 2.4 Constraints
- The system must be accessible via modern web browsers.
- Development must adhere to the MVC architectural pattern.
- Database operations must use Entity Framework for maintainability.

### 2.5 Assumptions and Dependencies
- Users have access to the internet and a web browser.
- The SQL Server database is available and properly configured.
- SMTP service is available for email notifications (if implemented).

---

## 3. Specific Requirements

### 3.1 External Interface Requirements
- **User Interfaces**: Responsive web interface using HTML5, CSS3, and JavaScript.
- **Software Interfaces**: ASP.NET MVC 5, Entity Framework 6, SQL Server.
- **Communication Interfaces**: HTTP/HTTPS protocols for client-server communication.

### 3.2 Functional Requirements

#### 3.2.1 Authentication & Authorization
- **R1.1**: The system shall allow users to register as either a Driver or a Passenger.
- **R1.2**: The system shall require users to log in with a valid email and password.
- **R1.3**: The system shall use SHA256 hashing for password storage.
- **R1.4**: The system shall implement Role-Based Access Control (RBAC) for Admins, Drivers, and Passengers.

#### 3.2.2 Driver Module
- **R2.1**: Drivers shall be able to post rides with route details and seat capacity.
- **R2.2**: Drivers shall be able to accept or reject booking requests from passengers.
- **R2.3**: Drivers shall be able to cancel a ride, which automatically cancels all associated bookings.
- **R2.4**: Drivers shall be able to mark a ride as 'Reached' or 'Completed'.

#### 3.2.3 Passenger Module
- **R3.1**: Passengers shall be able to search for available rides.
- **R3.2**: Passengers shall be able to request a booking for a specific ride.
- **R3.3**: Passengers shall be able to view their booking history and current ride status.

#### 3.2.4 Administrative Module
- **R4.1**: Admins shall be able to view a dashboard with system-wide statistics.
- **R4.2**: Admins shall be able to approve or reject new Driver registrations.
- **R4.3**: Admins shall be able to ban or unban any user account.

### 3.3 Non-Functional Requirements

#### 3.3.1 Security
- Implement CSRF protection using Anti-Forgery Tokens.
- Secure session management.
- Data validation on both client and server sides.

#### 3.3.2 Performance
- Page load times should be under 2 seconds for standard operations.
- The system should support concurrent ride postings and bookings without data corruption.

#### 3.3.3 Availability
- The system should aim for 99.9% uptime.

#### 3.3.4 Maintainability
- Code should follow clean coding standards and be well-documented.
- Use of MVC pattern ensures separation of concerns.

---

## 4. Logical Database Requirements
The database schema includes the following primary entities:
- **Users**: Core user data and credentials.
- **Drivers/Passengers**: Role-specific profiles linked to Users.
- **Rides**: Details of carpooling offerings.
- **Bookings**: Records of passenger requests and their statuses.
- **Routes & Stops**: Geographic pathing for rides.
- **Ratings**: Feedback system for users.
- **AuditLogs**: Tracking of sensitive system actions.

---

## 5. Design Constraints
- Technology Stack: C#, ASP.NET MVC, Entity Framework.
- Design Pattern: MVC (Model-View-Controller).
- Security: SHA256 hashing, Anti-Forgery Tokens.

---
*End of Document*
