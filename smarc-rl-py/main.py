import numpy as np
import torch
from torch import optim, nn
from torch.optim.lr_scheduler import ReduceLROnPlateau

from file_parsing import read_file
from network import ResidualModel
from residual_env import UnityResidualEnv


def main():
    nr_agents = 100
    epochs = 50000
    unity_timestep = 0.02
    env = UnityResidualEnv(port=50010, start_process=False, nr_agents=nr_agents, no_graphics=False)
    data = read_file()

    resets = data[:-1, np.r_[0:4, 10:14]]
    state_action = data[:-1, 4:15]
    next_state_action = data[1:, 4:15]

    acceleration_model = ResidualModel(6, 5, 6)
    optimizer = optim.Adam(acceleration_model.parameters(), lr=1e-3)
    loss_fn = nn.MSELoss()
    scheduler = ReduceLROnPlateau(optimizer, mode='min', factor=0.1, patience=10, min_lr=1e-6)

    for epoch in range(epochs):
        idx = np.random.choice(state_action.shape[0], size=nr_agents, replace=False)

        env.reset(options={"init": resets[idx, :]})

        action_tensor = torch.tensor(state_action[idx, :], dtype=torch.float32)

        env_action = torch.cat((action_tensor, torch.zeros((nr_agents, 6), dtype=torch.float32)), dim=1)
        obs, _, _, _ = env.step(env_action.detach().numpy())

        next_sim_vel = torch.tensor(obs[:, 4:10], dtype=torch.float32)  # fixed slice
        next_data_vel = torch.tensor(next_state_action[idx, 0:6], dtype=torch.float32)
        start_vel = torch.tensor(state_action[idx, 0:6], dtype=torch.float32)

        sim_acc = (next_sim_vel - start_vel) / unity_timestep
        data_acc = (next_data_vel - start_vel) / unity_timestep
        acc_delta = data_acc - sim_acc

        predicted_acceleration_residual = acceleration_model(action_tensor)

        loss = loss_fn(predicted_acceleration_residual, acc_delta)

        optimizer.zero_grad()
        loss.backward()
        optimizer.step()
        scheduler.step(loss.item())

        if epoch % 10 == 0:
            print(f"Epoch {epoch}, Loss: {loss.item():.6f}")


if __name__ == '__main__':
    main()
