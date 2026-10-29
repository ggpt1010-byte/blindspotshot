# 블라인드건파이트 (가제)

PUBG: BLINDSPOT 같은 **3D 쿼터뷰 택티컬 슈터** 프로토타입. 눈높이 시야 부채꼴 밖은 어둡고, 좀비와 무장한 인간 세력(Greywolf)이 그 어둠 속에서 다가온다.
기획 내용은 Claude 기획서 문서를 기준으로 한다. 예전 2D 버전은 `archive/blindgunfight_2d` 에 보관되어 있다.

## 필요한 것

- **Unity 6** (6000.0 이상)
- 프로젝트 템플릿: **Universal 3D** (URP)
- 추가 패키지 없음 (적 길찾기 NavMesh 는 게임 시작 때 코드로 만든다)

## 집 컴퓨터에서 처음 설정하기

1. Unity Hub → New project → **Universal 3D** 템플릿으로 새 프로젝트를 만든다.
2. 이 저장소의 `Assets/_BlindGunfight` 폴더를 새 프로젝트의 `Assets` 폴더 안에 그대로 복사한다.
3. Unity 로 돌아가면 자동으로 컴파일된다. Console 창에 빨간 에러가 없는지 확인한다.
4. 상단 메뉴 **BlindGunfight → Build Prototype Scene** 을 누른다.
   - Obstacles / Characters 레이어, 재질, 무기 6종, 요원 4명(+잠긴 칸 2개), 총알 궤적 프리팹, 창고형 테스트 맵, 조명, 플레이어, 좀비 8마리, Greywolf 4명이 자동으로 만들어진다.
   - 씬은 `Scenes/Lobby.unity`(첫 화면)와 `Scenes/Prototype.unity`(미션) 두 개가 만들어진다.
5. 열려 있는 **Lobby** 씬에서 **Play** 를 누른다. 캠페인 카드 → 요원 고르기 → 출격.

## 자동 확인 (명령줄 빌드 + 자동 촬영)

Unity 창을 열지 않고 씬 생성, 컴파일, Windows 빌드를 한 번에 합니다. PowerShell 에서 프로젝트 폴더로 이동한 뒤 실행하세요. Unity 경로는 설치된 버전에 맞게 바꿔야 합니다.

```powershell
& "C:\Program Files\Unity\Hub\Editor\<버전>\Editor\Unity.exe" -batchmode -quit -projectPath . `
  -executeMethod BlindGunfight.EditorTools.BatchTools.BuildAll -logFile build.log
