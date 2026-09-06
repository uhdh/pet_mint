# 🐾 민트 키우기 (Raising Mint) - AI Agent / 개발자 인수인계 문서 (HANDOFF)

> **문서 작성 일시**: 2026-09-06 21:35 (KST)  
> **프로젝트 루트**: `E:\project\mint_mom\desktop-bunny-pet`  
> **Git 원격 저장소**: `https://github.com/uhdh/pet_mint.git` (기본 브랜치: `master`)  
> **운영체제 환경**: Windows 11 (PowerShell / CMD)  
> **빌드 환경**: Visual Studio Build Tools 2022 (MSBuild 17.14+), .NET Framework 4.8

---

## 1. 프로젝트 요약 (Executive Summary)

**민트 키우기(Raising Mint)**는 사용자의 실제 반려 토끼 **'민트(Mint)'**를 모티브로 제작된 독립형 Windows 데스크톱 가상 펫(Desktop Pet) 애플리케이션 및 반응형 소개 웹사이트입니다.

1. **데스크톱 앱 (`BunnyPet.exe`)**:
   - C# .NET Framework 4.8 WPF 단일 무설치 독립 실행 파일 (외부 DLL 설치 불필요).
   - 화면 최상단(`Topmost`)에서 마우스 접근 감지, 걷기, 잠자기, 세수, 벌러덩 눕기, 래빗키스, 실사 애니메이션 등을 실행.
   - 5종 선물 아이템(건초, 반성의자, 인형, 가방, 집) 장착 및 호감도 시스템(0~100점).
   - 트레이 아이콘 우클릭 메뉴 및 전용 대시보드 창(`DashboardWindow.xaml`) 제공.
2. **소개 웹사이트 (`site/`)**:
   - 브라우저 상에서 데스크톱 펫의 움직임과 포즈를 그대로 체험해볼 수 있는 가상 시뮬레이터 및 샌드박스 탑재.
   - 빌드된 최신 실행 파일(`site/downloads/BunnyPet.exe` 및 `.zip`) 배포 제공.

---

## 2. 필수 빌드 및 실행 방법 (Build & Run Guide)

### 1) 빌드 도구 (MSBuild) 경로
- **경로**: `D:\VisualStudio\BuildTools\MSBuild\Current\Bin\MSBuild.exe`
- **프로젝트 파일**: `native\BunnyPet.csproj`

### 2) 릴리스 빌드 명령어 (PowerShell)
```powershell
# 1. 실행 중인 프로세스 종료 (실행 중이면 빌드 파일 잠김 발생)
Stop-Process -Name BunnyPet -Force -ErrorAction SilentlyContinue

# 2. MSBuild 실행 (x64 Release)
& "D:\VisualStudio\BuildTools\MSBuild\Current\Bin\MSBuild.exe" native\BunnyPet.csproj /p:Configuration=Release /p:Platform=x64

# 3. 루트 및 웹 다운로드 디렉터리로 배포 복사 및 압축 갱신
Copy-Item native\bin\x64\Release\BunnyPet.exe .\BunnyPet.exe -Force
Copy-Item native\bin\x64\Release\BunnyPet.exe site\downloads\BunnyPet.exe -Force
Compress-Archive -Path native\bin\x64\Release\BunnyPet.exe -DestinationPath site\downloads\BunnyPet-v1.0.1-win-x64.zip -Force
```

### 3) 앱 실행 방법 (매우 중요!)
- 백그라운드 콘솔(Subprocess)에서 실행하면 콘솔 세션 종료 시 앱이 함께 닫힙니다.
- 사용자의 Windows 데스크톱 세션에서 독립적으로 띄우려면 반드시 **`explorer.exe`**를 통해 실행해야 합니다:
```cmd
cmd.exe /c explorer.exe "E:\project\mint_mom\desktop-bunny-pet\BunnyPet.exe"
```
- 프로세스 확인:
```cmd
tasklist /FI "IMAGENAME eq BunnyPet.exe"
```

---

## 3. 핵심 아키텍처 및 구현 세부사항

