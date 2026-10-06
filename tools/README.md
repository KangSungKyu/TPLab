# 재사용 검증 도구

작업별 입력과 출력은 명시한다. 과거 출력은 현재 테스트/승인을 대신하지 않는다. Python 표준 라이브러리만 사용하며 도구는 파일 삭제·Git 통합·Unity 실행을 수행하지 않는다.

```powershell
python tools/verify_validation.py --evidence doc/validation/scene-transition-contracts
python tools/check_github_ci.py --repository KangSungKyu/TPLab --ref <정확한-40자리-commit> --output Temp/<작업>/ci.json
```

verify_validation.py는 evidence의 checks.json이 지정한 결과 건수·문서·asset, test-inputs.json의 LF 정규화 source hash, preserved-inputs.json의 raw bytes, final Console/Editor 증거를 확인한다. Unity 테스트를 실행하지 않는다. 최소 실제 실행은 [Phase 1 검증](../doc/validation/scene-transition-contracts/README.md)에 기록한다.

check_github_ci.py는 기존 Git credential helper를 사용해 정확한 commit·main 보호·workflow/check/status/run을 읽는다. 비밀값을 출력하거나 저장하지 않는다. 현재 자동 확인은 **보호되지 않은 main + CI 미구성**인 경우만 완료하며 이것을 CI 성공으로 표시하지 않는다. CI/보호 설정이 있으면 report를 보존하고 실패 종료하므로 필수 검사와 정책을 별도로 검토해야 한다. 전체 정책 해석·페이지별 결과 통합은 지원하지 않으며 configured CI에 대한 자동 병합 승인을 제공하지 않는다.
