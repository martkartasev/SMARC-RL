import numpy as np
import torch
from sympy.abc import delta
from torch import optim, nn
from torch.optim.lr_scheduler import ReduceLROnPlateau

from file_parsing import read_file
from network import ResidualModel
from residual_env import UnityResidualEnv


def main():
    batch_size = 1000
    epochs = 5000
    unity_timestep = 0.02

    data = read_file()
    resets = data[:-1, np.r_[0:4, 10:14]]
    state_action = data[:-1, 4:15]
    next_state_action = data[1:, 4:15]

    next_sim_state = prepare_sim_data(resets, state_action)

    residual_model = ResidualModel(6, 5, 6)
    optimizer = optim.Adam(residual_model.parameters(), lr=1e-3)
    loss_fn = nn.MSELoss()
    scheduler = ReduceLROnPlateau(
        optimizer, mode='min', factor=0.1, patience=100, min_lr=1e-6
    )

    num_samples = state_action.shape[0]
    num_batches_per_epoch = num_samples // batch_size

    state_action_tensor = torch.tensor(state_action, dtype=torch.float32)
    next_sim_vel_tensor = torch.tensor(next_sim_state, dtype=torch.float32)
    next_data_vel_tensor = torch.tensor(next_state_action[:, 0:6], dtype=torch.float32)

    for epoch in range(epochs):
        indices = np.random.permutation(num_samples)

        epoch_loss_total = 0.0

        for batch_num in range(num_batches_per_epoch):
            batch_idx = indices[batch_num * batch_size: (batch_num + 1) * batch_size]

            action_batch = state_action_tensor[batch_idx]
            sim_vel_batch = next_sim_vel_tensor[batch_idx]
            data_vel_batch = next_data_vel_tensor[batch_idx]

            vel_delta = data_vel_batch - sim_vel_batch

            predicted_residual = residual_model(action_batch)
            loss = loss_fn(predicted_residual, vel_delta)

            optimizer.zero_grad()
            loss.backward()
            optimizer.step()

            epoch_loss_total += loss.item()


        avg_epoch_loss = epoch_loss_total / num_batches_per_epoch
        scheduler.step(avg_epoch_loss)

        if epoch % 10 == 0:
            print(f"Epoch {avg_epoch_loss}, Loss: {loss.item():.6f}, Lr: {optimizer.param_groups[0]['lr']:.6f}")
        if epoch != 0 and epoch % 1000 == 0:
            was_training = residual_model.training
            residual_model.eval()
            residual_model.export_onnx()
            if was_training:
                residual_model.train()


def prepare_sim_data(resets, state_action, nr_agents=100):
    env = UnityResidualEnv(port=50012, start_process=True, nr_agents=nr_agents, no_graphics=True)

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
