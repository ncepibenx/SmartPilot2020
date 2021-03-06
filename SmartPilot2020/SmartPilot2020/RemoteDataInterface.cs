﻿using SmartPilot2020;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPilot2020
{
    public class RemoteDataInterface
    {
        private SmartPilot2020 main;
        private SerialPort SerialPort;

        public int tx = 0;
        public int rx = 0;
        public bool PacketOutputState = false;

        public RemoteDataInterface(SmartPilot2020 main)
        {
            this.main = main;

            main.SetPacketOutputState(PacketOutputState);

            this.SerialPort = new SerialPort("COM13", 115200);
            this.SerialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
        }

        public void SendControlPacket(RemoteControlPacket remotePacket)
        {
            String encoded = remotePacket.Encode();

            if(SerialPort.IsOpen && PacketOutputState)
            {
                this.SerialPort.WriteLine(encoded);

                tx++;
                main.SetTraffic("RX " + rx + " / TX " + tx);
            }
        }

        public void SendAltitudeReferencePacket(int BaroReference)
        {
            String encoded = "7;" + BaroReference;

            if (SerialPort.IsOpen && PacketOutputState)
            {
                this.SerialPort.WriteLine(encoded);

                tx++;
                main.SetTraffic("RX " + rx + " / TX " + tx);
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            String input = SerialPort.ReadLine();

            // Try parsing the packet id
            string[] inputSplit;
            int packetId = -1;

            try
            {
                inputSplit = input.Split(';');
                packetId = Int32.Parse(inputSplit[0]);

                switch (packetId)
                {
                    case 0: // not implemented as incoming packet
                        break;
                    case 1: // not implemented as incoming packet
                        break;
                    case 2: // RemoteTelemetryPacket
                        main.FlightHandler.CurrentPitchAngle = Int32.Parse(inputSplit[1]);
                        main.FlightHandler.CurrentRollAngle = Int32.Parse(inputSplit[2]);
                        main.FlightHandler.CurrentHeading = Int32.Parse(inputSplit[3]);
                        main.FlightHandler.CurrentSpeed = Int32.Parse(inputSplit[4]);
                        main.FlightHandler.CurrentAltitude = Int32.Parse(inputSplit[5]);
                        break;
                    case 3: // RemotePositionPacket
                        main.FlightHandler.CurrentGpsData.Latitude = Double.Parse(inputSplit[1].Replace('.', ','));
                        main.FlightHandler.CurrentGpsData.Longitude = Double.Parse(inputSplit[2].Replace('.', ','));
                        main.FlightHandler.CurrentGpsData.Altitude = Int32.Parse(inputSplit[3]);
                        break;
                    case 4: // RemoteEnvironmentPacket
                        main.FlightHandler.CurrentAircraftTemperature = Int32.Parse(inputSplit[1]);
                        main.FlightHandler.CurrentAircraftHumidity = Int32.Parse(inputSplit[2]);
                        main.FlightHandler.CurrentAircraftPressure = Int32.Parse(inputSplit[3]);
                        break;
                    case 5: // StationaryEnvironmentPacket
                        main.FlightHandler.CurrentStationaryTemperature = Double.Parse(inputSplit[1]);
                        main.FlightHandler.CurrentStationaryHumidity = Double.Parse(inputSplit[2]);
                        main.FlightHandler.CurrentStationaryPressure = Int32.Parse(inputSplit[3]);
                        break;
                    case 6: // RemoteSignalInformationPacket
                        main.FlightHandler.UsedRadioChannel = Int32.Parse(inputSplit[1]);
                        main.FlightHandler.CarrierTest = Int32.Parse(inputSplit[2]) == 0 ? false : true;
                        main.FlightHandler.RpdTest = Int32.Parse(inputSplit[3]) == 0 ? false : true;
                        break;
                }

            } catch(Exception)
            {
                main.MonitoringHandler.AddMessageTimed("FLT MSG RECV", System.Drawing.Color.Orange, 1000);
                return;
            }

            rx++;
            main.SetTraffic("RX " + rx + " / TX " + tx);
        }

        public void Connect()
        {
            try
            {
                this.SerialPort.Open();
                main.SetComPortState(true);
            }
            catch (Exception)
            {
                main.SetComPortState(false);
            }
        }

        public void Disconnect()
        {
            try
            {
                if(!this.SerialPort.IsOpen)
                {
                    main.SetComPortState(false);
                    return;
                }

                this.SerialPort.Close();
                main.SetComPortState(false);
            }
            catch (Exception)
            {
                main.SetComPortState(false);
            }
        }

    }
}
