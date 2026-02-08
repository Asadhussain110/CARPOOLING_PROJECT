using System;
using System.Linq;
using CARPOOLING_PROJECT.Models;

namespace CARPOOLING_PROJECT.Helpers
{
    public static class StatusHelper
    {
        public static byte EnsureRideStatus(CarpoolMGSEntities1 db, string statusName)
        {
            var status = db.RideStatus.FirstOrDefault(s => s.StatusName.Equals(statusName, StringComparison.OrdinalIgnoreCase));
            if (status != null)
            {
                return status.StatusID;
            }

            status = new RideStatu
            {
                StatusID = GetNextRideStatusId(db),
                StatusName = statusName
            };

            db.RideStatus.Add(status);
            db.SaveChanges();
            return status.StatusID;
        }

        public static byte EnsureBookingStatus(CarpoolMGSEntities1 db, string statusName)
        {
            var status = db.BookingStatus.FirstOrDefault(s => s.StatusName.Equals(statusName, StringComparison.OrdinalIgnoreCase));
            if (status != null)
            {
                return status.StatusID;
            }

            status = new BookingStatu
            {
                StatusID = GetNextBookingStatusId(db),
                StatusName = statusName
            };

            db.BookingStatus.Add(status);
            db.SaveChanges();
            return status.StatusID;
        }

        public static string GetBookingStatusName(CarpoolMGSEntities1 db, byte statusId)
        {
            return db.BookingStatus.FirstOrDefault(s => s.StatusID == statusId)?.StatusName ?? "Unknown";
        }

        public static string GetRideStatusName(CarpoolMGSEntities1 db, byte statusId)
        {
            return db.RideStatus.FirstOrDefault(s => s.StatusID == statusId)?.StatusName ?? "Unknown";
        }

        private static byte GetNextRideStatusId(CarpoolMGSEntities1 db)
        {
            return (byte)(db.RideStatus.Any() ? db.RideStatus.Max(r => r.StatusID) + 1 : 1);
        }

        private static byte GetNextBookingStatusId(CarpoolMGSEntities1 db)
        {
            return (byte)(db.BookingStatus.Any() ? db.BookingStatus.Max(r => r.StatusID) + 1 : 1);
        }
    }
}

