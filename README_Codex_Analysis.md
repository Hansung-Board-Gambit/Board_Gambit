# Board_Gambit Codex Analysis

이 문서는 현재 Unity 프로젝트 구조를 빠르게 파악하기 위한 분석 메모입니다. 코드 동작은 변경하지 않았고, 파일/씬/스크립트 역할을 정리했습니다.

## 1. 주요 폴더 구조

- `Assets/Scenes`
  - Unity 씬 파일들이 위치합니다.
  - 현재 확인된 씬: `Taeyoung.unity`, `Jinsoo2.unity`, `Junseo.unity`, `Minhyeok.unity`

- `Assets/Scripts`
  - 프로젝트의 주요 게임 로직 스크립트가 위치합니다.
  - 로비, 네트워크, UI, 준비 단계, 배치 시스템 등이 이 폴더에 섞여 있습니다.

- `Assets/Scripts/Player`
  - 플레이어 이동, 체력, 무기 장착, 무기 공통 베이스 로직이 위치합니다.

- `Assets/Scripts/Weapon`
  - 개별 무기 구현체가 위치합니다.
  - 예: `Hammer`, `Grappling`, `PaintGun`, `TimerTrap`, `FlameGun`, `BounceGun`

- `Assets/Scripts/Weapon/Projectile`
  - 무기에서 생성되는 투사체/장판류 네트워크 오브젝트 스크립트가 위치합니다.

- `Assets/Scripts/SpawnTest`
  - Fusion 기본 스폰 테스트로 보이는 샘플성 코드가 위치합니다.

- `Assets/ScriptObject`
  - 무기 데이터용 ScriptableObject 클래스가 위치합니다.

- `Assets/ScriptObject/SO`
  - 실제 무기 데이터 에셋들이 위치합니다.

- `Assets/Prefabs`
  - 네트워크 플레이어, 로비 상태, 무기 프리팹 등이 위치합니다.

- `Assets/Prefabs/Weapons`
  - 무기 및 투사체 프리팹이 위치합니다.

- `Assets/MapObjectPrefab`
  - 보드 위에 배치 가능한 오브젝트 프리팹으로 보이는 `Cube`, `Cylinder`, `Capsule` 등이 위치합니다.

- `Assets/Photon/Fusion`
  - Photon Fusion 패키지 코드, 런타임, 에디터, 설정 파일이 포함되어 있습니다.

- `Assets/Editor`
  - 에디터 전용 도구가 위치합니다.
  - 현재 `GameSceneFrameBuilder..cs`가 준비 단계 씬 UI/오브젝트 구조를 생성하는 도구로 보입니다.

- `ProjectSettings`
  - Unity 프로젝트 설정, Build Settings, 렌더 파이프라인, 입력/물리/패키지 설정 등이 위치합니다.

- `Packages`
  - Unity Package Manager 의존성 정의가 위치합니다.

## 2. 주요 씬 목록

- `Assets/Scenes/Taeyoung.unity`
  - `ProjectSettings/EditorBuildSettings.asset`에서 현재 유일하게 활성화된 씬입니다.
  - 플레이어/무기/전투 테스트용 씬처럼 보입니다.

- `Assets/Scenes/Jinsoo2.unity`
  - 로비/방 생성/방 참가/옵션 UI가 들어 있는 씬으로 보입니다.
  - `NetworkManager`, `PlayerSpawner`, `LobbyState`, 로비 버튼 이벤트들이 배치되어 있습니다.

- `Assets/Scenes/Junseo.unity`
  - 준비 단계 게임 씬으로 보입니다.
  - 오브젝트 배치, 스폰 위치 배치, 장비 선택 UI, `GameFlow`, `BoardManager`, `RoundManager`, `PlayerManager` 같은 오브젝트가 포함되어 있습니다.
  - `LobbyState.StartGame()`에서 `Runner.LoadScene("Junseo")`로 로드하는 대상입니다.

