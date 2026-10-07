# TPLab Input

PackageVersion: 0.0.1. Package installation/Player validation: **NotRun**.

## Install

Supply dependencies in consumer Packages/manifest.json; Git dependencies cannot be declared in package.json.

- UniTask 2.5.11: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11`
- Package dependencies: `{"com.cysharp.unitask": "2.5.11", "com.tplab.core": "0.0.1", "com.unity.inputsystem": "1.19.0"}`
- Git URL after release: `https://github.com/KangSungKyu/TPLab.git?path=/upm/com.tplab.input#v0.0.1`
- Tarball: `com.tplab.input-0.0.1.tgz`; explicitly supply Core before Input/Editor.

Tag and installation are not verified by this packager. Do not install alongside Assets source copies of the same assemblies/GUIDs.

[Human API](Documentation~/api/README.md) · [AI guide](Documentation~/ai/README.md) · [License](LICENSE.md) · [Third-party notices](Third%20Party%20Notices.md)

Confirmed source environment: Unity 6000.3.18f1, Windows Mono. Other versions/platforms/IL2CPP are unverified.
Samples and their UPM import integration are deferred to P2; this P1 package has no bundled samples.
