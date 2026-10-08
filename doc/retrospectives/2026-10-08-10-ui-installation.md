# UIContext P6 설정·root·예제

상태: 자동 focused 검증/예제 생성 완료, P7 소비·Player·성능·최종 사용자 확인 대기.
기준: codex/game-ui-p6-integration, P5 tip37ad2416e79b7d241bca26935b60a341b8b1a712. Source21f89e2580f08be3903724efa0a42e5ad0567c83.

UIContextSettings의 script/Inspector 공용 validator와 설치 시 snapshot, root별 UIContextInstaller를 구현했다. 등록/자산 preload/firstHUD는 기존 Context 경로를 사용하고 정상 reverseRelease에서 UI를 먼저 await한다. 빌린 Settings·host·Resource/Input·EventSystem·prefab을 해제하지 않는다. Input 준비 차단은 프로젝트가 전체 root 준비 후 공개한다.

선택 Input 예제는 자체 UIInput actions와 직접 prefab으로 구성했다. Input→scope→UI 설치와 Core Bootstrap/Single/Additive/Area/Nested/loading callback을 연결하며 별도 manager/Factory를 추가하지 않았다. 새 UI 자산19개와 meta만 생성하고 원래 씬 구성/BuildSettings/사용자6개를 보존했다.

실제 Red7 실패 후 첫 Green86에서 fixture3건이 실패했다. enumValueIndex999가 정상enum1을 선택하는 실제 관찰을 근거로 intValue로 수정; 비활성 하위 installer 검색을 명시; 테스트 자체 empty Addressables catalog와 prefs/locator 복원을 추가했다. Runtime validator 완화나 로그 숨김은 하지 않았다. 예제 compile의 SceneTarget using 누락은 namespace만 보정했다. 최종 원본Editor24376 Edit33/33+Play53/53, 실패0/skip0, 제품Console0. 입력471/비보호Git470/보호6 raw unchanged. [검증 근거](../validation/ui-system/p6/README.md).

원본 Editor의 UI3 compiler registry 누락은 일회성 입력 보완만 수행했다. 해당 타입 로드/실제 테스트 통과를 확인했지만 영구 복구를 주장하지 않는다. P7 fresh source 소비 compile이 독립 확인이다. 자동 생성 YAML 공백은 보존하고 코드/문서 공백은 엄격히 검사했으며 신규 소유 파일의 줄바꿈만 LF 정책에 맞췄다. 입력 해시 도구에 prefab 본문도 포함했다.

Git: 소유 source70파일을21f89e2에 기록; 문서/XML/사람·AI API/증거·회고 후 track ancestry/CAS 통합한다. main/upm/version/tag/Release는 변경하지 않았다. P7 helper Red4/Green4와 실제 CRLF/Git blob 차이 추가Red1은 독립 Temp 준비며 아직 소비/Player 실행 성공이 아니다. P7은 원본 회귀, fresh 소비/그래픽Player, 동일1,000개 비교 및 가상10,000개, Canvas 비교, 예제Player 이후 최종 시각/물리 입력 확인을 모은다. 진행 중 Temp는 해당 gate 완료와 증거 보존 후 정확 소유 목록만 제거한다.
