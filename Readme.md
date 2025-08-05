# Installation

---

When installing with the below option, add manually to interpreter path for IDE.
```
conda create -n SMARCRL python=3.10 && conda activate SMARCRL
```
Then install the packages
```
pip install -r requirements.txt
```

# Managing Protobuf

---

To change the API you need to regenerate the C# and Python files.

To compile protos for python, install grpcio-tools https://grpc.io/docs/languages/python/quickstart/

```
python -m pip install grpcio-tools
```

Then in the [Proto folder](./Assets/Proto), run:
```
conda activate SMARCRL
python -m grpc_tools.protoc -I./ --python_out=../../smarc-rl-py/protobuf-gen --pyi_out=../../smarc-rl-py/protobuf-gen  ./communication.proto
```
To compile protos for C#, I suggest downloading the tools package https://www.nuget.org/packages/Grpc.Tools
Extracting the correct binary inside the "Tools" folder in the package, and adding it to your "PATH" environment variables.
For windows, I also suggest renaming "grpc_csharp_plugin.exe" to "protoc-gen-grpc_csharp.exe". Allows using the plugin more easily.

The commands corresponding to python and c# compilation are then as follows:

Run in [Proto folder](./Assets/Proto)
```
protoc -I ./ --csharp_out=../Assets/Grpc  ./communication.proto
```
