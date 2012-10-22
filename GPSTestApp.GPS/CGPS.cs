/**************************************************************************
 *
 * GPS Test App
 * [CGPS.c]
 * Copyright (C) 2012 Shawn Novak - Kernel86@muleslow.net
 *
 *************************************************************************/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;

namespace GPSTestApp.GPS
{
    public class CGPS
    {
    // Private Properties
        private static SerialPort _oCOMPort;

        private string _sCOM;
        private int _iBaudRate;

    // Public Properties
        public string COM
        {
            get { return _sCOM; }
            set { _sCOM = value; }
        }

        public int BaudRate
        {
            get { return _iBaudRate; }
            set { _iBaudRate = value; }
        }

    // Constructors
        public CGPS()
        {

        }

        public CGPS(string vsCOM, int viBaudRate)
        {
            _sCOM = vsCOM;
            _iBaudRate = viBaudRate;
        }

    // Events
        public event EventHandler<String> DataReceived;

    // Private Methods

    // Public Methods
        public void Enable()
        {
            if (!IsPortInUse("COM8") && !IsGPSEnabled())
            {
                SerialPort oCOM = new SerialPort("COM8", 9600, Parity.Odd, 8, StopBits.One);

                oCOM.Open();

                oCOM.Write("AT+CFUN=1\r\n");
                oCOM.BaseStream.Flush();

                bool loop = true;
                while (loop)
                {
                    string read = oCOM.ReadLine();
                    if (read == "OK\r")
                    {
                        oCOM.Write("AT*E2GPSCTL=1,1,1\r\n");
                        oCOM.BaseStream.Flush();
                        loop = false;
                    }
                    else if (read == "ERROR\r")
                        throw new Exception("Error starting GPS.");
                }

                oCOM.Close();
                oCOM.Dispose();
                oCOM = null;
            }
        }

        public void Disable()
        {
            if (!IsPortInUse("COM8") && IsGPSEnabled())
            {
                SerialPort oCOM = new SerialPort("COM8", 9600, Parity.Odd, 8, StopBits.One);

                oCOM.Open();

                oCOM.Write("AT+CFUN=4\r\n");
                oCOM.BaseStream.Flush();

                oCOM.Close();
                oCOM.Dispose();
                oCOM = null;
            }
        }

        private bool IsPortInUse(string port)
        {
            try
            {
                SerialPort oCOM = new SerialPort(port);
                oCOM.Open();

                if (oCOM.IsOpen)
                {
                    oCOM.Close();
                    oCOM.Dispose();
                    oCOM = null;
                    return false;
                }
                else
                    return true;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        public bool IsGPSEnabled()
        {
            if (!IsPortInUse("COM8"))
            {
                SerialPort oCOM = new SerialPort("COM8", 9600, Parity.Odd, 8, StopBits.One);
                oCOM.Open();

                oCOM.DiscardInBuffer();
                oCOM.Write("AT+CFUN?\r\n");
                oCOM.BaseStream.Flush();

                oCOM.DiscardOutBuffer();
                string read = oCOM.ReadLine();
                read = oCOM.ReadLine();

                oCOM.Close();
                oCOM.Dispose();
                oCOM = null;

                if (read == "+CFUN: 1\r")
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public void Start()
        {
            if (SerialPort.GetPortNames().Contains(_sCOM))
            {
                if (!IsPortInUse(_sCOM))
                {
                    if (!IsGPSEnabled())
                    {
                        _oCOMPort = new SerialPort(_sCOM, _iBaudRate);
                        _oCOMPort.ReadTimeout = SerialPort.InfiniteTimeout;
                        _oCOMPort.Open();
                        _oCOMPort.DataReceived += _oCOMPort_DataReceived;
                    }
                }
                else
                    throw new Exception(_sCOM + " Port is in use.");
            }
            else
                throw new Exception("Invalid COM Port (" + _sCOM + ").");
        }

        private void _oCOMPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            DataReceived(sender, indata);
        }

        public void Dispose()
        {
            if (IsGPSEnabled())
                if (_oCOMPort != null)
                {
                    _oCOMPort.Close();
                    _oCOMPort.Dispose();
                }
        }
    }
}
