/**************************************************************************
 *
 * GPS Test App
 * [CLocation.c]
 * Copyright (C) 2012 Shawn Novak - Kernel86@muleslow.net
 *
 *************************************************************************/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Device.Location;

namespace GPSTestApp.Location.Services
{
    // Public Enums
    // Enum for globe planes
    public enum Plane
    {
        Horizontal,
        Vertical
    }

    public class CLocation
    {
    // Private Properties
        private GeoCoordinateWatcher _watcher;
        private GeoPositionAccuracy _accuracy;

    // Public Properties
        public GeoPositionAccuracy Accuracy
        {
            get { return _accuracy; }
            set { _accuracy = value; }
        }

    // Constructors
        public CLocation()
        {
            _watcher = new GeoCoordinateWatcher();
            _watcher.StatusChanged += _StatusChanged;
            _watcher.PositionChanged += _PositionChanged;
        }

        public CLocation(GeoPositionAccuracy vAccuracy)
        {
            _accuracy = vAccuracy;

            _watcher = new GeoCoordinateWatcher(vAccuracy);
            _watcher.StatusChanged += _StatusChanged;
            _watcher.PositionChanged += _PositionChanged;
        }

    // Events
        // Forward GeoCoordinateWatcher PositionChanged event as our own
        public event EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>> PositionChanged;
        private void _PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            PositionChanged(sender, e);
        }

        // Forward GeoCoordinateWatcher StatusChanged event as our own
        public event EventHandler<GeoPositionStatusChangedEventArgs> StatusChanged;
        private void _StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            StatusChanged(sender, e);
        }

    // Public Methods
        // Start Watcher
        public void Start()
        {
            _watcher.Start();
        }

        public void Dispose()
        {
            _watcher.Dispose();
        }

        // Convert decimal coordinates to formatted degrees
        public string DecimalToDegree(double vdCoordinate, Plane vePlane)
        {
            string pole = String.Empty;

            // Get compass direction
            if (vePlane == Plane.Horizontal)
            {
                // 90(N)-0-90(S)
                if (vdCoordinate > 0)
                    pole = "N";
                else if (vdCoordinate < 0)
                    pole = "S";
            }
            else if (vePlane == Plane.Vertical)
            {
                // 180(E)-0-180(W)
                if (vdCoordinate > 0)
                    pole = "E";
                else if (vdCoordinate < 0)
                    pole = "W";
            }

            // Degrees are the integer portion of the coordinate
            int degree = (int)vdCoordinate;
            // Total seconds are the decimal portion of the coordinate times 3600, the number of seconds in a degree
            double totalseconds;
            if (degree < 0)
                totalseconds = -1 * (vdCoordinate - degree) * 3600;
            else
                totalseconds = (vdCoordinate - degree) * 3600;
            // Minutes
            int minutes = (int)(totalseconds / 60);
            // Seconds
            double seconds = totalseconds - minutes * 60;

            // Return the formatted coordinate
            if (pole != String.Empty)
                return pole + " " + degree.ToString() + "°" + minutes.ToString() + "'" + seconds.ToString("N3") + "\"";
            else
                return degree.ToString() + "°" + minutes.ToString() + "'" + seconds.ToString("N3") + "\"";
        }
    }
}
