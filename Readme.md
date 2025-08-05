# Installation

---

When installing with the below option, add manually to interpreter path for IDE.
```
conda create -n ABBEnv python=3.10 && conda activate SMARCRL
```
Then install the packages
```
pip install -r requirements.txt
```
## Trying it out

Once your python environment is set up, you should be able to just run the main method in ik_server/ik_server.py.
The code there executes the server on Python side and also starts the sim which should automatically connect to the IK service example.

If you like the look of it, I can add a few additional commands etc that can help ease the reset of the environment without
restarting the simulation etc.

## Managing Protobuf
You might wish to extend the API with additional variables. For that you need to regenerate the C# and Python files.

To compile protos for python, install grpcio-tools https://grpc.io/docs/languages/python/quickstart/

```
python -m pip install grpcio-tools
```

To compile protos for C#, I suggest downloading the tools package https://www.nuget.org/packages/Grpc.Tools
Extracting the correct binary inside the "Tools" folder in the package, and adding it to your "PATH" environment variables.
For windows, I also suggest renaming "grpc_csharp_plugin.exe" to "protoc-gen-grpc_csharp.exe". Allows using the plugin more easily.

The commands corresponding to python and c# compilation are then as follows:

Run in folder "proto"
```
conda activate SMARCRL
python -m grpc_tools.protoc -I./ --python_out=../../smarc-rl-py/protobuf-gen --pyi_out=../../smarc-rl-py/protobuf-gen  ./communication.proto 
protoc -I ./ --csharp_out=../Assets/Grpc  ./communication.proto
```
