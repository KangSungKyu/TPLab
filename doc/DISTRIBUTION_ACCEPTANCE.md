# 0.0.1 배포 후보 최종 확인

2026-10-08. 후보: `de879b9396ddae0523bd3ab86939b679b383dd92`. 자동 검증 결과와 사람의 사용성 확인은 구분한다. 최종 확인을 완료하고 [정식 Release](https://github.com/KangSungKyu/TPLab/releases/tag/v0.0.1)를 공개했다.

## 확인할 프로젝트

Git과 tarball 소비 프로젝트의 자동 검증을 모두 완료했다.

자동 검증이 끝난 소비 프로젝트 `tplab/worktrees/p3-source/Temp/DistributionConsumer/git-full-p3-b` 또는 `tarball-full-p3-b`를 Unity Hub의 Add project from disk로 등록해 Unity **6000.3.18f1**로 연다. 원래 개발 프로젝트를 변경하지 않는다. 두 소비 프로젝트 모두 실제 UPM 패키지와 가져온 예제가 준비된다. 자신의 프로젝트에서 직접 새로 설치해 아래 항목을 확인해도 된다.

1. Package Manager에서 TPLab Core/Input/Editor **0.0.1**과 Core의 **Core Pooling**, Input의 **Scene Transitions** 예제 표시를 확인한다. 직접 설치할 때는 동일 후보 SHA 또는 동일 SHA256의 `.tgz`를 사용한다. UniTask2.5.11, Addressables2.9.1, Input System1.19.0, Newtonsoft.Json3.2.2와 예제용 uGUI2.0.0 공급원을 명시한다.
2. 가져온 `Assets/Samples/TPLab Core/0.0.1/Core Pooling/CorePooling.unity`를 열고 Play한다. Console의 `TPLab Core pooling sample passed.`를 확인하고 Play를 중단한다.
3. 씬을 저장한 상태에서 `TPLab > Scene Transitions > Build Sample Assets`를 실행한다. Package Manager로 가져온 예제 폴더 안의 씬/설정만 갱신되고, 기존 씬과 Build Settings는 복원되어야 한다.
4. `Open Additive`, 이어서 `Open Single`을 각각 Play한다. `CommonSceneRoot`의 `Use Loading Presentation`을 켜서 실행한다. Hub/Main 전환, Area/Nested 추가·해제, 가림막과 진행 표시, 실제 pointer/키 입력이 정상인지 확인한다. Single에서도 공용 root/UI가 유지되어야 한다.
5. 한 번은 `Manual Proceed`도 켜고 실행한다. Ready 상태에서 Continue 전까지 진행이 대기하고, 클릭 후 전환이 끝나는지 확인한다. 화면을 넓거나 좁게 바꿔 가림막이 화면 전체를 덮는지 확인한다.
6. Play 중단 뒤 기존 열었던 씬/Build Settings가 복원되는지, 예외/컴파일 오류나 중복 assembly/GUID가 없는지 확인한다. 필요하면 `Restore Original Setup`을 사용한다.

Importer의 실제 CSV/schema/typed validator와 거부 시 이전 소스 보존은 자동 검증에 포함된다. 개인 DTO/프로젝트의 전용 validator 설정은 별도 프로젝트 책임이며 이번 수락에서 새 DTO 작성을 요구하지 않는다.

확인 결과는 **설치·예제·Additive/Single·수동 Continue·화면비·복원 확인 완료**, 또는 실패한 항목과 재현 과정으로 전달한다. 자동 검사 통과/시간 경과를 이 확인으로 대체하지 않는다.

## 후속 순서

사용자 확인 → 최신 main 대조·통합·push → 검증한 source에 `v0.0.1` → tag URL 설치와 다운로드한 Release 첨부물 SHA256 확인 → 정식 공개 및 해당 작업 브랜치 정리 순서다. 기존 source를 가리키는 tag는 후속 마감 문서 commit으로 이동하지 않는다. 배포가 끝나면 Unity 저장·정상 종료 후 컴퓨터를 종료한다. Unity 안전한 종료가 실패하면 오프라인 전환 후 절전한다. 확인 대기만으로 Release나 컴퓨터 종료 완료를 보고하지 않는다.

## 확인 상태 (2026-10-08)

후속 사용자 응답: **이 문서의 내용을 실행해서 확인했어**. 최종 사용자 확인은 Confirmed다. main/tag/Release·브랜치 정리·Unity/PC 종료를 후속 P4로 진행한다.

2026-10-08 후속: main 통합·v0.0.1 tag·실제 태그 설치·공개 첨부물8개 다운로드 비교를 완료했다. [P4 증거](validation/distribution-release/README.md)를 따른다.
