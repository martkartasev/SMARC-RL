from typing import Optional

import numpy as np
from gymnasium import Env, spaces
from statsmodels.sandbox.distributions.genpareto import shape

import protobuf_gen.communication_pb2
from unity_client import start_unity_process, start_client


class UnityResidualEnv(Env):

    def __init__(self,
                 start_process: bool = True,
                 port: int = 10000,
                 nr_agents: int = 1):
        super(UnityResidualEnv, self).__init__()

        self.process = start_unity_process(port=port, nr_agents=nr_agents) if start_process else None
        self.client = start_client(port=port)
        self.nr_agents = nr_agents

        # TODO: Fetch from env? self.client.get_environment_description()
        self.action_space = spaces.Box(low=-1, high=1, shape=(2 * (4 + 3 + 3) + 6,), dtype=np.float32)
        self.observation_space = spaces.Box(low=-1, high=1, shape=(4 + 3 + 3,), dtype=np.float32)

    def reset(self, seed: Optional[int] = None, options: Optional[dict] = None):
        # TODO: Reset parameter mapping
        reset = self.client.reset(protobuf_gen.communication_pb2.Reset())
        obs = map_observation_to_numpy(reset, self.nr_agents)
        return obs

    def step(self, action):
        action_msg = map_action_to_unity(action, self.nr_agents)
        action_msg.stepCount = 2
        step = self.client.step(action_msg)

        obs = map_observation_to_numpy(step, self.nr_agents)

        done = False
        reward = 0
        info = ""
        return obs, reward, done, info

    def render(self, mode='human'):
        pass
        # TODO: Screenshot manager back into API

    def close(self):
        if self.process is not None:
            self.process.terminate()


def map_observation_to_numpy(unity_observations: protobuf_gen.communication_pb2.Observations, nr_agents):
    obs = np.zeros((nr_agents, 4 + 3 + 3))
    for i, unity_obs in enumerate(unity_observations.observations):
        obs[i, :] = np.array(unity_obs.floats, dtype=np.float32)
    return obs


def map_action_to_unity(action, nr_agents):
    step = protobuf_gen.communication_pb2.Step()
    for i in range(nr_agents):
        action_msg = protobuf_gen.communication_pb2.Action()
        action_msg.continuous = action[i, :]

        step.actions.append(action_msg)
    return step