```

- 컴파일 오류가 있으면 빌드가 멈추고 `build.log` 에 오류가 남습니다.
- 성공하면 `Builds/Windows/BlindGunfight.exe` 가 생깁니다. Unity 메뉴 BlindGunfight > Build Windows Player 로도 빌드할 수 있습니다.

자동 촬영은 빌드한 게임을 아래처럼 실행합니다. 로비, 요원 탭, 출격까지 자동으로 진행한 뒤, 가까운 적에게 다가가 스팟하고 조준해서 사격합니다. 약 30초(게임 시간) 뒤에 종료됩니다.

```powershell
.\Builds\Windows\BlindGunfight.exe -autocapture -screen-fullscreen 0 -screen-width 1280 -screen-height 720
```

결과는 `Builds/Windows/Capture/` 에 남습니다.

- `stills/*.png`: 로비, 요원 선택, 미션 시작, 스팟, 처치 장면, 4초마다 찍은 장면
- `frames/*.jpg`: 영상용 프레임(초당 20장). `ffmpeg -framerate 20 -i frames/f_%05d.jpg -pix_fmt yuv420p play.mp4` 로 영상을 만듭니다.
- `log.txt`: 스팟, 명중 부위, 처치, 오류 기록

## 로비와 요원

- 위쪽 탭: 플레이 / 요원 / 기록 (Q, E 로 이동). 플레이 탭 아래 SELECT MODE 카드: 캠페인, 미션 선택(잠김), 사격장, 튜토리얼(잠김)
- 요원 탭: 왼쪽 요원 목록, 가운데 큰 그림(그림이 없으면 머리글자), 오른쪽 이름/역할/소개/체력/주무기/보조무기/도구, 오른쪽 아래 출격
- 요원 정보는 `Agents/*.asset` 에서 고친다. `portrait` 에 그림을 넣으면 목록과 큰 화면에 바로 보인다.

| 요원 | 역할 | 주 무기 | 보조 무기 | 전용 도구 / 도구 | 체력 | 속도 |
| --- | --- | --- | --- | --- | --- | --- |
| Ethan Cole | 돌격 | Rifle | Pistol | 섬광탄 / 파편수류탄 | 100 | ×1.0 |
| Mira Vance | 정찰 | SMG-SD (소음기) | Pistol | 동작 감지기 / 연막탄 | 90 | ×1.1 |
| Marcus Reyes | 돌파 | Shotgun | Pistol | 돌파 폭약 / 파편수류탄 | 120 | ×0.92 |
| Lena Park | 저격 | DMR | Pistol | 표식 조준경 / 섬광탄 | 90 | ×1.0 |

도구는 아직 화면 표시만 되고 기능은 다음 단계에서 만든다.

## 조작

| 키 | 동작 |
| --- | --- |
| WASD | 이동 (화면 기준) |
| Shift | 달리기 (발소리가 커진다) |
| Ctrl 또는 C | 앉아 걷기 (소리 없음) |
| 마우스 | 바라보는 방향 = 시야 방향 |
| 우클릭 | 조준 (시야가 좁고 길어지고, 탄이 덜 퍼지고, 반동이 줄어든다) |
| 좌클릭 | 사격 |
| R | 재장전 |
| F | 칼 (소리 없음, 등 뒤에서 찌르면 즉사) |
| Q | 스포팅: 보이는 적 표시 (머리 위 마커, 시야 밖이면 마지막 위치 마커) |
| 1 / 2 | 주 무기 / 보조 무기 교체 (탄약은 무기별로 따로 남는다) |
| Esc | 로비로 돌아가기 |

## 블라인드스팟에서 따라한 것

- 55° 기울어진 원근 쿼터뷰 카메라, 마우스 쪽으로 화면이 조금 당겨진다
- 눈높이 Spot Light 시야 부채꼴. 벽이 그림자를 만들어 안 보이는 곳은 어둡고, 시야 밖 적은 숨겨진다
- 벽 윗면을 검게 칠해 단면처럼 보이게 함
- 머리 위 이름표와 체력바 (나 초록, 적 빨강)
- 총구에서 나가는 조준 레이저선 (반동이 반영된 실제 탄 방향)
- 오른쪽 아래 무기/탄약/체력, 오른쪽 키 안내, 가운데 아래 "KILL +20 PT" 문구

## 전투 규칙

- **시야**: 눈높이(1.4m)에서 판정. 1.2m 상자는 시야는 안 막고 총알은 막는다(엄폐물). 2.5m 벽은 둘 다 막는다.
- **반동**: 쏠 때마다 조준이 좌우로 튀고 연사할수록 쌓인다. 쉬면 돌아온다.
- **명중 부위**: 탄이 적 몸 중심(위에서 본 원)에서 얼마나 떨어져 지나갔는지로 정한다. 머리 ×2, 몸통 ×1, 팔다리 ×0.6, 스침 ×0.3. 화면에 HEADSHOT / BODY / LIMB / GRAZE 로 표시된다.
- 부위 경계와 배율은 `Scripts/Weapons/HitZones.cs`, 무기별 수치는 `Weapons/*.asset` 에서 바꾼다.

## 테스트할 것

- 시야 부채꼴 밖이 어둡고, 벽 뒤 적은 안 보이는가
- 총을 쏘면 좀비가 몰려오는가, 앉아서 걸으면 들키지 않는가
- 비명자(보라색)가 플레이어를 보면 주변 좀비가 몰려오는가
- Greywolf 가 소리를 확인하러 오고, 발견하면 동료를 부르는가
- Q 스포팅 마커가 적을 따라다니고, 시야 밖에서는 마지막 위치에 남는가
- 연사할 때 탄이 흩어지며 스침/팔다리 판정이 섞여 나오는가
- 적이 벽을 돌아서 길을 잘 찾아오는가 (NavMesh)

## 폴더 구조

```
Assets/_BlindGunfight/
  Scripts/Core      입력, 체력/세력, 소음, 사망 처리, XZ 평면 계산
  Scripts/Player    이동, 사격, 칼, 스포팅, 쿼터뷰 카메라
  Scripts/Weapons   무기 데이터, 총, 반동, 명중 부위, 총알 궤적, 조준 레이저
  Scripts/Vision    시야 판정, 플레이어 시야 조명, 안 보이는 적 숨기기, 스포팅 마커
  Scripts/AI        NavMesh 생성, 적 이동, 감지(눈/귀), 좀비 AI, 무장 인간 AI
  Scripts/UI        임시 HUD (이름표, 무기, 체력)
  Editor            테스트 씬 자동 생성 도구
  Art/Placeholder   스포팅 마커 이미지
```

## 문제가 생기면

- **벽 뒤까지 빛이 새어 나간다 / 그림자가 없다**: Project Settings → Quality 에서 쓰는 URP Asset 을 선택하고 Lighting → Additional Lights 의 **Cast Shadows** 를 켠다.
- **화면이 너무 어둡거나 밝다**: Player → View Light 의 Intensity, `Dim Directional Light` 의 Intensity 를 조절한다.
- **적이 안 움직인다**: Console 에 NavMesh 경고가 있는지 확인한다. NavMesh 오브젝트의 Size 가 맵을 덮는지 본다.
- **입력이 안 먹는다**: Project Settings → Player → Active Input Handling 을 `Both` 로 바꿔본다.
- 그 외 Console 에러는 메시지를 그대로 Claude 에게 보내면 된다.

## 알려진 한계 (다음 단계)

- 캐릭터는 임시 도형이다. → AI 3D 생성 모델 + Mixamo 애니메이션으로 교체 예정
- 벽 뒤에 가려진 캐릭터의 하늘색 외곽선(x-ray)은 아직 없다. → URP Renderer Feature 로 추가 예정
- 카메라 회전, 문, 수류탄, 붕대 줍기, 저장 지점은 아직 없다.
- HUD 는 임시(OnGUI)다.

## GitHub 로 연결하기

1~2번 대신, Unity 로 만든 새 프로젝트 폴더에서 아래를 실행하면 이 저장소와 연결된다.

```
git init
git remote add origin https://github.com/<계정>/<저장소>.git
git fetch origin
git checkout -t origin/main
```