- `Assets/Scenes/Minhyeok.unity`
  - 기본 카메라/라이트 중심의 빈 작업 씬에 가까워 보입니다.

- 주의
  - `ProjectSettings/EditorBuildSettings.asset`에는 `Assets/Scenes/Jinsoo.unity`가 등록되어 있지만 실제 파일은 현재 존재하지 않습니다.
  - `Jinsoo2.unity`, `Junseo.unity`는 BuildSettings에 등록되어 있지만 비활성화 상태입니다.

## 3. 주요 스크립트 목록과 역할 추정

### 네트워크/로비

- `Assets/Scripts/NetworkManager.cs`
  - Fusion `NetworkRunner` 생성 및 Host/Client 시작을 담당합니다.
  - 방 코드 생성, 방 참가, 로비 UI 갱신, 준비 상태 버튼, 게임 시작 버튼, 입력 수집을 처리합니다.
  - `INetworkRunnerCallbacks`를 구현합니다.

- `Assets/Scripts/Lobbystate.cs`
  - Fusion `NetworkBehaviour` 기반 로비 상태입니다.
  - `guestReady`, `gameValue`, `hostName`, `guestName`을 `[Networked]`로 동기화합니다.
  - Host 권한에서 `Runner.LoadScene("Junseo")`를 호출합니다.

- `Assets/Scripts/NetworkInputData.cs`
  - Fusion 입력 구조체입니다.
  - 이동 방향, yaw/pitch, 점프, 달리기, 앉기, 무기 버튼 입력을 담습니다.

- `Assets/Scripts/PlayerSpawner.cs`
  - `NetworkPrefabRef playerPrefab`을 이용해 플레이어를 랜덤 위치에 `runner.Spawn`합니다.
  - 현재 `NetworkManager.OnPlayerJoined()`와 직접 연결되어 있지는 않습니다.

- `Assets/Scripts/SpawnTest/BasicSpawnerTest.cs`
  - Fusion 기본 스폰 테스트 코드로 보입니다.
  - 현재 메인 로비/게임 흐름과는 별도의 실험용 코드일 가능성이 큽니다.

### 플레이어

- `Assets/Scripts/Player/Player.cs`
  - Fusion `NetworkBehaviour` 기반 플레이어 이동/시점/카메라 제어입니다.
  - 1인칭 카메라, 마우스 회전, WASD 이동, 점프, 달리기, 앉기, 넉백, 페인트 트레일 디버프를 처리합니다.

- `Assets/Scripts/Player/PlayerHealth.cs`
  - 네트워크 HP와 스턴 상태를 관리합니다.
  - 데미지/스턴은 RPC로 StateAuthority에 전달됩니다.

- `Assets/Scripts/Player/PlayerWeapon.cs`
  - 플레이어의 무기 장착과 입력 전달을 담당합니다.
  - `WeaponData`의 `weaponPrefab`을 `Runner.Spawn`하고, `NetworkObject` 변경을 렌더 콜백으로 처리합니다.

- `Assets/Scripts/Player/WeaponBase.cs`
  - 모든 무기의 추상 베이스 클래스입니다.
  - 탄약, 장전, 쿨타임, 카메라 고정 타이머, UI 업데이트 공통 로직을 갖습니다.

### 무기/투사체

- `Assets/Scripts/Weapon/BounceGun.cs`
  - 튕기는 탄환 계열 무기로 보입니다.

- `Assets/Scripts/Weapon/FlameGun.cs`
  - 화염 투사체/화염 장판을 생성하는 무기로 보입니다.

- `Assets/Scripts/Weapon/Gauntlet.cs`
  - 근접/돌진/타격 계열 무기로 보입니다.

- `Assets/Scripts/Weapon/Grappling.cs`
  - 갈고리 이동 및 충전 상태를 네트워크로 관리하는 무기로 보입니다.

