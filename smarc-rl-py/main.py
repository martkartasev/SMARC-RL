import numpy as np

from file_parsing import read_file
from residual_env import UnityResidualEnv


def main():
    nr_agents = 30
    env = UnityResidualEnv(port=50010, start_process=False, nr_agents=nr_agents, no_graphics=False)
    data = read_file()

    resets = data[:-1, np.r_[0:4, 10:14]]
    state_action = data[:-1, 4:15]
    next_state_action = data[1:, 4:15]

    for i in range(0, resets.shape[0], nr_agents):
        initialization = resets[i:i+nr_agents, :]
        env.reset(options={"init": initialization})

        action = state_action[i:i+nr_agents, :]
        force_torque = predict_force_torque(nr_agents)

        action = np.hstack((action, force_torque))
        env.step(action)


def predict_force_torque(nr_agents):
    return np.zeros((nr_agents, 6))


if __name__ == '__main__':
    main()