### 1) 풀컬러 32-bit 이모티콘 렌더링 엔진 (`MessageEmoji`)
- **문제 배경**: WPF .NET 4.8의 `TextBlock`은 Windows 컬러 폰트(COLR/CPAL)를 지원하지 않아 이모지가 흑백 윤곽선으로만 렌더링됨.
- **해결책**:
  - `native/Assets/emojis/` 폴더에 87종 이상의 64x64 RGBA 투명 PNG 스프라이트를 내장(`1f47f.png` 등).
  - 유니코드 코드포인트를 키로 매핑하여 `MessageEmoji` (`Image` 컨트롤)에 고화질 캐시 렌더링.
- **★ 핵심 사용자 요구사항 (절대 준수)**:
  - **민트 대화 말풍선에는 텍스트(한국어, 외국어 등)를 일체 표시하지 않고, 100% 순수 이모티콘만 표출해야 합니다.**
  - `MainWindow.xaml.cs`의 `ShowMessage(string text, ...)` 함수 내부에서 `Message.Text` 일반 텍스트 블록은 원천적으로 `Visibility = Visibility.Collapsed`로 고정되어 있습니다. 이 설정을 풀거나 텍스트를 노출하지 마십시오.

### 2) 실사 애니메이션 및 프레임 시퀀스
- **실사 쓰다듬기 (`BunnyState.PetReal`, 45점)**:
  - `native/Assets/pet/pet_000.png` ~ `pet_032.png` (33프레임, 30fps)
  - 마우스 쓰다듬기 시 45점 이상이면 랜덤으로 부드러운 실사 쓰다듬기 시퀀스가 재생됩니다.
- **어리둥절 민트 (`BunnyState.Confused`, 70점)**:
  - `native/Assets/confused/confused_000.png` ~ `confused_045.png` (46프레임)
- **현상수배 전단지 (`BunnyState.Wanted`, 90점)**:
  - `bunny-wanted.png` (가독성을 위해 150x220 스케일 팝업으로 크기 동적 확장 처리).
- **실물 사진 6종 + 찐 화남 (전부 누끼 배경 제거 완료)**:
  - `bunny-real-angry.png` (찐 화난 민트, 절규 실사 만화풍 누끼)
  - `bunny-angry2.png` (화났어2, 뾰로통하게 삐진 실사)
  - `bunny-curious.png` (호기심 민트, 동그란 눈으로 쳐다보기)
  - `bunny-cheer.png` (환호 민트, 두 발로 서서 기뻐하기)
  - `bunny-chin.png` (턱괴기 민트, 문틈에 턱 괴고 휴식)
  - `bunny-paw.png` (앞발 올리기 민트, 앞발 살포시 얹기)
  - `bunny-stand2.png` (두발 서기 민트, 공손하게 서서 간식 청하기)

### 3) 글로벌 치트키 & 호감도 시스템
- `GlobalInputWatcher.cs`: Windows Low-Level Keyboard Hook (`SetWindowsHookEx`) 장착.
- 키보드로 `mintcheat`를 연속 입력하거나 단축키를 누르면 호감도 100점 전체 해금이 즉시 적용됩니다.
- 팝업/소리 없이 조용히 모든 기능이 즉시 언락되도록 구성되어 있습니다.

### 4) 단일 인스턴스 보호 및 설정 저장
- `App.xaml.cs`: `MyBunnyDesktopPet.SingleInstance` 네임드 뮤텍스로 중복 실행 방지 및 이전 인스턴스 자동 정리.
- `AppSettings.cs`: `%LOCALAPPDATA%\MyBunnyDesktopPet\settings.json`에 항상위, 호감도, 소리, 빈도수 등 저장/복원.
- 로그 기록: `%LOCALAPPDATA%\MyBunnyDesktopPet\run.log`에 실시간 디버그 로그가 기록됩니다.

---

## 4. 포즈 및 아이템 상태 매핑 레지스트리