- `Assets/Scripts/Weapon/Hammer.cs`
  - 망치 공격, 돌진/찍기 계열 행동을 처리하는 무기로 보입니다.

- `Assets/Scripts/Weapon/MagicMirror.cs`
  - 은신/홀드/충전 상태를 가진 무기로 보입니다.

- `Assets/Scripts/Weapon/PaintGun.cs`
  - 페인트 장판/속도 효과와 관련된 무기로 보입니다.

- `Assets/Scripts/Weapon/RechargeableLaser.cs`
  - 충전 게이지 기반 레이저 무기로 보입니다.

- `Assets/Scripts/Weapon/SelfieStick.cs`
  - 근접 타격 계열 무기로 보입니다.

- `Assets/Scripts/Weapon/TimerTrap.cs`
  - 설치형 타이머 폭탄/트랩을 생성하는 무기로 보입니다.

- `Assets/Scripts/Weapon/Projectile/BounceProjectile.cs`
  - 바운스 탄환 네트워크 이동/충돌 처리로 보입니다.

- `Assets/Scripts/Weapon/Projectile/FlameProjectile.cs`
  - 화염 투사체 이동 및 장판 생성으로 보입니다.

- `Assets/Scripts/Weapon/Projectile/FlameArea.cs`
  - 화염 장판 지속시간/피해 타이머 처리로 보입니다.

- `Assets/Scripts/Weapon/Projectile/PaintArea.cs`
  - 페인트 장판 지속시간 및 속도 버프 소유자 정보를 관리합니다.

### 준비 단계/배치

- `Assets/Scripts/PrepPhaseFlowUI.cs`
  - 준비 단계 UI 플로우를 코루틴으로 진행합니다.
  - 오브젝트 배치, 스폰 배치, 장비 선택 순서로 패널을 전환합니다.

- `Assets/Scripts/PrepDataStore.cs`
  - 준비 단계에서 배치한 오브젝트와 스폰 위치를 저장하는 로컬 데이터 저장소입니다.

- `Assets/Scripts/PlacementManager.cs`
  - 보드 위 오브젝트 배치를 담당합니다.
  - 프리뷰 생성, 그리드 스냅, 포인트 차감, 충돌 검사, 배치 데이터 저장을 처리합니다.

- `Assets/Scripts/SpawnPlacementManager.cs`
  - 내 스폰/상대 스폰 위치 배치를 담당합니다.
  - 보드 위 클릭 위치를 스냅하고 마커를 갱신합니다.

- `Assets/Scripts/PlaceableObject.cs`
  - 배치 가능한 오브젝트의 footprint, yOffset, prefabId 정보를 정의합니다.

### UI/기타

- `Assets/Scripts/PlayerUI.cs`
  - 플레이어 HP/탄약 UI 갱신용 싱글톤입니다.

- `Assets/Scripts/UIManager.cs`
  - 로비/옵션/종료 경고 패널 전환을 담당합니다.
  - 클래스명은 `UIManger`로 오타가 있습니다.

- `Assets/Scripts/GuestUI.cs`
  - 게스트 Ready 버튼용 간단한 래퍼로 보입니다.

- `Assets/Scripts/ButtonHoverUI.cs`
  - 버튼 hover UI 효과 처리로 보입니다.

- `Assets/Editor/GameSceneFrameBuilder..cs`
  - 에디터 메뉴에서 준비 단계 씬 프레임을 생성하는 도구입니다.
  - `Managers`, `World`, `PrepUI`, `OverlayUI`, `HUDUI` 같은 씬 구조와 UI 패널을 생성합니다.

## 4. Photon Fusion 또는 네트워크 관련 파일 목록

### 프로젝트 코드

