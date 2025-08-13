import numpy as np
import torch
from torch import optim, nn
from torch.optim.lr_scheduler import ReduceLROnPlateau

from file_parsing import read_file
from network import ResidualModel
from residual_env import UnityResidualEnv


def main():
    batch_size = 512
    epochs = 25000
    unity_timestep = 0.02

    data = read_file()
    resets = data[:-1, np.r_[0:4, 10:14]]
    state_action = data[:-1, 4:15]
    next_state_action = data[1:, 4:15]

    next_sim_state = prepare_sim_data(resets, state_action)

    acceleration_model = ResidualModel(6, 5, 6)
    optimizer = optim.Adam(acceleration_model.parameters(), lr=1e-3)
    loss_fn = nn.HuberLoss(delta=0.1)
    scheduler = ReduceLROnPlateau(optimizer, mode='min', factor=0.3, patience=2000, min_lr=1e-6)

    for epoch in range(epochs):
        idx = np.random.choice(state_action.shape[0], size=batch_size, replace=False)

        action_tensor = torch.tensor(state_action[idx, :], dtype=torch.float32)

        next_sim_vel = torch.tensor(next_sim_state[idx, :], dtype=torch.float32)
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
            print(f"Epoch {epoch}, Loss: {loss.item():.6f}, Lr: {optimizer.param_groups[0]['lr']:.6f}")
        if epoch != 0 and epoch % 1000 == 0:
            acceleration_model.export_onnx()


def prepare_sim_data(resets, state_action, nr_agents=100):
    env = UnityResidualEnv(port=50010, start_process=True, nr_agents=nr_agents, no_graphics=True)

    next_sim_state = np.zeros((state_action.shape[0], 6), dtype=state_action.dtype)
    for i in range(0, state_action.shape[0], nr_agents):
        end_index = min(i + nr_agents, state_action.shape[0])
        if i >= end_index:
            continue

        env.reset(options={"init": resets[i:end_index, :]})
        action = np.hstack((state_action[i:end_index, :], np.zeros((end_index - i, 6))))
        obs, _, _, _ = env.step(action)
        next_sim_state[i:end_index, :] = obs[:, 4:10]
        print(f"Preparing values {end_index} of {state_action.shape[0]}")

    return next_sim_state


if __name__ == '__main__':
    main()
