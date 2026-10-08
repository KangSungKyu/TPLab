# P3 배포 후보 검증

Candidate source: `de879b9396ddae0523bd3ab86939b679b383dd92`, Unity **6000.3.18f1**, Windows Mono, 동일 PC. 정확한 후보에서 전체 EditMode286/286·PlayMode253/253, fail0/skip0을 확인했다. [전체 결과](final2-full-regression.json), [EditMode](final2-EditMode.xml), [PlayMode](final2-PlayMode.xml)를 따른다. 실제 Git/tarball 설치8/8도 통과했다. [최종 matrix](final2-matrix.json)는 Core/Input/Editor/전체+예제를 각각 확인한다.

원본 Editor PID28008에서 main `af9958ba402be2bc7f31d453afb2b14294a47ac8`의 Edit271/271·Play253/253을 별도로 확인했다. [원본 Edit](original-edit.json), [원본 Play](original-play.json), [원본 Console](original-console.json). 원본 결과는 새 후보 검증을 대신하지 않는다. Console에는 테스트의 의도된 예외가 있으므로 제품 Console 전체 무오류로 표현하지 않는다.

## 발견한 실패와 수정

1. 최초 고정5ffe 후보 Edit283 중1개가 manifest 원자적 교체에서 실패했다. [최초 결과](candidate-full-regression.json)를 보존한다. 외부 reader 차단을 실제 재현한 [lock Red](manifest-lock-red.json) 뒤, Windows 오류32/33/1175에만 동일 File.Replace를 최대5회·50ms4회 대기한다. 다른 오류/소진은 전파하고 복원·정리를 유지한다. 원래 첫 실패의 reader 원인은 확정하지 않았다.
2. 네이티브 전역 입력을 사용한 batch Play243/253, graphics-enabled batch도243/253이었다. 공식 InputTestFixture로 가상 입력을 격리했고, 첫 상속안은 Loading12건의 teardown 순서 실패를 만들었다. [상속안 실패](candidate-full-regression-fixture.json), [비동기 정리 뒤 mock 복원 성공](candidate-PlayMode-fixture-cleanup.json)을 보존한다. 제품 Input runtime은 바꾸지 않았다. 가상 입력 테스트 통과는 물리 장치/화면 수락을 대신하지 않는다.
3. cc9609 실제 Git-full 설치에서 정상226자 최종 manifest에 임시 suffix37자가 붙어263자로 실패했다. [최초 설치 실패](final-matrix.json)와 [240자 최종 경로 Red](long-path-red.json)를 보존한다. 같은 출력 폴더 안의 GUID.tmp로 변경한 [Green1/1](long-path-green.json)은 초기 생성·갱신·idempotence·meta/무관한 파일 보존·임시 잔존0을 확인한다. 일반적인 임의 긴 경로 지원은 보장하지 않는다.

## 재현·정책·남은 확인

[생성 manifest](final2-distribution-manifest.json), [SHA256](final2-SHA256SUMS.txt), [exact CI](final2-source-ci.json)를 따른다. 생성 시점 publishable:false/NotRun 값은 그대로 보존하며 후속 실제 결과를 덮어쓰지 않는다. 같은 후보의 두 생성 run은3개 archive bytes/manifest가 일치한다. Core/Input archive는 P2와 동일하고 Editor는 수정됐다.

사람/AI API와 MIT·제3자 고지가 각 패키지에 포함된다. CI는 미구성이며 확인 도구 성공은 CI 실행 성공이 아니다. 사용자 소유 씬/설정6개와 원본 main을 별도로 보호한다. 타PC/Unity버전/IL2CPP/다른플랫폼·remote Addressables download·실제 장치/해상도는 미검증이다.

[사용자 최종 확인](../../DISTRIBUTION_ACCEPTANCE.md) 뒤 P4 main/tag/Release·실제 tag URL/첨부물 다운로드 검증·브랜치 정리를 진행한다. 확인 대기에는 main/tag/Release를 진행하지 않는다.

## 실제 설치 결과

| 설치 | 범위 | 실제 process | 결과 | fresh evidence |
|---|---|---|---|---|
| git | full | 6 | Pass | [core-consumer-20261008T011939Z-29272.json](core-consumer-20261008T011939Z-29272.json) |
| git | core | 3 | Pass | [core-consumer-20261008T012343Z-29272.json](core-consumer-20261008T012343Z-29272.json) |
| git | input | 3 | Pass | [core-consumer-20261008T012450Z-29272.json](core-consumer-20261008T012450Z-29272.json) |
| git | editor | 3 | Pass | [core-consumer-20261008T012613Z-29272.json](core-consumer-20261008T012613Z-29272.json) |
| tarball | full | 6 | Pass | [core-consumer-20261008T012727Z-29272.json](core-consumer-20261008T012727Z-29272.json) |
| tarball | core | 3 | Pass | [core-consumer-20261008T013037Z-29272.json](core-consumer-20261008T013037Z-29272.json) |
| tarball | input | 3 | Pass | [core-consumer-20261008T013143Z-29272.json](core-consumer-20261008T013143Z-29272.json) |
| tarball | editor | 3 | Pass | [core-consumer-20261008T013454Z-29272.json](core-consumer-20261008T013454Z-29272.json) |

소비 Editor18회·기본 Player8회·sample Player4회, 총30process. 기본build8개와 samplebuild4개 오류0, sample4개 각각12checks(총48). Git exact SHA/lock/provider·installed payload를 비교했으며 실제 Sample.Import와 generated importer의 다음 launch 컴파일/validator를 확인했다. 소스 사본을 artifact 설치의 증거로 사용하지 않았다.

[재현·원본 보호](final2-reproduction-protection.json)와 [Python33 methods](python-final2.txt):32pass/1Windows symlink권한skip.

최종 Git/tarball full 소비 프로젝트와 archive·실패/회귀 raw log·Release 안내 초안은 사용자 확인/P4에 필요해 보존한다. 사용을 마친 일회성 스크립트와 중복 evidence는 보존된 증거를 확인한 뒤 소유 목록만 정리한다. 나머지 실패 output은 최초 JSON/XML/진단 보존 후 이번 소유 목록만 정리한다. `Temp` 전체·사용자 파일·원본 tplab/worktrees는 삭제하지 않는다.
