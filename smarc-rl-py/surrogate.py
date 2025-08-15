# Example of data collection
import numpy as np
import torch
from torch import optim, nn

from file_parsing import read_file
from main import run_sim_batches
from network import SurrogateModel
from residual_env import UnityResidualEnv


def generate_surrogate_data():
    env = UnityResidualEnv(port=50012, start_process=True, nr_agents=100, no_graphics=True)
    try:
        num_samples = 100000
        data = read_file()
        resets = data[:-1, np.r_[0:4, 10:14]]
        state_action = data[:-1, 4:15]

        random_indices = np.random.randint(0, len(data) - 1, num_samples)

        states_actions = state_action[random_indices]

        random_residuals = np.random.randn(num_samples, 6)  # Scale residuals as needed

        next_sim_states = run_sim_batches(env, resets[random_indices], states_actions, residuals=random_residuals)

        surrogate_data = np.hstack((states_actions, random_residuals, next_sim_states))
        np.save('surrogate_dataset.npy', surrogate_data)
    finally:
        env.close()


# Create a surrogate model class


def train_surrogate_model():
    # Load the collected data
    surrogate_data = np.load('surrogate_dataset.npy')
    inputs = torch.tensor(surrogate_data[:, :-6], dtype=torch.float32)
    targets = torch.tensor(surrogate_data[:, -6:], dtype=torch.float32)
    # Instantiate the surrogate model
    surrogate = SurrogateModel(inputs.shape[1], targets.shape[1])
    optimizer_surrogate = optim.Adam(surrogate.parameters(), lr=1e-3)
    loss_fn_surrogate = nn.MSELoss()
    # Training loop for the surrogate model
    for epoch in range(1000):
        optimizer_surrogate.zero_grad()
        predictions = surrogate(inputs)
        loss = loss_fn_surrogate(predictions, targets)
        loss.backward()
        optimizer_surrogate.step()
        if epoch % 10 == 0:
            print(f'Surrogate Epoch {epoch}, Loss: {loss.item():.6f}')

    torch.save(surrogate.state_dict(), 'surrogate_model.pth')


if __name__ == '__main__':
    # generate_surrogate_data()
    train_surrogate_model()
