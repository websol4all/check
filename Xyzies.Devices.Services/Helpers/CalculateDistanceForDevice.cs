using System;

namespace Xyzies.Devices.Services.Helpers
{
    public static class CalculateDistanceForDevice
    {
        public static bool DeviceIsInLocation(double latBase, double lonBase, double latCur, double lonCur, double radius)
        {
            var R = 6371; // km
            var dLat = ToRadian(latCur - latBase);
            var dLon = ToRadian(lonCur - lonBase);
            var lat1 = ToRadian(latBase);
            var lat2 = ToRadian(latCur);

            var a = Math.Sin(dLat/2) * Math.Sin(dLat/2) +
              Math.Sin(dLon/2) * Math.Sin(dLon/2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c;
            return (d - radius / 1000) <= 0;
        }

        public static double ToRadian(double value)
        {
            return value * Math.PI / 180;
        }
    }
}
