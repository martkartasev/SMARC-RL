using System.Globalization;
using CsvHelper.Configuration;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

namespace BagReplay
{
    public class BagData
    {
        public float Vbs { get; set; }
        public float Lcg { get; set; }
        public int Thruster1RPM { get; set; }
        public int Thruster2RPM { get; set; }
        public float ThrusterHorizontalRad { get; set; }
        public float ThrusterVerticalRad { get; set; }
        public Vector3 PrevPositionRos { get; set; }
        public Quaternion PrevOrientationRos { get; set; }
        public Vector3 PositionRos { get; set; }
        public Quaternion OrientationRos { get; set; }
        public Vector3 LinearVelocityRos { get; set; }
        public Vector3 AngularVelocityRos { get; set; }

        public BagCsvRow ToCsv()
        {
            var bagCsvRow = new BagCsvRow();
            bagCsvRow.Vbs = Vbs / 100f;
            bagCsvRow.Lcg = Lcg / 100f;
            bagCsvRow.Thruster1RPM = Thruster1RPM / 1000f;
            bagCsvRow.Thruster2RPM = Thruster2RPM / 1000f;
            bagCsvRow.ThrusterHorizontalRad = ThrusterHorizontalRad / 0.2f;
            bagCsvRow.ThrusterVerticalRad = ThrusterVerticalRad / 0.2f;

            bagCsvRow.LinearVelocity = NED.ConvertToRUF(LinearVelocityRos);
            bagCsvRow.AngularVelocity = FRD.ConvertAngularVelocityToRUF(AngularVelocityRos);

            return bagCsvRow;
        }
    }

    public class BagCsvRow
    {
        public Vector3 LinearVelocity { get; set; }
        public Vector3 AngularVelocity { get; set; }
        public float Vbs { get; set; }
        public float Lcg { get; set; }
        public float Thruster1RPM { get; set; }
        public float Thruster2RPM { get; set; }
        public float ThrusterHorizontalRad { get; set; }
        public float ThrusterVerticalRad { get; set; }
    }

    public sealed class BagCsvRowMap : ClassMap<BagCsvRow>
    {
        public BagCsvRowMap()
        {
            Map(m => m.LinearVelocity).Ignore();
            Map(m => m.AngularVelocity).Ignore();
            
            Map(m => m.LinearVelocity.x).Name("LinVelX");
            Map(m => m.LinearVelocity.y).Name("LinVelY");
            Map(m => m.LinearVelocity.z).Name("LinVelZ");

            Map(m => m.AngularVelocity.x).Name("AngVelX");
            Map(m => m.AngularVelocity.y).Name("AngVelY");
            Map(m => m.AngularVelocity.z).Name("AngVelZ");

            Map(m => m.Vbs);
            Map(m => m.Lcg);
            Map(m => m.Thruster1RPM);
            Map(m => m.Thruster2RPM);
            Map(m => m.ThrusterHorizontalRad);
            Map(m => m.ThrusterVerticalRad);
        }
    }
}