- `Assets/Scripts/NetworkManager.cs`
- `Assets/Scripts/Lobbystate.cs`
- `Assets/Scripts/NetworkInputData.cs`
- `Assets/Scripts/PlayerSpawner.cs`
- `Assets/Scripts/SpawnTest/BasicSpawnerTest.cs`
- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Player/PlayerHealth.cs`
- `Assets/Scripts/Player/PlayerWeapon.cs`
- `Assets/Scripts/Player/WeaponBase.cs`
- `Assets/Scripts/Weapon/*.cs`
- `Assets/Scripts/Weapon/Projectile/*.cs`

### 네트워크 프리팹

- `Assets/Prefabs/Player.prefab`
- `Assets/Prefabs/Lobbystate.prefab`
- `Assets/Prefabs/Weapons/*.prefab`

### Fusion 패키지/설정

- `Assets/Photon/Fusion/Runtime`
- `Assets/Photon/Fusion/Editor`
- `Assets/Photon/Fusion/CodeGen`
- `Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion`
- `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`
- `Assets/Photon/PhotonLibs/WebSocket`
- `ProjectSettings/MultiplayerManager.asset`

## 5. UI 관련 파일 목록

### UI 스크립트

- `Assets/Scripts/NetworkManager.cs`
  - 로비 UI 참조와 갱신 로직을 많이 포함합니다.

- `Assets/Scripts/UIManager.cs`
  - 로비 패널, 옵션 패널, 종료 경고 패널 전환.

- `Assets/Scripts/GuestUI.cs`
  - 게스트 Ready 버튼 처리.

- `Assets/Scripts/PlayerUI.cs`
  - HP/Ammo 표시.

- `Assets/Scripts/PrepPhaseFlowUI.cs`
  - 준비 단계 패널 전환, 타이머 fill, turn intro overlay 처리.

- `Assets/Scripts/ButtonHoverUI.cs`
  - 버튼 hover 처리.

- `Assets/Scripts/PlacementManager.cs`
  - 오브젝트 배치 UI 버튼과 포인트 텍스트 연동.

- `Assets/Scripts/SpawnPlacementManager.cs`
  - 스폰 배치 UI 버튼 연동.

### UI가 포함된 주요 씬

- `Assets/Scenes/Jinsoo2.unity`
  - 초기 화면, 로비, 옵션, JoinPanel, WarningPanel 등.

- `Assets/Scenes/Junseo.unity`
  - 준비 단계 UI: `Prep_ObjectPlacementPanel`, `Prep_SpawnPlacementPanel`, `Prep_RandomEquipPanel`, `Overlay_TurnIntro`, `HUD_InGame`.

### UI 생성 도구

- `Assets/Editor/GameSceneFrameBuilder..cs`
  - 준비 단계 UI를 에디터에서 재생성하는 도구입니다.

## 6. 현재 구조에서 기능 구현 시 주의해야 할 점

- BuildSettings와 코드의 씬 흐름이 어긋나 있습니다.
  - 현재 활성화된 씬은 `Taeyoung.unity`뿐입니다.
  - 로비 시작 코드는 `Junseo` 씬을 로드합니다.
  - `Jinsoo2.unity`와 `Junseo.unity`를 실제 플레이 흐름에 쓸 계획이라면 BuildSettings 활성화와 시작 씬 정책을 먼저 정리해야 합니다.

- `EditorBuildSettings.asset`에 없는 씬 파일이 등록되어 있습니다.
  - `Assets/Scenes/Jinsoo.unity`가 등록되어 있지만 실제 파일은 없습니다.

- 플레이어 스폰 흐름이 아직 완전히 연결되어 있지 않습니다.
  - `PlayerSpawner`는 존재하지만 `NetworkManager.OnPlayerJoined()`에서 호출되지 않습니다.
  - 게임 시작 후 `Junseo` 씬에서 플레이어를 언제/어디에 스폰할지 연결 작업이 필요합니다.

- 준비 단계 데이터는 네트워크 동기화되지 않습니다.
  - `PrepDataStore`, `PlacementManager`, `SpawnPlacementManager`는 일반 `MonoBehaviour` 중심입니다.
  - Host/Guest 모두에게 같은 배치 결과가 보여야 한다면 Fusion `NetworkBehaviour`, RPC, 또는 Host 권한 기반 확정 로직이 필요합니다.

- Fusion 오브젝트는 `Instantiate` 대신 `Runner.Spawn`을 써야 하는 경우가 많습니다.
  - 플레이어, 무기, 투사체, 장판 등은 `NetworkObject` 기반입니다.
  - 네트워크 동기화가 필요한 오브젝트를 일반 `Instantiate`로 생성하면 다른 클라이언트에 복제되지 않을 수 있습니다.

- 싱글톤/정적 상태에 주의해야 합니다.
  - `LobbyState.Instance`, `PlayerUI.instance`, `Player.NetworkedYaw`, `Player.NetworkedPitch`가 있습니다.
  - 씬 전환, 재접속, 여러 플레이어 입력 상황에서 초기화 순서와 값 공유 문제가 생길 수 있습니다.

- UI 참조가 씬 오브젝트에 많이 의존합니다.
  - `NetworkManager`는 다수의 TMP/UI 오브젝트를 인스펙터 참조로 들고 있습니다.
  - 씬 복사/리네임/프리팹화 시 누락 참조가 발생하기 쉽습니다.

- `Lobbystate.cs` 파일명과 클래스명이 다릅니다.
  - 파일명은 `Lobbystate.cs`, 클래스명은 `LobbyState`입니다.
  - Unity에서는 동작 가능하지만 검색/관리 시 혼동될 수 있습니다.

- `UIManager.cs` 내부 클래스명이 `UIManger`입니다.
  - 오타로 보이며, 컴포넌트 검색이나 리팩터링 때 주의가 필요합니다.

- 일부 한글 주석/텍스트가 깨져 있습니다.
  - `PrepPhaseFlowUI.cs`, `GameSceneFrameBuilder..cs` 등에서 인코딩이 깨진 주석/문자열이 보입니다.
  - 기능에는 직접 영향이 없을 수 있지만 문서화/유지보수 시 정리가 필요합니다.

- 무기 데이터는 ScriptableObject와 프리팹이 강하게 연결되어 있습니다.
  - `WeaponData.weaponPrefab`을 통해 네트워크 무기가 생성됩니다.
  - 새 무기를 추가할 때는 스크립트, 프리팹, SO, Fusion prefab 등록 상태를 함께 확인해야 합니다.

## 7. 다음에 구현하기 좋은 작은 작업 후보 5개

1. BuildSettings 정리
   - 실제 시작 씬과 게임 씬을 정하고 `Jinsoo2.unity`, `Junseo.unity` 활성화 여부를 정리합니다.
   - 존재하지 않는 `Jinsoo.unity` 등록도 제거하거나 실제 파일을 복구합니다.

2. 게임 시작 후 플레이어 스폰 연결
   - `Junseo` 씬 로드 완료 후 Host가 `PlayerSpawner`를 통해 각 플레이어를 스폰하도록 연결합니다.
   - 이후 `PrepDataStore`의 스폰 위치와 연결할 수 있습니다.

3. 준비 단계 데이터 확정 흐름 만들기
   - 오브젝트 배치/스폰 배치 결과를 Host 기준으로 저장하고, 다음 단계에서 재사용할 수 있게 인터페이스를 정리합니다.

4. 로비/게임 씬 흐름 문서화 또는 enum화
   - 현재 `Runner.LoadScene("Junseo")`처럼 문자열 기반입니다.
   - 씬 이름 상수 또는 간단한 SceneFlow 관리 클래스를 두면 실수를 줄일 수 있습니다.

5. UI 참조 누락 방지 체크 추가
   - `NetworkManager`, `PrepPhaseFlowUI`, `PlacementManager`, `SpawnPlacementManager`의 주요 인스펙터 참조가 비어 있을 때 명확한 경고를 출력하도록 정리합니다.
