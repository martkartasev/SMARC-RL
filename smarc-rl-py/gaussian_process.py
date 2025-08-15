import numpy as np
import torch
from torch import optim, nn
from torch.optim.lr_scheduler import ReduceLROnPlateau

from file_parsing import read_file
from network import ResidualModel, SurrogateModel
from residual_env import UnityResidualEnv

# NEW imports for sklearn
from sklearn.gaussian_process import GaussianProcessRegressor
from sklearn.gaussian_process.kernels import RBF, ConstantKernel as C
from sklearn.multioutput import MultiOutputRegressor
from sklearn.metrics import mean_squared_error
import joblib


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
    train_idx = np.random.choice(train_idx, size=500, replace=False)
    validation_idx = np.arange(split_idx, state_action.shape[0])

    # Compute baseline simulator next-state (without residuals) for training set
    next_sim_state_train = run_sim_batches(env=env,
                                           resets=resets[train_idx],
                                           state_action=state_action[train_idx])

    # ---------- BEGIN: Gaussian Process replacement ----------
    # Prepare training data for sklearn (numpy)
    x_train = state_action[train_idx].astype(np.float64)           # features: state_action (shape N x 11)
    y_data_train = next_state_action[train_idx, 0:6].astype(np.float64)  # true next velocities (N x 6)
    y_sim_train = next_sim_state_train.astype(np.float64)               # sim baseline (N x 6)

    # target residuals = data - sim_baseline
    y_residual_train = y_data_train - y_sim_train  # shape (N, 6)

    # Choose kernel and wrap GPR for multioutput
    # Kernel: Constant * RBF (you can tune length_scale and hyperparams)
    kernel = C(1.0, (1e-3, 1e3)) * RBF(length_scale=1.0, length_scale_bounds=(1e-3, 1e3))

    # Wrap the GPR so we can predict 6 outputs
    base_gpr = GaussianProcessRegressor(kernel=kernel,
                                        alpha=1e-6,
                                        normalize_y=True,
                                        n_restarts_optimizer=2,
                                        random_state=0)

    gpr_multi = MultiOutputRegressor(base_gpr, n_jobs=1)

    print("Fitting Gaussian Process to residuals (this may be slow for large N)...")
    gpr_multi.fit(x_train, y_residual_train)
    print("GPR fit complete.")

    # Save the model to disk
    joblib.dump(gpr_multi, "gpr_residual_model.pkl")
    print("Saved GPR model to gpr_residual_model.pkl")

    # Evaluate on validation set:
    # Option A: predict residuals and compare to true residuals computed from sim baseline
    x_val = state_action[validation_idx].astype(np.float64)
    y_data_val = next_state_action[validation_idx, 0:6].astype(np.float64)

    # compute sim baseline on validation (no residuals)
    next_sim_state_validation = run_sim_batches(env=env,
                                                resets=resets[validation_idx],
                                                state_action=state_action[validation_idx])
    y_sim_val = next_sim_state_validation.astype(np.float64)
    y_residual_val_true = y_data_val - y_sim_val

    # predict residuals with the GPR
    y_residual_val_pred = gpr_multi.predict(x_val)

    # MSE on the residual prediction
    mse_val = mean_squared_error(y_residual_val_true, y_residual_val_pred)
    print(f"Validation MSE (residual prediction) : {mse_val:.6f}")

    # Option B: apply predicted residuals in simulator and measure resulting sim error (closer to original objective)
    # NOTE: this reproduces the original style of checking what happens if we actually apply the predicted residuals.
    print("Running validation sim with predicted residuals to measure true sim error...")
    # run simulator with residuals predicted by GPR
    next_sim_state_with_pred = run_sim_batches(env=env,
                                               resets=resets[validation_idx],
                                               state_action=state_action[validation_idx],
                                               residuals=y_residual_val_pred)
    # Compare simulator output to true data velocities
    sim_vs_data_mse = mean_squared_error(y_data_val, next_sim_state_with_pred)
    print(f"Validation MSE (sim output after applying predicted residual vs data): {sim_vs_data_mse:.6f}")

    # ---------- END: Gaussian Process replacement ----------

    # Optionally: if you still want to export something or continue, do it here.
    return


def run_sim_batches(env, resets, state_action, residuals=None):
    next_sim_state = np.zeros((state_action.shape[0], 6), dtype=state_action.dtype)
    for i in range(0, state_action.shape[0], env.nr_agents):
        end_index = min(i + env.nr_agents, state_action.shape[0])
        if i >= end_index:
            continue

        env.reset(options={"init": resets[i:end_index, :]})

        torques = np.zeros((end_index - i, 6))
        if residuals is not None:
            # residuals may be numpy array; ensure correct slicing and dtype
            torques = np.asarray(residuals[i:end_index, :], dtype=torques.dtype)

        action = np.hstack((state_action[i:end_index, :], torques))

        obs, _, _, _ = env.step(action)

        next_sim_state[i:end_index, :] = obs[:, 4:10]
    return next_sim_state


if __name__ == '__main__':
    main()