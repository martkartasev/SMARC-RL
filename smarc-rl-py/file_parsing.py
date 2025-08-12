import pandas as pd


def read_file(filename: str = "file.csv"):
    df = pd.read_csv(filename)
    arr = df.to_numpy()

    if (df.max() > 1).any():
        print("Warning, data not properly normalized!")
        print(df.max())
    if (df.min() < -1).any():
        print("Warning, data not properly normalized!")
        print(df.min())

    return arr
