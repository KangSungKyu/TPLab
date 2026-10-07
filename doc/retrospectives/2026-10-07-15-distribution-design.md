# 첫 배포 규격·dev-build track 설계

- 요청: 배포 파이프라인의 첫 단계로 패키지 구조와 dev-build track 명세를 작성한다. 후속 응답으로 저장소 public 전환·외부 제공·참조 의존성의 최소 조건 유지가 승인됐다.
- 기준: main `993a0617b8b5253175d9a225432f0aa642d19d3d`; 별도 `codex/distribution-design`. 기존 InitScene·Volume·RP·Build Settings·SceneTemplate 변경을 보존한다.
- 변경: [배포 명세](../DISTRIBUTION_PIPELINE.md)에 Core/Input/Editor의 3개 UPM `.tgz`, 선택 sample·문서 projection, 생성 전용 `tplab/<run-id>`, exact commit/독립 consumer·P0~P4 gate를 작성했다. 현재 사람/AI 입구·색인·운영/문서 지침을 연결했다. TPLab 자체 코드/문서는 MIT, 제3자는 원문 조건으로 제공한다.
- 핵심 결정: 개발 원본 Assets는 유지한다. worktree는 각 PC가 clone에서 생성하며 객체·ref 공유는 같은 로컬 Git 안에서 이뤄진다. 생성물은 Git 제외이므로 첫 배포에서 Git UPM path를 제공하지 않는다. UniTask Git dependency는 소비 project manifest에 명시한다.
- 선행 제약: 기존 소비 도구는 소스 복사여서 artifact 설치 검증이 아니다. sample 경로는 Assets 위치 고정이라 UPM import 위치 수정/검증이 필요하다. Core PlayMode tests는 Input/Samples/UI를 참조하므로 Core-only 검증과 전체 개발 회귀를 구분한다.
- 라이선스: 사용자 최소 제한 요청에 따라 TPLab 자체 구현에 MIT를 적용했다. CsvHelper는 MS-PL OR Apache-2.0 원문과 선택권을 보존하며 Unity/UniTask/개발 Connector의 고지·원본을 분리했다. 공개 허용을 제3자 코드의 재허가로 취급하지 않는다.
- 공개 점검: 변경 전 reachable 이력63 commits/1483 blobs의 알려진 credential pattern8개를 검사했고 검출0이었다. 일치 값을 출력/보존하지 않는다. 패턴 검사의 범위/한계와 세부 결과는 [공개 점검](../validation/distribution-design/public-preflight.json)을 따른다.
- 검증: 문서/라이선스만 변경; Unity 테스트0·실행/새 build0·artifact 설치0. source 입력317개 불변, 보호 파일6개 bytes 유지, 문서110개·로컬 링크1145개, 공백·이름·MIT 원문·DLL hash 정적 검증을 통과했다. 결과는 [증거](../validation/distribution-design/README.md)에 기록한다. 과거 테스트를 현재 실행으로 표시하지 않는다.
- 상태: 설계·정적 검증 완료 / `58fc7a15ae772b08afb8d6e450a2a3d0065480c7`까지 main 반영·public 전환 완료. GitHub 서버 private→public·비인증 저장소/MIT 조회를 확인했다. 마감 문서 반영 뒤 작업 브랜치를 정리한다. actual pipeline·packages·v0.0.1 tag/Release는 미구현/미발행이다.
- 다음: P1의 packaging CLI와 실패/재현 검사부터 별도 구현 단위로 시작한다. 버전·소스·asset/script GUID·참조·문서 링크를 실제 생성물로 검증하며 외부 공개 상태와 첫 패키지 발행을 구분한다.
