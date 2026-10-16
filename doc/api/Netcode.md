# Netcode 세션 모듈
유형: 레퍼런스
현재 상태: P2 internal 프로토콜 구현; 소비 프로젝트용 public API 없음
기준 source: TPLab P2 source d374cb3fc88629b300245f959f07825558f7f355
갱신일: 2026-10-16
계약 링크: [Netcode 세션 계약](../NETCODE_SESSION_DRAFT.md)

`Assets/TPLab/Netcode/Runtime`의 `TPLab.Netcode`는 개발 중인 모듈이다. 현재는 확정 명단·접속 세대·전원 Ready·서버 시작 승인·deadline·중단 규칙을 internal 순수 로직으로 구현했다. 프로젝트가 호출할 세션 wrapper와 NGO 어댑터는 아직 없다. 이 단계의 타입을 reflection으로 직접 사용하거나 public 지원 API로 노출하지 않는다.

| 구분 | 현재 제공 범위 |
|---|---|
| 내부 구현 | [서버](../../Assets/TPLab/Netcode/Runtime/ServerSessionProtocol.cs)·[클라이언트](../../Assets/TPLab/Netcode/Runtime/ClientSessionProtocol.cs)의 단일 attempt 상태기계 |
| assembly | TPLab.Core + Unity.Netcode.Runtime 참조; UI/Input에 NGO 의존 추가 없음 |
| 준비 | 인증 mapping·연결 완료·Ready·서버 자신의 준비를 구분 |
| 실패 | 첫 실패 보존, 늦은 입력 무효화, 기한 만료; 종료/복원 완료는 아직 관찰하지 않음 |
| 시각 | 소유자가 로컬 단조 표본과 양수 budget을 명시; 기본값 없음 |
| 실행 경계 | 호출 직렬화는 소유자 책임; Unity/NGO 호출·lock·비동기 작업 없음 |

설계에서 `EnableSceneManagement=true`를 권장하고 `false`는 프로젝트 선택이다. GameSceneManager는 로컬 씬 수명, NGO는 공유 씬 동기화, 후속 세션 wrapper는 입장·준비·시작 승인을 맡는다. 같은 씬을 두 manager가 동시에 소유하지 않는다. 이 역할 분리의 실제 통합은 P2 구현 범위 밖이다.

정확한 내부 흐름과 남은 결정은 [P2 계약](../NETCODE_SESSION_DRAFT.md#p2-내부-프로토콜-구현)을, AI 작업의 사용 제한은 [AI 참조](../ai/api/Netcode.md)를 따른다. 검증 결과는 [P2 회고](../retrospectives/2026-10-16-02-netcode-p2-protocol.md)에 모았다. Player/IL2CPP·GC·UPM 소비 설치는 미검증이며 0.0.1 배포 사본에 이 모듈을 추가하지 않았다.
