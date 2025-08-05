from typing import Optional

import numpy as np
from gymnasium import Env, spaces

import protobuf_gen.communication_pb2
from unity_client import start_unity_process, start_client


class UnityResidualEnv(Env):

    def __init__(self, startProcess: bool = True,
                 port: int = 10000):
        super(UnityResidualEnv, self).__init__()

        self.process = start_unity_process(port) if startProcess else None
        self.client = start_client(port=port)

        # TODO: Fetch from env?
        self.action_space = spaces.Discrete(2)
        self.observation_space = spaces.Box(low=-np.inf, high=np.inf, shape=(4,), dtype=np.float32)

    def reset(self, seed: Optional[int] = None, options: Optional[dict] = None):
        reset = self.client.reset(protobuf_gen.communication_pb2.Reset())
        return self.state

    def step(self, action):
        done = False
        reward = 0
        info = ""
        return self.state, reward, done, info

    def render(self, mode='human'):
        pass
        #TODO: Screenshot thingy back in


    def close(self):
        if self.process is not None:
            self.process.terminate()
