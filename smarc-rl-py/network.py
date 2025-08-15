import torch
import torch.nn as nn


class ResidualModel(nn.Module):
    def __init__(self, state_dim, action_dim, force_dim):
        super().__init__()
        self.state_dim = state_dim
        self.action_dim = action_dim

        self.net = nn.Sequential(
            nn.Linear(state_dim + action_dim, 64),
            nn.ReLU(),
            nn.Linear(64, 64),
            nn.ReLU(),
            nn.Linear(64, force_dim)
        )

        final_layer = self.net[-1]
        final_layer.weight.data *= 1e-3
        final_layer.bias.data *= 1e-3

    def forward(self, state_action):
        return self.net(state_action)

    def export_onnx(self):
        # 2. Create a dummy input
        dummy_input = torch.randn(1, self.state_dim + self.action_dim)

        # 3. Export the model to ONNX
        torch.onnx.export(self,
                          dummy_input,
                          "residual.onnx",
                          input_names=['input'],
                          output_names=['output'],
                          opset_version=15)
