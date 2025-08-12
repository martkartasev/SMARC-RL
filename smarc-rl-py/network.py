import torch
import torch.nn as nn
import torch.optim as optim


class ResidualModel(nn.Module):
    def __init__(self, state_dim, action_dim, force_dim):
        super().__init__()
        self.net = nn.Sequential(
            nn.Linear(state_dim + action_dim, 128),
            nn.ReLU(),
            nn.Linear(128, 128),
            nn.ReLU(),
            nn.Linear(128, force_dim)
        )

        final_layer = self.net[-1]
        final_layer.weight.data *= 1e-3
        final_layer.bias.data *= 1e-3

    def forward(self, state_action):
        return self.net(state_action)
