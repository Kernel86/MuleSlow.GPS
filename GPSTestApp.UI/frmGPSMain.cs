/**************************************************************************
 *
 * GPS Test App
 * [frmGPSMain.c]
 * Copyright (C) 2012 Shawn Novak - Kernel86@muleslow.net
 *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Device.Location;

using GPSTestApp.Location.Services;
using GPSTestApp.GPS;

namespace GPSTestApp.UI
{
    public partial class frmGPSMain : Form
    {
        CLocation _location; // Location Services
        CGPS _gps;

        delegate void SetTextCallback(string text);

        public frmGPSMain()
        {
            InitializeComponent();
        }

        private void frmGPSMain_Load(object sender, EventArgs e)
        {
            if (_gps == null)
                _gps = new CGPS("COM11", 4800);

            if (_gps.IsGPSEnabled())
            {
                chkGPS.Text = "On";
                chkGPS.Checked = true;
            }
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            // Configure Location Class
            if (_location == null)
            {
                _location = new CLocation(GeoPositionAccuracy.High);
                _location.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(_location_StatusChanged);
                _location.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(_location_PositionChanged);
            }

            // Start _location
            _location.Start();

            btnGPSStart.Enabled = false;
            btnGPSStop.Enabled = false;
        }

        // Handle location services status changes
        private void _location_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case GeoPositionStatus.Disabled:
                    lblStatus.Text = "Location Service is not enabled on the device";
                    break;
                case GeoPositionStatus.NoData:
                    lblStatus.Text = "The Location Service is working, but it cannot get location data.";
                    break;
                default:
                    lblStatus.Text = String.Empty;
                    break;
            }
        }

        // Handle location change updates
        private void _location_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            lblStatus.Text = String.Empty;

            // Check if Location Services have located us
            if (e.Position.Location.IsUnknown)
                lblStatus.Text = "Please wait while your position is determined....";
            else
            {
                // Display values
                lblTime.Text = e.Position.Timestamp.ToString();
                lblLat.Text = _location.DecimalToDegree(e.Position.Location.Latitude, Plane.Horizontal);
                lblLong.Text = _location.DecimalToDegree(e.Position.Location.Longitude, Plane.Vertical);

                if (e.Position.Location.VerticalAccuracy.ToString() == "NaN")
                    lblAccuracy.Text = "0 m";
                else
                    lblAccuracy.Text = e.Position.Location.HorizontalAccuracy.ToString() + " m";

                if (e.Position.Location.Altitude.ToString() == "NaN")
                    lblAlt.Text = "0 m";
                else
                    lblAlt.Text = e.Position.Location.Altitude.ToString() + " m";

                if (e.Position.Location.Speed.ToString() == "NaN")
                    lblSpeed.Text = "0 m/s";
                else
                    lblSpeed.Text = e.Position.Location.Speed.ToString() + " m/s";

                if (e.Position.Location.Course.ToString() == "NaN")
                    lblCourse.Text = "0°";
                else
                    lblCourse.Text = e.Position.Location.Course.ToString() + "°";
            }
        }

        // Cleanup on close
        private void frmGPSMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_location != null)
                _location.Dispose();
            if (_gps != null)
                _gps.Dispose();
        }

        private void btnGPSStart_Click(object sender, EventArgs e)
        {
            try
            {
                    btnGo.Enabled = false;
                    _gps.Start();
                    _gps.DataReceived += _gps_DataReceived;

            }
            catch (Exception ex)
            {
                lblStatus.Text = ex.Message;
            }
        }

        private void _gps_DataReceived(object sender, string s)
        {
            /*  GGA          Global Positioning System Fix Data
             *  1 123519       Fix taken at 12:35:19 UTC
             *  2 4807.038,N   Latitude 48 deg 07.038' N
             *  4 01131.000,E  Longitude 11 deg 31.000' E
             *  6 1            Fix quality: 0 = invalid
             *                            1 = GPS fix (SPS)
             *                            2 = DGPS fix
             *                            3 = PPS fix
			 *                            4 = Real Time Kinematic
			 *                            5 = Float RTK
             *                            6 = estimated (dead reckoning) (2.3 feature)
			 *                            7 = Manual input mode
			 *                            8 = Simulation mode
             *  7 08           Number of satellites being tracked
             *  8 0.9          Horizontal dilution of position
             *  9 545.4,M      Altitude, Meters, above mean sea level
             * 10 46.9,M       Height of geoid (mean sea level) above WGS84 ellipsoid
             * 11 (empty field) time in seconds since last DGPS update
             * 12 (empty field) DGPS station ID number
             * 12 *47          the checksum data, always begins with *
             * 
             *  RMC          Recommended Minimum sentence C
             *  1 123519       Fix taken at 12:35:19 UTC
             *  2 A            Status A=active or V=Void.
             *  3 4807.038,N   Latitude 48 deg 07.038' N
             *  5 01131.000,E  Longitude 11 deg 31.000' E
             *  7 022.4        Speed over the ground in knots
             *  8 084.4        Track angle in degrees True
             *  9 230394       Date - 23rd of March 1994
             * 10 003.1,W      Magnetic Variation
             *   *6A          The checksum data, always begins with *
             */
            string[] sSentences = s.Split('$');

            foreach (string sentence in sSentences)
            {
                string checksum;
                int checksum2 = 0;
                if (sentence.LastIndexOf('*') != -1)
                {
                    checksum = sentence.Substring(sentence.LastIndexOf('*') + 1, 2);
                    string sentence2 = sentence.Substring(0, sentence.LastIndexOf('*'));

                    int i = 0;
                    while (i <= sentence2.Length - 2)
                    {
                        if (i == 0)
                            checksum2 = (int)sentence2.ToCharArray()[i] ^ (int)sentence2.ToCharArray()[i + 1];
                        else
                            checksum2 = checksum2 ^ sentence2.ToCharArray()[i + 1];
                        i++;
                    }

                    if (checksum == checksum2.ToString("X"))
                    {
                        if (sentence2 != String.Empty)
                            this.AddItem(sentence2);

                        string[] sFields = sentence2.Split(',');

                        if (sFields[0] == "GPGGA")
                        {
                            if (sFields[2].Length > 0 && sFields[4].Length > 0)
                            {
                                SetTime(sFields[1].Substring(0, 2) + ":" + sFields[1].Substring(2, 2) + ":" + sFields[1].Substring(4, 2) + " UTC");

                                int Lat1 = (int)(double.Parse(sFields[2]) / 100);
                                double Lat2 = double.Parse(sFields[2]) - (Lat1 * 100);
                                SetLatitude(sFields[3] + " " + Lat1.ToString() + "°" + Lat2.ToString("N3") + "'");
                                int Long1 = (int)(double.Parse(sFields[4]) / 100);
                                double Long2 = double.Parse(sFields[4]) - (Long1 * 100);
                                SetLongitude(sFields[5] + " " + Long1.ToString() + "°" + Long2.ToString("N3") + "'");
                                SetAltitude((double.Parse(sFields[9]) * 3.28084).ToString("N2") + " ft");
                                SetAccuracy(sFields[8]);
                            }
                            else
                                lblStatus.Text = "No Fix";
                        }
                        else if (sFields[0] == "GPGSA")
                        {
                            // SetAccuracy((double.Parse(sFields[17].Substring(0,sFields[17].IndexOf('*'))) * 3.28084).ToString("N2") + " ft");
                            // SetAccuracy(sFields[16]);
                        }
                        else if (sFields[0] == "GPRMC")
                        {
                           // SetSpeed((double.Parse(sFields[7]) * 1.15078 / 60 / 24).ToString("N2") + " mph");
                           // SetCourse(sFields[8] + "°");
                        }
                    }
                }
            }
            //this.AddItem(s);
        }

        private void AddItem(string text)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (this.lstGPS.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(AddItem);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.lstGPS.Items.Insert(0, text);
            }
        }

        private void SetTime(string text)
        {
            if (this.lstGPS.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetTime);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.lblTime.Text = text;
            }
        }

        private void SetLatitude(string text)
        {
            if (this.lstGPS.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetLatitude);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.lblLat.Text = text;
            }
        }

        private void SetLongitude(string text)
        {
            if (this.lstGPS.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetLongitude);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.lblLong.Text = text;
            }
        }

        private void SetAltitude(string text)
        {
            if (this.lstGPS.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetAltitude);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.lblAlt.Text = text;
            }
        }

        private void SetAccuracy(string text)
        {
            if (this.lstGPS.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetAccuracy);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.lblAccuracy.Text = text;
            }
        }

        private void SetSpeed(string text)
        {
            if (this.lstGPS.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetSpeed);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.lblSpeed.Text = text;
            }
        }

        private void SetCourse(string text)
        {
            if (this.lstGPS.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetCourse);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.lblCourse.Text = text;
            }
        }

        private void btnGPSStop_Click(object sender, EventArgs e)
        {
            _gps.Dispose();
            _gps = null;
            btnGo.Enabled = true;
        }

        private void btnGPSEnable_Click(object sender, EventArgs e)
        {
           // _gps.Enable();
        }

        private void _gpsSend(object sender, string s)
        {
            MessageBox.Show(s);
        }

        private void chkGPS_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void chkGPS_Click(object sender, EventArgs e)
        {
            if (chkGPS.Checked == true)
            {
                _gps.Disable();
                chkGPS.Text = "Off";
                chkGPS.Checked = false;
            }
            else
            {
                _gps.Enable();
                chkGPS.Text = "On";
                chkGPS.Checked = true;
            }
        }
    }
}
