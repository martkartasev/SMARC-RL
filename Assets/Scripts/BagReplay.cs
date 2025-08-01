using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using RosMessageTypes.Nav;
using RosMessageTypes.Smarc;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using UnityEngine;

namespace DefaultNamespace
{
    public class BagReplay : MonoBehaviour
    {
        private void Start()
        {
            var dbPath = "C:\\Users\\Mart9\\Workspace\\ROSBAGS\\TankJuly03_14_58_37\\rosbag2_2025_07_03-14_58_37_0.db3";
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            var vbs_cmd = ReadMessagesOfType<PercentStampedMsg>(connection, "/sam/core/vbs_cmd");
            var odometry = ReadMessagesOfType<OdometryMsg>(connection, "/mocap/sam_mocap/odom");
            
        }

        private static Dictionary<long, T> ReadMessagesOfType<T>(SqliteConnection connection, String topicName) where T : Message
        {
            var id = GetTopicId(connection, topicName);

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT topic_id, timestamp, data FROM messages WHERE topic_id = $topic_id";
            cmd.Parameters.AddWithValue("$topic_id", id);

            using var reader = cmd.ExecuteReader();
            var method = typeof(T).GetMethod("Deserialize", new[] { typeof(MessageDeserializer) });

            Dictionary<long, T> messages = new Dictionary<long, T>();
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