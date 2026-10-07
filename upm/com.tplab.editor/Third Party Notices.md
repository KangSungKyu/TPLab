Repository dependency inventory. CsvHelper DLL is included only in com.tplab.core.

# Third-party notices

TPLab의 자체 코드와 문서는 [MIT License](LICENSE.md)로 제공한다. 이 허가는 아래 제3자 구성 요소의 라이선스·저작권·상표를 변경하지 않는다. 외부 프로젝트 사용·수정·재배포를 허용하며 TPLab의 MIT 고지와 제3자의 해당 고지를 유지한다. 별도의 의무적 홍보·UI 크레딧·소스 공개 조건을 TPLab 자체에 추가하지 않는다.

## 포함한 라이브러리

| 구성 요소 | 버전/원본 | 적용 라이선스 | 보존한 고지 |
|---|---|---|---|
| CsvHelper DLL (`netstandard2.1`) | 33.1.0 / [공식 NuGet](https://www.nuget.org/packages/CsvHelper/33.1.0), [해당 버전 원본](https://github.com/JoshClose/CsvHelper/tree/33.1.0) | **MS-PL OR Apache-2.0** | [동봉 원문](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/Assets/Plugins/CsvHelper/LICENSE.txt) |

CsvHelper의 두 라이선스는 대안이며 원래의 선택권을 유지한다. TPLab의 MIT로 DLL을 재허가하지 않는다. DLL은 수정하지 않았으며 SHA-256은 `20101c398654a14bfd42bd78d7281f43197d19b7b3cf41c7aae93f1eaba65a61`이다. package 생성 시 이 DLL의 정확한 bytes·`.meta`와 원문 라이선스를 함께 보존한다. 원래 제공된 copyright/attribution과 별도 NOTICE가 있으면 그것도 보존한다. [CsvHelper 공식 라이선스 안내](https://joshclose.github.io/CsvHelper/)와 [33.1.0 license expression](https://github.com/JoshClose/CsvHelper/blob/33.1.0/src/CsvHelper/CsvHelper.csproj)을 근거로 한다.

## 소비 프로젝트가 설치하는 의존성

| 구성 요소 | 기준 버전 | 원문/조건 |
|---|---|---|
| UniTask | 2.5.11 | [MIT 원문](ThirdPartyNotices~/UniTask-LICENSE.txt), Copyright (c) 2019 Yoshifumi Kawai / Cysharp, Inc.; [원본 버전](https://github.com/Cysharp/UniTask/tree/2.5.11) |
| Unity Addressables | 2.9.1 | [Unity package LICENSE](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/license/LICENSE.html), Unity Companion License |
| Unity Input System (선택적 Input) | 1.19.0 | [Unity package LICENSE](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/license/LICENSE.html), Unity Companion License |
| Unity Newtonsoft.Json wrapper (선택적 Editor) | 3.2.2 | 해당 package의 LICENSE·제3자 고지. Wrapper와 포함 Newtonsoft.Json 각각의 고지를 구분한다. |
| Unity uGUI (선택적 Sample) | 2.0.0 | 해당 package의 LICENSE, Unity Companion License |

이 Unity package들의 구현 코드는 TPLab 배포물에 복사하지 않고 공식 Package Manager 원본으로 설치한다. 설치된 각 package의 LICENSE/Third Party Notices가 해당 코드에 적용된다. TPLab의 MIT는 Unity Companion License 및 Unity 사용 조건을 대체하지 않는다. Unity 프로젝트 template·Unity가 제공한 기존 설정/자산도 TPLab 자체 코드의 재허가 대상으로 주장하지 않는다. TPLab 자체 구현과 제3자/Unity 구성 요소를 결합한 실제 배포물의 고지를 확인한다.

## 개발 전용 구성

Unity CLI/Connector 0.4.1의 [MIT 원문](https://github.com/KangSungKyu/TPLab/blob/v0.0.1/doc/licenses/UnityCli-LICENSE.txt)은 Copyright (c) 2025 DevBookOfArray이며 [고정 원본](https://github.com/youngwoocho02/unity-cli/tree/07c62fd29f1e6d29d8f9a504b968bedbbd47ddc8)을 사용한다. Connector 구현은 manifest 참조로 설치하고 TPLab 소비 runtime package에 동봉하지 않는다. Unity Test Framework·IDE·URP 등 개발 프로젝트 package는 각각의 설치 원문 조건을 따르며 소비 코어의 필수 의존성으로 전파하지 않는다.

새 패키지/플러그인을 채택하거나 제3자 소스를 수정·동봉하면 해당 원본·버전·변경 표시·라이선스·NOTICE를 같은 작업에서 갱신한다. 외부 의존성에 원문보다 강한 추가 제한을 붙이지 않고, 필요한 원문 조건을 삭제해서도 안 된다. 실제 TPLab UPM 산출물은 아직 발행되지 않았다.
