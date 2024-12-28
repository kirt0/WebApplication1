using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Parkingmap.Models
{
    public class Event
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string EnName { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public List<ParkingLot> ParkingLots { get; set; }
    }

    public class ParkingLot
    {
        public string ID { get; set; }
        public string Position { get; set; }
        public string EnPosition { get; set; }
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public int TotalCar { get; set; }
        public int AvailableCar { get; set; }
        public string AvailableCarRGB { get; set; }
        public int TotalMotor { get; set; }
        public int AvailableMotor { get; set; }
        public string AvailableMotorRGB { get; set; }
        public int ShowVauleCar { get; set; }
        public int ShowVauleMotor { get; set; }
        public string Notes { get; set; }
        public string EnNotes { get; set; }
        public string Type { get; set; }
        public DateTime Updatetime { get; set; }
    }
}