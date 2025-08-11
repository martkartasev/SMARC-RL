import numpy as np

from residual_env import UnityResidualEnv


def main():
    env = UnityResidualEnv(port=50010, start_process=False, nr_agents=1, no_graphics=False)

    for i in range(100000000):
        initialization = np.array([
            [0, 0, 0, 1, 0.1, 0.1, 80, 80]
        ])
        env.reset(options={"init": initialization})

        action = np.array([
            [0, 0, 0,
             0, 0, 0,
             0.1,
             0.1,
             80,
             80,
             800,
             0, 0, 0,
             0, 0, 0]
        ])
        env.step(action)


if __name__ == '__main__':
    main()
