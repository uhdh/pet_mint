# Microsoft Store 제출 준비서

**작성자: Manus AI**  
**대상 앱: 내 토끼 데스크톱 펫 1.0.1**  
**작성일: 2026년 9월 3일**

## 결론

현재 앱은 Windows 10/11 x64용 Electron 데스크톱 앱으로 정상 패키징되고 테스트되었습니다. Microsoft Store에는 **MSIX 방식**으로 제출하는 것이 적합합니다. MSIX 제출은 Microsoft가 인증 통과 후 패키지를 다시 서명하므로 별도의 상용 코드 서명 인증서를 구매할 필요가 없습니다.[1]

제출용 스크린샷 4장, 300 × 300 Store 로고, MSIX 내부 로고, 한국어 제품 설명, 심사 메모, 개인정보 안내와 연령 등급 응답안이 준비되었습니다. 최종 MSIX 생성에는 Partner Center에서 제품 이름을 예약한 후 표시되는 **Package/Identity/Name**, **Package/Identity/Publisher**, **Publisher display name** 값이 필요합니다. 이 값들은 대소문자와 구두점까지 정확히 일치해야 합니다.[1]

Microsoft Store 개발자 등록과 Windows Apps & Games 작업 영역 활성화가 완료되었습니다. 제품 이름 **내 토끼 데스크톱 펫**도 예약되었으며 Store ID는 **9P717Q34DQDF**입니다. 고객용 URL은 <https://apps.microsoft.com/detail/9P717Q34DQDF>입니다. Partner Center에서 발급한 Package Identity는 `store-identity.txt`와 최종 `AppxManifest.xml`에 반영되었습니다.

## 권장 출시 설정

| 항목 | 권장값 | 근거 |
|---|---|---|
| 제품 유형 | MSIX 또는 PWA 앱 | Windows 데스크톱 앱의 Store 배포 및 자동 업데이트에 적합합니다. |
| 제품 이름 | 내 토끼 데스크톱 펫 | 공개 Store 검색에서 동일한 정확한 이름이 확인되지 않았지만 Partner Center에서 최종 가용성을 확인해야 합니다. |
| 카테고리 | 개인 설정(Personalization) | 바탕화면 경험을 꾸미는 앱의 실제 기능과 가장 가깝습니다. |
| 가격 | 무료 | 결제 기능이나 유료 콘텐츠가 없습니다. |
| 초기 시장 | 대한민국 | 한국어 UI와 한국어 Store 문구가 준비되어 있습니다. |
| 아키텍처 | x64 | 현재 Electron Windows 패키지가 x64입니다. |
| 최소 운영체제 | Windows 10 버전 2004, 빌드 19041 | Manifest가 `packagedClassicApp` 런타임 동작을 사용합니다.[2] |
| 개인정보처리방침 URL | 필수 아님 | 앱은 개인정보를 수집하거나 전송하지 않습니다. Partner Center가 선택적으로 요구하면 준비된 방침을 공개 URL에 게시합니다.[3] |

## 준비된 제출 자료

| 자료 | 위치 | 상태 |
|---|---|---|
| Store 설명 | `store-listing-ko-KR.md` | 완료 |
| 심사 메모 | `certification-notes-ko.md` | 완료 |
| 연령 등급 응답 | `age-rating-answers-ko.md` | 완료 |
| 개인정보 안내 | `privacy-policy-ko.md` | 완료 |
| 데스크톱 스크린샷 | `screenshots/01`–`04` | 1366 × 768 RGB PNG, 완료 |
| Store 타일 로고 | `logos/StoreTile-300x300.png` | 300 × 300 RGB PNG, 완료 |
| MSIX 패키지 로고 | `store-package/Assets` | Manifest 규격별 완료 |
| 최종 Manifest | `store-package/AppxManifest.xml` | Partner Center 식별자 반영 완료 |
| MSIX 빌드 스크립트 | `scripts/build_store_msix.ps1` | Store Identity 기본값 반영 완료 |

