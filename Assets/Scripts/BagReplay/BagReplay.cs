using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using RosMessageTypes.Sam;
using RosMessageTypes.Smarc;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using UnityEngine;

namespace BagReplay
{
    public class BagReplay : MonoBehaviour
    {
        [HideInInspector]public string filePath = "C:\\Users\\Mart9\\Workspace\\SMARC\\SMARC-RL\\RosBag\\TankJuly03_14_58_37\\rosbag2_2025_07_03-14_58_37_0.db3";
        public double startOffset;
        private double start;
        private double end;
        private double diff;

        public SortedDictionary<long, PercentStampedMsg> vbs_cmd;
        public SortedDictionary<long, OdometryMsg> odometry;
        public SortedDictionary<long, PercentStampedMsg> lcg_cmd;
        public SortedDictionary<long, ThrusterAnglesMsg> angles_cmd;
        public SortedDictionary<long, ThrusterRPMsMsg> rpms_cmd;
        public SortedDictionary<long, PoseStampedMsg> pose;
        public SortedDictionary<long, TwistStampedMsg> twist;

        public BagData CurrentBagData;

        private void Awake()
        {
            ReadFile();
        }

        public void ReadFile()
        {
            using var connection = new SqliteConnection($"Data Source={filePath}");
            connection.Open();

            vbs_cmd = ReadMessagesOfType<PercentStampedMsg>(connection, "/sam/core/vbs_cmd");
            lcg_cmd = ReadMessagesOfType<PercentStampedMsg>(connection, "/sam/core/lcg_cmd");
            rpms_cmd = ReadMessagesOfType<ThrusterRPMsMsg>(connection, "/sam/core/thruster_rpms_cmd");
            angles_cmd = ReadMessagesOfType<ThrusterAnglesMsg>(connection, "/sam/core/thrust_vector_cmd");

            odometry = ReadMessagesOfType<OdometryMsg>(connection, "/mocap/sam_mocap/odom");
            pose = ReadMessagesOfType<PoseStampedMsg>(connection, "/mocap/sam_mocap/pose");
            twist = ReadMessagesOfType<TwistStampedMsg>(connection, "/mocap/sam_mocap/velocity");

            start = vbs_cmd.Keys.Min();
            end = vbs_cmd.Keys.Max();
            diff = (end - start) / 1000000000f;

            CurrentBagData = new BagData();
            CurrentBagData = ReadFields(startOffset * 1000000000);
        }

        public (double, double) GetStartEnd()
        {
            return (start, end);
        }

        private void FixedUpdate()
        {
            var currentTime = startOffset * 1000000000 + start + Time.fixedTimeAsDouble * 1000000000;
            var bagData = ReadFields(currentTime);
            if (bagData != null) CurrentBagData = bagData;
        }

        public BagData ReadFields(double timeToReadAt)
        {
            if (timeToReadAt >= start && timeToReadAt <= end)
            {
                var vbsMsg = vbs_cmd.GetLatestMessage(timeToReadAt);
                var lcgMsg = lcg_cmd.GetLatestMessage(timeToReadAt);
                var rpmMsg = rpms_cmd.GetLatestMessage(timeToReadAt);
                var angleMsg = angles_cmd.GetLatestMessage(timeToReadAt);

                var odometryMsg = odometry.GetLatestMessage(timeToReadAt);

                var poseMsg = pose.GetLatestMessage(timeToReadAt);
                var twistMsg = twist.GetLatestMessage(timeToReadAt);

                BagData bagData = new BagData
                {
                    Vbs = vbsMsg.value,
                    Lcg = lcgMsg.value,
                    Thruster1RPM = rpmMsg.thruster_1_rpm,
                    Thruster2RPM = rpmMsg.thruster_2_rpm,
                    ThrusterHorizontalRad = angleMsg.thruster_horizontal_radians,
                    ThrusterVerticalRad = angleMsg.thruster_vertical_radians,
                    PositionRos = new Vector3((float)odometryMsg.pose.pose.position.x, (float)odometryMsg.pose.pose.position.y, (float)odometryMsg.pose.pose.position.z),
                    OrientationRos = new Quaternion((float)poseMsg.pose.orientation.x, (float)poseMsg.pose.orientation.y, (float)poseMsg.pose.orientation.z, (float)poseMsg.pose.orientation.w),
                    LinearVelocityRos = new Vector3((float)twistMsg.twist.linear.x, (float)twistMsg.twist.linear.y, (float)twistMsg.twist.linear.z),
                    AngularVelocityRos = new Vector3((float)twistMsg.twist.angular.x, (float)twistMsg.twist.angular.y, (float)twistMsg.twist.angular.z),
                };
                bagData.PrevPositionRos = CurrentBagData?.PositionRos == null ? bagData.PositionRos : CurrentBagData.PositionRos;
                bagData.PrevOrientationRos = CurrentBagData?.OrientationRos == null ? bagData.OrientationRos : CurrentBagData.OrientationRos;
                return bagData;
            }

            return null;
        }


        private static SortedDictionary<long, T> ReadMessagesOfType<T>(SqliteConnection connection, String topicName) where T : Message
        {
            var id = GetTopicId(connection, topicName);

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT topic_id, timestamp, data FROM messages WHERE topic_id = $topic_id";
            cmd.Parameters.AddWithValue("$topic_id", id);

            using var reader = cmd.ExecuteReader();
            var method = typeof(T).GetMethod("Deserialize", new[] { typeof(MessageDeserializer) });

            SortedDictionary<long, T> messages = new SortedDictionary<long, T>();
            while (reader.Read())
            {
                long topicId = reader.GetInt64(0);
                long timestamp = reader.GetInt64(1);
                byte[] data = (byte[])reader["data"];


                var messageDeserializer = new MessageDeserializer();
                messageDeserializer.InitWithBuffer(data);

                var invoke = method.Invoke(null, new object[] { messageDeserializer });
                messages.Add(timestamp, (T)invoke);
            }

            return messages;
        }

        public static int? GetTopicId(SqliteConnection connection, string topicName)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT id FROM topics WHERE name = $name";
            cmd.Parameters.AddWithValue("$name", topicName);

            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    Console.WriteLine($"Id: {id}");
                    return id;
                }
                else
                {
                    Debug.LogWarning("Topic Id not found.");
                }
            }

            return null;
        }
    }

  
}