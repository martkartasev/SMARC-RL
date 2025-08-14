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

        public BagCsvRow ToCsv(Transform helperTransform)
        {
            var bagCsvRow = new BagCsvRow();
            bagCsvRow.Vbs = Vbs / 100f;
            bagCsvRow.Lcg = Lcg / 100f;
            bagCsvRow.Thruster1RPM = Thruster1RPM / 1000f;
            bagCsvRow.Thruster2RPM = Thruster2RPM / 1000f;
            bagCsvRow.ThrusterHorizontalRad = ThrusterHorizontalRad / 0.13f;
            bagCsvRow.ThrusterVerticalRad = ThrusterVerticalRad / 0.13f;

            bagCsvRow.Orientation = NED.ConvertToRUF(OrientationRos);
            helperTransform.rotation = bagCsvRow.Orientation;

            bagCsvRow.LinearVelocity = helperTransform.InverseTransformVector(NED.ConvertToRUF(LinearVelocityRos));
            bagCsvRow.AngularVelocity = helperTransform.InverseTransformVector(FRD.ConvertAngularVelocityToRUF(AngularVelocityRos)) / 7;

            return bagCsvRow;
        }
    }

    public class BagCsvRow
    {
        public Vector3 LinearVelocity { get; set; }
        public Vector3 AngularVelocity { get; set; }
        public Quaternion Orientation { get; set; }
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
            Map(m => m.Orientation).Ignore();

            Map(m => m.Orientation.x).Name("OrientationX").Index(0);
            Map(m => m.Orientation.y).Name("OrientationY").Index(1);
            Map(m => m.Orientation.z).Name("OrientationZ").Index(2);
            Map(m => m.Orientation.w).Name("OrientationW").Index(3);

            Map(m => m.LinearVelocity.x).Name("LinVelX").Index(4);
            Map(m => m.LinearVelocity.y).Name("LinVelY").Index(5);
            Map(m => m.LinearVelocity.z).Name("LinVelZ").Index(6);

            Map(m => m.AngularVelocity.x).Name("AngVelX").Index(7);
            Map(m => m.AngularVelocity.y).Name("AngVelY").Index(8);
            Map(m => m.AngularVelocity.z).Name("AngVelZ").Index(9);

            Map(m => m.ThrusterHorizontalRad).Index(10);
            Map(m => m.ThrusterVerticalRad).Index(11);
            Map(m => m.Vbs).Index(12);
            Map(m => m.Lcg).Index(13);
            Map(m => m.Thruster1RPM).Index(14);
            Map(m => m.Thruster2RPM).Index(15);
        }
    }
}