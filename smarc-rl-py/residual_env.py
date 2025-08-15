import subprocess
from typing import Optional

import numpy as np
from gymnasium import Env, spaces
from statsmodels.sandbox.distributions.genpareto import shape

import protobuf_gen.communication_pb2
from unity_client import start_unity_process, start_client


class UnityResidualEnv(Env):

    def __init__(self,
                 start_process: bool = True,
                 no_graphics: bool = False,
                 port: int = 10000,
                 nr_agents: int = 1):
        super(UnityResidualEnv, self).__init__()

        self.process = start_unity_process(port=port, nr_agents=nr_agents, no_graphics=no_graphics) if start_process else None
        self.client = start_client(port=port)
        self.nr_agents = nr_agents

        # TODO: Fetch from env? self.client.get_environment_description()
        self.action_space = spaces.Box(low=-1, high=1, shape=(3 + 3 + 5 + 3 + 3,), dtype=np.float32)
        self.observation_space = spaces.Box(low=-1, high=1, shape=(4 + 3 + 3,), dtype=np.float32)

    def reset(self, seed: Optional[int] = None, options: Optional[dict] = None):
        agent_inits = options["init"]

        reset_msg = protobuf_gen.communication_pb2.Reset()
        reset_msg.reloadScene = False

        for i in range(agent_inits.shape[0]):
            reset_msg.envsToReset.append(map_reset_params_to_proto(i, agent_inits[i, :]))

        reset = self.client.reset(reset_msg)
        obs = map_observation_to_numpy(reset, agent_inits.shape[0])
        return obs

    def step(self, action):
        action_msg = map_action_to_proto(action)
        action_msg.stepCount = 2
        action_msg.timeScale = 1
        step = self.client.step(action_msg)

        obs = map_observation_to_numpy(step, action.shape[0])

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
            try:
                self.process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                self.process.kill()
                self.process.wait()


def map_reset_params_to_proto(i, initialization):
    params = protobuf_gen.communication_pb2.ResetParameters()
    params.index = i
    params.continuous.extend(initialization)
    return params


def map_observation_to_numpy(unity_observations: protobuf_gen.communication_pb2.Observations, nr_agents):
    obs = np.zeros((nr_agents, 4 + 3 + 3))
    for i, unity_obs in enumerate(unity_observations.observations):
        obs[i, :] = np.array(unity_obs.floats, dtype=np.float32)
    return obs


def map_action_to_proto(action):
    step = protobuf_gen.communication_pb2.Step()
    for i in range(action.shape[0]):
        action_msg = protobuf_gen.communication_pb2.Action()
        action_msg.continuous.extend(action[i, :])

        step.actions.append(action_msg)
    return step
