# TPLab Netcode

PackageVersion: 0.0.2. Package installation/Player validation: **NotRun**.

## Install

Supply dependencies in consumer Packages/manifest.json; Git dependencies cannot be declared in package.json.

- UniTask 2.5.11: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11`
- Package dependencies: `{"com.cysharp.unitask": "2.5.11", "com.unity.collections": "2.6.6", "com.unity.netcode.gameobjects": "2.13.3"}`
- Git URL after release: `https://github.com/KangSungKyu/TPLab.git?path=/upm/com.tplab.netcode#v0.0.2`
- Tarball: `com.tplab.netcode-0.0.2.tgz`; supply any listed TPLab package dependencies from the same version.

Tag and installation are not verified by this packager. Do not install alongside Assets source copies of the same assemblies/GUIDs.

[Human API](Documentation~/api/README.md) · [AI guide](Documentation~/ai/README.md) · [License](LICENSE.md) · [Third-party notices](Third%20Party%20Notices.md)

Confirmed source environment: Unity 6000.3.18f1, Windows Mono. Other versions/platforms/IL2CPP are unverified.
Optional samples: Package Manager > selected TPLab package > Samples > Import. Netcode Session sample additionally requires `com.unity.inputsystem: 1.19.0` and `com.unity.ugui: 2.0.0`; package dependencies are `com.unity.netcode.gameobjects: 2.13.3`, `com.cysharp.unitask: 2.5.11`, and `com.unity.collections: 2.6.6`. Import into a fresh project to avoid duplicate script GUIDs.
