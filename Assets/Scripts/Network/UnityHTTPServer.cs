using System;
using System.Net;
using System.Threading;
using ExternalCommunication;
using Google.Protobuf;
using UnityEditor.Rendering.LookDev;
using UnityEngine;

namespace Network
{
    public class UnityHttpServer : MonoBehaviour
    {
        public static int Channel = 50010;

        private HttpListener httpListener;
        private Observations next_observations;

        private Step step;
        private Reset reset;

        private Thread listenerThread;
        private CommunicationService _communicationService;

        private bool isRunning = true;

        private ManualResetEvent _stepEvent = new(false);
        private ManualResetEvent _resetEvent = new(false);

        public void Awake()
        {
            _communicationService = FindFirstObjectByType<CommunicationService>();
            Physics.simulationMode = SimulationMode.Script;
            Application.targetFrameRate = 60;
            DontDestroyOnLoad(gameObject);
        }

        public void Update()
        {
            if (reset != null) ResetCompleted(_communicationService.DoReset(reset));
            if (step != null) StepCompletedCheck(_communicationService.DoStep(step));
        }

        public void StepCompletedCheck(Observations doStep)
        {
            if (doStep == null) return;

            step = null;
            next_observations = doStep;
            _stepEvent.Set();
        }

        public void ResetCompleted(Observations doReset)
        {
            if (doReset == null) return;

            reset = null;
            next_observations = doReset;
            _resetEvent.Set();
        }

        void Start()
        {
            httpListener = ListenerSetup();

            listenerThread = new Thread(StartListener);
            listenerThread.Start();

            Debug.Log($"Server Started at http://localhost:{Channel.ToString()}/");
        }


        private HttpListener ListenerSetup()
        {
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://localhost:{Channel.ToString()}/reset/");
            httpListener.Prefixes.Add($"http://localhost:{Channel.ToString()}/step/");
            httpListener.Prefixes.Add($"http://127.0.0.1:{Channel.ToString()}/reset/");
            httpListener.Prefixes.Add($"http://127.0.0.1:{Channel.ToString()}/step/");
            httpListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            httpListener.Start();

            return httpListener;
        }

        private void StartListener()
        {
            while (isRunning)
            {
                var result = httpListener.BeginGetContext(CallbackListener, httpListener);
                result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10));
            }
        }


        private void CallbackListener(IAsyncResult result)
        {
            var context = httpListener.EndGetContext(result);
            try
            {
                if (context.Request.HttpMethod == "POST")
                {
                    if (context.Request.Url.ToString().Contains("step"))
                    {
                        _stepEvent = new ManualResetEvent(false);
                        step = Step.Parser.ParseFrom(context.Request.InputStream);
                        _stepEvent.WaitOne(TimeSpan.FromSeconds(9));
                        WriteObservationResponse(context);
                    }

                    if (context.Request.Url.ToString().Contains("reset"))
                    {
                        _resetEvent = new ManualResetEvent(false);
                        reset = Reset.Parser.ParseFrom(context.Request.InputStream);
                        _resetEvent.WaitOne(TimeSpan.FromSeconds(9));
                        WriteObservationResponse(context);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
            finally
            {
                context.Response.Close();
            }
        }

        private void WriteObservationResponse(HttpListenerContext context)
        {
            var bytes = next_observations.ToByteArray();
            WriteBytesToOutputStream(context, bytes);
            next_observations = null;
        }

        private static void WriteBytesToOutputStream(HttpListenerContext context, byte[] bytes)
        {
            context.Response.ContentLength64 = bytes.Length;

            System.IO.Stream output = context.Response.OutputStream;
            output.Write(bytes, 0, bytes.Length);
        }

        public void OnDestroy()
        {
            isRunning = false;
            httpListener.Stop();
            listenerThread.Join();
        }

        public void OnApplicationQuit()
        {
            isRunning = false;
            httpListener.Stop();
            listenerThread.Join();
        }
    }
}