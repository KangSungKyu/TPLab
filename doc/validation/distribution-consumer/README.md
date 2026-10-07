# P2 실제 UPM 소비 검증

검증 후보 source: `5f2e08d8bacb2320ca0832120f194c326869ce40`, Unity **6000.3.18f1**, 동일 PC의 Windows Mono. 패키징 사본은 Core118/Input69/Editor40, 총227 files다. [manifest](distribution-manifest-e.json), [SHA256](SHA256SUMS-e.txt), [재현](archive-reproduction-e.json), [matrix](matrix-e.json), [집계/보호](summary-e.json)가 현재 완료 근거다. packager manifest의 `NotRun`/`publishable:false`는 생성 시점 상태를 보존한 것이며 아래 실제 소비 결과와 tag/Release 상태를 대신하지 않는다.

| 설치 | 범위 | 실제 process | 결과 | fresh evidence |
|---|---|---|---|---|
| git | full | 6 | Pass | [core-consumer-20261007T093824Z-30376.json](core-consumer-20261007T093824Z-30376.json) |
| git | core | 3 | Pass | [core-consumer-20261007T094205Z-30376.json](core-consumer-20261007T094205Z-30376.json) |
| git | input | 3 | Pass | [core-consumer-20261007T094328Z-30376.json](core-consumer-20261007T094328Z-30376.json) |
| git | editor | 3 | Pass | [core-consumer-20261007T094503Z-30376.json](core-consumer-20261007T094503Z-30376.json) |
| tarball | full | 6 | Pass | [core-consumer-20261007T094629Z-30376.json](core-consumer-20261007T094629Z-30376.json) |
| tarball | core | 3 | Pass | [core-consumer-20261007T095005Z-30376.json](core-consumer-20261007T095005Z-30376.json) |
| tarball | input | 3 | Pass | [core-consumer-20261007T095127Z-30376.json](core-consumer-20261007T095127Z-30376.json) |
| tarball | editor | 3 | Pass | [core-consumer-20261007T095301Z-30376.json](core-consumer-20261007T095301Z-30376.json) |

**8/8 configurations 통과**, 소비 Editor18회·기본 Player8회·sample Player4회, 총30회. 소비 build8개와 Additive/Single sample build4개의 오류는0이다. Sample Player는 각각12 checks, 총48 checks다. [개발 경로 fixture](canonical-samples-e.json)는 별도로 Editor1회·Player3회, 개발 경로 sample 생성·소유권 충돌 거부·원래 setup/Build Settings 복원·두 모드 각각12 checks를 확인했다. 이 fixture의 sample 소스 복사는 UPM `Sample.Import` 증거와 구분하며 기존 원본 Editor의 실행 증거로 표현하지 않는다.

Core만/Input 추가/Editor 추가/전체+Samples의 의존성 resolve·컴파일·최소 실행을 Git URL과 실제 file tarball provider에서 확인했다. artifact scope에는 Core/Input/Editor Assets 소스 사본이 없다. lock provider와 Git exact SHA, 등록 package의 실제 version·해결 경로와 모든 package payload files를 대조했다. DLL/바이너리와 tarball의 소스/문서는 exact bytes, Git text는 Git EOL 차이만 정규화한다. Unity가 추가한 package.json `_fingerprint`만 provider별32/40 hex와 실제 cache 이름을 확인해 허용하며 원래 명세의 JSON 값/추가 필드는 바뀔 수 없다.

전체 scope는 실제 Package Manager `Sample.Import`로 Core Pooling과 Scene Transitions를 Assets에 가져왔다. Input sample은 소비 프로젝트의 uGUI2.0.0을 명시적으로 추가했으며 Core-only에는 Input/uGUI를 강제하지 않는다. 예제 builder는 보존한 MonoScript GUID로 실제 위치를 찾고 Player는 Bootstrap 소유 scene에서 root를 결정한다. [25개 기존 meta/GUID](sample-guid-preservation.json)는 그대로 보존했다. `Build Sample Assets`를 명시적으로 실행한 뒤에만 imported scene/settings의 프로젝트 경로를 사용한다.

Editor scope4개 각각에서 installed importer의 설정 없음/default Disabled 자동 무동작, 실제 CSV/schema typed/project validator, 잘못된 schema와 Packages 출력 거부, 생성 대기와 다음 launch의 실제 generated DTO 컴파일/validator, rejecting validator 시 이전 소스 보존, package/기존 Assets/Build Settings 보호와 own-folder/profile cleanup을 확인했다. `setting.asset`은 소비 프로젝트 소유이며 생성 대기를 Validated로 기록하지 않는다. Sample import→compilation→importer generation→compilation/use를 별도 Editor launch로 구분했다.

도구 계약: [최종33 tests](python-final-committed.txt), **32 pass·1 skip**(OS가 native directory symlink 생성을 허용하지 않음). Python14 consumer+19 packaging methods다. [path Red11/11 실패](path-red.xml) → [Green12/12 통과](path-green.xml)는 실패 없는 compile만을 Red로 오인하지 않는다. 최초 [Python Red](python-red.txt), [sample packaging Red](samples-packaging-red.txt), [DLL gate Red](dll-gate-red.txt), [UPM metadata Red](upm-metadata-red.txt), [Git metadata Red](git-metadata-red.txt)와 수정 후 Green을 보존했다.

최초 실패들도 보존했다: PackageInfo alias 컴파일 오류([기록](first-consumer-failure.txt)); Unity32 fingerprint([관찰](upm-metadata-observation.json)); batch 종료/다음 build의 Temp 삭제([관찰](sample-temp-observation.json)); Git40 fingerprint([관찰](git-metadata-observation.json)). 수정 이후 새 output/SHA에서 검증했고 실패를 성공으로 덮어쓰지 않았다. 예제 Player는 각 성공 build 직후 소비 `Build/Sample-<mode>`에 보존한다.

원본 main `af9958ba402be2bc7f31d453afb2b14294a47ac8`, 기존 Editor PID28008과 사용자 씬/설정6개를 보존했다. 원본 Editor는 ready/compile=false/update=false/play=false를 읽기 전용으로 확인했다. 전체 개발 회귀·제품 Console·시각/physical input/해상도 수락을 이 소비 결과로 확대하지 않는다. 다른 PC/Unity/플랫폼/IL2CPP와 실제 addressable scene/download는 이번 최소 소비 범위 밖이다. [exact CI](source-ci-e.json)는 main unprotected/CI unconfigured 확인이며 CI 실행 성공이 아니다.

P2 phase는 트랙으로 통합한다. P3 fixed 후보 회귀/최종 사용자 확인, P4 main/tag/Release와 다운로드/tag URL 검증은 남아 있다. 정식 `v0.0.1` tag/Release는 발행하지 않았다. `Temp/DistributionConsumer/git-full-e`, `tarball-full-e`, `canonical-samples-e`와 `tplab/p2-candidate-e`는 후속 P3 재검증/사용자 확인에 필요해 보존하며 종료 조건 뒤 정리한다. 다른 소비/실패 output은 필요한 JSON/XML/진단 보존 후 이번 작업의 명시 목록으로 정리한다.