### 1) BunnyState 상태 목록 (`BunnyStateMachine.cs`)
| State Enum | 스프라이트 / 리소스 | 해금 점수 | 전용 이모티콘 | 설명 |
| :--- | :--- | :--- | :--- | :--- |
| `Idle` | `bunny-idle.png` | 0 | - | 평상시 대기 |
| `Walk` | `bunny-walk.png` | 0 | - | 깡충깡충 걷기 |
| `Stand` | `bunny-stand.png` | 0 | `👀` | 마우스 커서 올려다보기 |
| `Sleep` | `bunny-sleep.png` | 0 | `💤` | 쿨쿨 잠자기 |
| `Happy` | `bunny-happy.png` | 0 | `💕` | 기쁨 깡충 모션 |
| `Intro` | `bunny-intro.png` | 0 | `✨` | 천사 날개 인사 |
| `Beg` | `bunny-beg.png` | 5 | `🌾` | 간식 내놔 포즈 |
| `Wash` | `bunny-wash.png` | 15 | `🧼` | 앞발 세수하기 |
| `Angry` | `bunny-angry.png` | 20 | `💢` | 뾰로통 화내기 |
| `Curious` | `bunny-curious.png` | 25 | `👀`, `❓` | 실사 호기심 민트 |
| `Kiss` | `bunny-kiss.png` | 30 | `💋` | 래빗키스 뽀뽀 |
| `Paw` | `bunny-paw.png` | 35 | `🐾`, `🥕` | 실사 앞발 올리기 |
| `PetReal` | `pet/*.png` (33 frames) | 45 | `🥰` | 실사 쓰다듬기 영상 |
| `Binky` | `bunny-binky.png` | 50 | `🤸` | 신나는 점프 빙키 |
| `Stand2` | `bunny-stand2.png` | 60 | `🥕`, `🥺` | 실사 두발 서기 (간식 청하기) |
| `Confused` | `confused/*.png` (46 frames) | 70 | `❓` | 실사 어리둥절 영상 |
| `Cheer` | `bunny-cheer.png` | 75 | `🎉` | 실사 환호 민트 |
| `RealAngry`| `bunny-real-angry.png` | 80 | `👿` | 실사 찐 화남 절규 |
| `Angry2` | `bunny-angry2.png` | 82 | `😾` | 실사 화났어2 (삐짐) |
| `Flop` | `bunny-flop.png` | 85 | `🛌` | 안심하고 벌러덩 눕기 |
| `Chin` | `bunny-chin.png` | 88 | `💤`, `😴` | 실사 턱괴기 민트 |
| `Wanted` | `bunny-wanted.png` | 90 | `🔍` | 현상수배 전단지 민트 |

### 2) BunnyItem 아이템 목록 (`BunnyItem`)
| Item Enum | 오버레이 스프라이트 | 해금 점수 | 설명 |
| :--- | :--- | :--- | :--- |
| `Hay` | `item-hay.png` | 0 | 티모시 건초 (먹을 때마다 호감도 상승) |
| `Chair` | `item-chair.png` | 10 | 반성의자 (귀엽게 쏙 올라앉아 쉬기) |
| `Doll` | `item-doll.png` | 25 | 토끼 인형 (라이벌 등장에 질투 폭발) |
| `Bag` | `item-bag.png` | 40 | 토끼 가방 (등에 메고 외출 기분) |
| `House` | `item-house.png` | 60 | 아늑한 집 (하우스 안으로 쏙 들어가 휴식) |

---

## 5. 작업 시 주의점 및 팁 (Important Pitfalls & Tips)

1. **한글 인코딩 (PowerShell / Python)**:
   - Windows 터미널에서 Python 스크립트 실행 시 `UnicodeEncodeError: 'cp949'` 에러가 발생할 수 있습니다.
   - Python 스크립트 상단에 `sys.stdout.reconfigure(encoding='utf-8')`를 선언하거나 UTF-8 파일 I/O를 명시하십시오.
2. **PowerShell 스크립트 실행 주의**:
   - `run_command` 호출 시 `$변수`가 Bash 스타일에 의해 공백으로 치환될 수 있습니다. 복잡한 로직은 Python 파일로 작성하여 실행하는 것이 훨씬 안전합니다.
3. **새 에셋 추가 시**:
   - `native/Assets/`에 파일 추가 후, 반드시 `native/BunnyPet.csproj`의 `<ItemGroup>` 내 `<Resource Include="Assets\..." />`에 등록해야 빌드에 패키징됩니다.
   - 웹 시뮬레이터(`site/assets/site.js`의 `ASSETS` 및 `site/index.html` 컨트롤)에도 동일하게 반영해야 합니다.
4. **Git 브랜치 관리**:
   - 현재 작업은 `origin/master` 단일 브랜치에 즉시 반영되고 있습니다.
   - 커밋 메시지는 `feat:`, `fix:`, `docs:` 등의 명확한 한글 컨벤션을 따르고 있습니다.