Microsoft는 데스크톱 스크린샷을 최소 1366 × 768 PNG로 요구합니다. 스크린샷은 한 장이 필수이며, 여러 장 제공이 권장됩니다. 앱용 1:1 타일 아이콘은 300 × 300 PNG가 권장됩니다.[4]

## Partner Center에서 완료할 순서

| 순서 | 작업 | 필요한 결정 또는 입력 |
|---:|---|---|
| 1 | 활성 개발자 계정으로 Partner Center 로그인 | Microsoft 계정 및 다중 인증 |
| 2 | 새 제품에서 MSIX 또는 PWA 앱 선택 | 제품 유형 확인 |
| 3 | 제품 이름 예약 | `내 토끼 데스크톱 펫`의 가용성 확인 |
| 4 | 앱 ID 세부 정보 복사 | Identity Name, Publisher, Publisher display name |
| 5 | 무서명 MSIX 생성 및 Windows에서 시험 | 준비된 PowerShell 스크립트 사용 |
| 6 | 가격, 시장, 카테고리, 연령 등급 입력 | 무료, 대한민국, 개인 설정 권장 |
| 7 | MSIX와 Store 자료 업로드 | 준비된 파일 사용 |
| 8 | 제출 옵션과 심사 메모 확인 | `runFullTrust` 사유 포함 |
| 9 | 인증 제출 | 최종 공개 전 사용자 확인 필요 |

> **중요:** 제품 이름 예약은 최대 3개월 동안 유지됩니다. 앱 제출을 진행할 계획이라면 이름 예약 후 해당 기간 안에 제출해야 합니다.[5]

## MSIX 생성 명령

Partner Center에서 세 가지 Identity 값을 확보한 뒤 Windows PowerShell에서 다음 명령을 실행합니다.

```powershell
.\scripts\build_store_msix.ps1 `
  -IdentityName "<Package/Identity/Name>" `
  -Publisher "<Package/Identity/Publisher>" `
  -PublisherDisplayName "<Publisher display name>" `
  -DisplayName "내 토끼 데스크톱 펫"
```

스크립트는 Microsoft의 공식 **Windows App Development CLI**를 사용해 무서명 MSIX를 만듭니다. Microsoft Store는 인증 통과 후 MSIX를 Microsoft 인증서로 다시 서명합니다.[1] [6]

## 인증 위험과 대응

| 위험 | 현재 대응 |
|---|---|
| `runFullTrust` 제한 기능 설명 부족 | Electron 데스크톱 프로세스, 투명 창, 화면 위치 이동을 위한 사용 목적을 심사 메모에 명시했습니다. |
| 메타데이터와 실제 기능 불일치 | 모든 설명과 스크린샷은 현재 구현된 기능만 다룹니다. |
| 개인정보 방침 불명확 | 데이터 수집·전송·광고·분석이 없음을 명시했습니다. |
| 패키지 Identity 불일치 | Partner Center에서 발급된 값을 스크립트 매개변수로 직접 사용하도록 했습니다. |
| 패키지 버전 규칙 위반 | 버전은 `1.0.1.0`이며 Store 예약 영역인 네 번째 숫자를 0으로 유지했습니다.[1] |
| 종료 방법을 찾기 어려움 | 우클릭 메뉴의 ‘토끼 보내기’ 방법을 설명과 심사 메모에 포함했습니다. |

## References

[1]: https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-package-requirements "App package requirements for MSIX app"
[2]: https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-manual-conversion "Generating MSIX package components"
[3]: https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/create-app-submission "Create app submission for MSIX apps"
[4]: https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/screenshots-and-images "Add app screenshots, images, and trailers for MSIX apps"
[5]: https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/reserve-your-apps-name "Reserve your app name for MSIX apps"
[6]: https://learn.microsoft.com/en-us/windows/apps/dev-tools/winapp-cli/guides/electron-packaging "Packaging Your Electron App for Distribution"
[7]: https://learn.microsoft.com/en-us/windows/apps/publish/partner-center/open-a-developer-account "Steps to open a developer account"
