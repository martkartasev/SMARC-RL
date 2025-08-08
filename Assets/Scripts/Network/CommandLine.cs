using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Environment = System.Environment;

namespace Network
{
    public class CommandLine : MonoBehaviour
    {
        private static bool _initialized;

        private void Awake()
        {
            if (_initialized) return;
            _initialized = true;

            Console.TreatControlCAsInput = true;


            if (Application.isEditor) return;

            var args = GetCommandlineArgs();

            if (args.ContainsKey("-nographics") || args.ContainsKey("-headless") || args.ContainsKey("-batchmode")) CommunicationService.noGraphics = true;

            if (args.TryGetValue("-channel", out var channel))
            {
                int.TryParse(channel, out var channelValue);
                UnityHttpServer.Channel = channelValue;
                Debug.Log($"Channel value: {channelValue}");
            }

            if (args.TryGetValue("-timescale", out var timeScale))
            {
                float.TryParse(timeScale, out var timeScaleValue);
                Time.timeScale = timeScaleValue;
                Debug.Log($"Time scale value: {timeScaleValue}");
            }

            if (args.TryGetValue("-agents", out var agents))
            {
                int.TryParse(agents, out var agentsValue);
                EnvironmentSpawner.defaultAgents = agentsValue;
                Debug.Log($"Agents value: {agentsValue}");
            }

            if (args.TryGetValue("-decisionperiod", out var requestPeriod))
            {
                int.TryParse(requestPeriod, out var requestPeriodValue);
                CommunicationService.decisionPeriod = requestPeriodValue;
                Debug.Log($"Request period value: {requestPeriodValue}");
            }
        }

        public void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            }
        }


        public static Dictionary<string, string> GetCommandlineArgs()
        {
            var argDictionary = new Dictionary<string, string>();

            var args = Environment.GetCommandLineArgs();

            for (var i = 0; i < args.Length; ++i)
            {
                var arg = args[i].ToLower();
                if (arg.StartsWith("-"))
                {
                    var value = i < args.Length - 1 ? args[i + 1].ToLower() : null;
                    value = value?.StartsWith("-") ?? false ? null : value;

                    argDictionary.Add(arg, value);
                }
            }

            foreach (var kvp in argDictionary)
                //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                Debug.Log($"Key = {kvp.Key}, Value = {kvp.Value}");

            return argDictionary;
        }
    }
}