import numpy as np
import torch
from torch import optim, nn
from torch.optim.lr_scheduler import ReduceLROnPlateau

from file_parsing import read_file
from network import ResidualModel, SurrogateModel
from residual_env import UnityResidualEnv


def main():
    env = UnityResidualEnv(port=50010, start_process=True, nr_agents=100, no_graphics=True)
    try:
        training(env)
    finally:
        env.close()


def training(env):
    batch_size = 1000
    epochs = 5000

    val_split = 0.2  # last 20% of sequence for validation
    data = read_file()
    resets = data[:-1, np.r_[0:4, 10:14]]
    state_action = data[:-1, 4:15]
    next_state_action = data[1:, 4:15]

    split_idx = int((1 - val_split) * state_action.shape[0])
    train_idx = np.arange(0, split_idx)
    validation_idx = np.arange(split_idx, state_action.shape[0])

    next_sim_state_train = run_sim_batches(env=env,
                                           resets=resets[train_idx],
                                           state_action=state_action[train_idx])

    residual_model = ResidualModel(6, 5, 6)
    optimizer = optim.Adam(residual_model.parameters(), lr=1e-3)
    loss_fn = nn.MSELoss()
    scheduler = ReduceLROnPlateau(
        optimizer, mode='min', factor=0.1, patience=1000, min_lr=1e-6
    )

    # surrogate_model = SurrogateModel(11 + 6, 6)  # 11 for state_action, 6 for residuals
    # surrogate_model.load_state_dict(torch.load('surrogate_model.pth'))
    # surrogate_model.eval()  # Set to evaluation mode

    state_action_tensor_train = torch.tensor(state_action[train_idx], dtype=torch.float32)
    next_sim_vel_tensor_train = torch.tensor(next_sim_state_train, dtype=torch.float32)
    next_data_vel_tensor_train = torch.tensor(next_state_action[train_idx, 0:6], dtype=torch.float32)

    num_train = len(train_idx)
    num_batches_per_epoch = num_train // batch_size

    for epoch in range(epochs):
        np.random.shuffle(train_idx)
        epoch_loss_total = 0.0
        residual_model.train()

        for batch_num in range(num_batches_per_epoch):
            batch_idx = train_idx[batch_num * batch_size: (batch_num + 1) * batch_size]

            action_batch = state_action_tensor_train[batch_idx]
            predicted_residual = residual_model(action_batch)

            # surrogate_input = torch.cat((action_batch, predicted_residual), dim=1)
            # sim_vel_batch = surrogate_model(surrogate_input)
            sim_vel_batch = torch.tensor(run_sim_batches(env=env, resets=resets[batch_idx], state_action=state_action[batch_idx], residuals=predicted_residual.detach().numpy()), dtype=torch.float32)
            #sim_vel_batch = next_sim_vel_tensor_train[batch_idx] #For training without sim in the loop
            data_vel_batch = next_data_vel_tensor_train[batch_idx]

            vel_delta = data_vel_batch - sim_vel_batch
            loss = loss_fn(predicted_residual, vel_delta)

            optimizer.zero_grad()
            loss.backward()
            optimizer.step()

            epoch_loss_total += loss.item()

        avg_epoch_loss = epoch_loss_total / num_batches_per_epoch
        scheduler.step(avg_epoch_loss)

        if epoch % 10 == 0:
            residual_model.eval()
            with torch.no_grad():
                action_validation = torch.tensor(state_action[validation_idx], dtype=torch.float32)
                predicted_validation = residual_model(action_validation)

                next_sim_state_validation = run_sim_batches(env=env, resets=resets[validation_idx], state_action=state_action[validation_idx], residuals=predicted_validation)

                sim_vel_validation = torch.tensor(next_sim_state_validation, dtype=torch.float32)
                data_vel_validation = torch.tensor(next_state_action[validation_idx, 0:6], dtype=torch.float32)

                vel_delta_val = data_vel_validation - sim_vel_validation
                validation_loss = loss_fn(predicted_validation, vel_delta_val).item()

            print(f"Epoch {epoch}, "
                  f"Train Loss: {avg_epoch_loss:.6f}, "
                  f"Validation Loss: {validation_loss:.6f}, "
                  f"Lr: {optimizer.param_groups[0]['lr']:.6f}")

        if epoch != 0 and epoch % 100 == 0:
            residual_model.export_onnx(epoch)


def run_sim_batches(env, resets, state_action, residuals=None):
    next_sim_state = np.zeros((state_action.shape[0], 6), dtype=state_action.dtype)
    for i in range(0, state_action.shape[0], env.nr_agents):
        end_index = min(i + env.nr_agents, state_action.shape[0])
        if i >= end_index:
            continue

        env.reset(options={"init": resets[i:end_index, :]})

        torques = np.zeros((end_index - i, 6))
        if residuals is not None: torques = residuals[i:end_index, :]

        action = np.hstack((state_action[i:end_index, :], torques))

        obs, _, _, _ = env.step(action)

        next_sim_state[i:end_index, :] = obs[:, 4:10]
    return next_sim_state


if __name__ == '__main__':
    main()
