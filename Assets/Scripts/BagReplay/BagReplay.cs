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
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    public class BagReplay : MonoBehaviour
    {
        public double startOffset;
        private double start;
        private double end;
        private double diff;

        private SortedDictionary<long, PercentStampedMsg> vbs_cmd;
        private SortedDictionary<long, OdometryMsg> odometry;
        private SortedDictionary<long, PercentStampedMsg> lcg_cmd;
        private SortedDictionary<long, ThrusterAnglesMsg> angles_cmd;
        private SortedDictionary<long, ThrusterRPMsMsg> rpms_cmd;
        private SortedDictionary<long, PoseStampedMsg> pose;
        private SortedDictionary<long, TwistStampedMsg> twist;

        public float vbs;
        public float lcg;
        public int thruster1rpm;
        public int thruster2rpm;
        public float thrusterHorizontalRad;
        public float thrusterVerticalRad;
        public Vector3 positionROS;
        public Quaternion orientationROS;
        public Vector3 linearVelocityROS;
        public Vector3 angularVelocityROS;

        private void Awake()
        {
            var dbPath = "C:\\Users\\Mart9\\Workspace\\ROSBAGS\\TankJuly03_14_58_37\\rosbag2_2025_07_03-14_58_37_0.db3";
            using var connection = new SqliteConnection($"Data Source={dbPath}");
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
            ReadFields();
        }

        private void FixedUpdate()
        {
            ReadFields();
        }

        private void ReadFields()
        {
            var currentTime = startOffset * 1000000000 + start + Time.fixedTimeAsDouble * 1000000000;
            if (currentTime <= end)
            {
                var vbsMsg = vbs_cmd.GetLatestMessage(currentTime);
                var lcgMsg = lcg_cmd.GetLatestMessage(currentTime);
                var rpmMsg = rpms_cmd.GetLatestMessage(currentTime);
                var angleMsg = angles_cmd.GetLatestMessage(currentTime);

                var odometryMsg = odometry.GetLatestMessage(currentTime);
                var poseMsg = pose.GetLatestMessage(currentTime);
                var twistMsg = twist.GetLatestMessage(currentTime);


                vbs = vbsMsg.value;
                lcg = lcgMsg.value;
                thruster1rpm = rpmMsg.thruster_1_rpm;
                thruster2rpm = rpmMsg.thruster_2_rpm;
                thrusterHorizontalRad = angleMsg.thruster_horizontal_radians;
                thrusterVerticalRad = angleMsg.thruster_vertical_radians;

                positionROS = new Vector3((float)odometryMsg.pose.pose.position.x, (float)odometryMsg.pose.pose.position.y, (float)odometryMsg.pose.pose.position.z);
                orientationROS = new Quaternion((float)poseMsg.pose.orientation.x, (float)poseMsg.pose.orientation.y, (float)poseMsg.pose.orientation.z, (float)poseMsg.pose.orientation.w);
                linearVelocityROS = new Vector3((float)twistMsg.twist.linear.x, (float)twistMsg.twist.linear.y, (float)twistMsg.twist.linear.z);
                angularVelocityROS = new Vector3((float)twistMsg.twist.angular.x, (float)twistMsg.twist.angular.y, (float)twistMsg.twist.angular.z);
            }
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