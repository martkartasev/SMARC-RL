import os
import subprocess
import time

import requests
from google.protobuf.message import DecodeError

from protobuf_gen.communication_pb2 import Reset, Observations, Step


class SimClient:
    def __init__(self, port):
        self.port = port

    def reset(self, message: Reset):
        attempts = 0
        observations = None

        while attempts < 20:
            try:
                obs_bytes = self.do_request(Reset.SerializeToString(message), "reset", timeout=10)
                observations = Observations.FromString(obs_bytes)
                break
            except DecodeError:
                print("Bad host issue, retrying...")
                time.sleep(1)
                attempts += 1

        return observations

    def step(self, message: Step):
        obs_bytes = self.do_request(Step.SerializeToString(message), "step")
        observations = Observations.FromString(obs_bytes)
        return observations

    def do_request(self, msg, method, **kwargs):
        attempts = 0
        while attempts < 20:
            try:
                response = requests.post(
                    f'http://localhost:{self.port}/{method}',
                    data=msg,
                    headers={
                        'Content-Type': 'application/octet-stream',
                    },
                    **kwargs
                )
                return response.content
            except (ConnectionRefusedError, ConnectionError, requests.exceptions.ConnectionError):
                print("Connection refused, retrying...")
                time.sleep(1)
                attempts += 1

        print("Failed to connect after multiple attempts.")
        return None


def start_client(port: int = 10000):
    client = SimClient(port)
    print("Client started, configured for port " + str(port))  # Confirm server started
    return client


def start_unity_process(nr_agents: int = 1,
                        port: int = 10000,
                        log_file: str = "",
                        timescale: float = 1,
                        decision_period: int = 10,
                        no_graphics: bool = True):
    executable_path = os.path.join(os.path.dirname(__file__), '../Builds/SAM-RL.exe')
    args = [executable_path,
            "-agents", str(nr_agents),  # Number of agents
            "-channel", str(port),  # Param to change connection port. If you want to start multiple instances
            ]
    if log_file != "":
        args += ["-log", log_file]

    if timescale != 1:
        args += ["-timescale", str(timescale)]

    if decision_period != 10:
        args += ["-decision_period", str(decision_period)]

    if no_graphics:
        args += ["-headless", "-batchmode", ]  # "-nographics" causes no renderer

    popen = subprocess.Popen(args)
    print("Started Unity process")
    return popen
