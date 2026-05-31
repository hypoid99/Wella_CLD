# Wella 일정관리 데스크탑 앱 설계 방안

> 최초 작성: 2026-05-31  
> 기술 스택: C# .NET WinForms + GDI+  
> 아키텍처: MVC 패턴 + 모듈별 독립 추가 방식

---

## 1. 프로젝트 구조

```
Wella/
├── Wella.sln
└── Wella/
    ├── Program.cs
    ├── Models/
    │   ├── CalendarEvent.cs        # 달력 일정 데이터
    │   ├── TodoItem.cs             # 할일 데이터
    │   ├── MemoItem.cs             # 메모 데이터
    │   └── CalculatorModel.cs      # 계산기 로직
    ├── Views/
    │   ├── MainForm.cs             # 메인 윈도우 (Shell)
    │   ├── Panels/
    │   │   ├── CalendarPanel.cs
    │   │   ├── TodoPanel.cs
    │   │   ├── MemoPanel.cs
    │   │   └── CalculatorPanel.cs
    │   └── Controls/
    │       ├── TopMenuBar.cs       # 상단 메뉴바 (커스텀 GDI)
    │       ├── SidebarControl.cs   # 좌측 사이드바 (커스텀 GDI)
    │       └── CalendarGrid.cs     # 달력 그리드 (커스텀 GDI)
    ├── Controllers/
    │   ├── AppController.cs        # 화면 전환 / 앱 전역 제어
    │   ├── CalendarController.cs
    │   ├── TodoController.cs
    │   ├── MemoController.cs
    │   └── CalculatorController.cs
    ├── Services/
    │   └── FileService.cs          # 텍스트 파일 I/O 공통 서비스
    └── Data/                       # 런타임 생성 데이터 폴더
        ├── calendar.txt
        ├── todos.txt
        └── memos.txt
```

---

## 2. MVC 역할 분리

| 레이어 | 역할 | 규칙 |
|---|---|---|
| **Model** | 순수 데이터 + 파일 직렬화/역직렬화 | UI 참조 금지 |
| **View** | GDI 렌더링 + 사용자 입력 이벤트 발생 | 비즈니스 로직 금지 |
| **Controller** | Model ↔ View 중재, 파일 저장 트리거 | 직접 그리기 금지 |

```
[View] 이벤트 발생
    → [Controller] 처리 결정
        → [Model] 데이터 변경
        → [FileService] 파일 저장
    → [View] Refresh() 호출
```

---

## 3. UI 레이아웃

```
┌───────────────────────────────────────────────────────┐
│  W Wella     [📅 달력]  [✓ 할일]  [📝 메모]  [🔢 계산기]  │  ← TopMenuBar (56px, GDI 커스텀)
├───────────┬───────────────────────────────────────────┤
│           │                                           │
│  📅 달력  │                                           │
│  ─────── │           ContentPanel                    │
│  ✓  할일  │         (도구별 패널이 교체됨)              │
│           │                                           │
│  📝 메모  │                                           │
│           │                                           │
│  🔢 계산기 │                                           │
│           │                                           │
│  (180px)  │                                           │
└───────────┴───────────────────────────────────────────┘
```

- **TopMenuBar**: `Panel` 상속 → `OnPaint` 오버라이드, 도구 버튼 GDI 직접 렌더
- **SidebarControl**: `Panel` 상속 → 메뉴 항목 GDI 직접 렌더, 선택 강조 애니메이션
- **ContentPanel**: `MainForm` 내 `Panel`, 도구 패널을 `Dock.Fill`로 교체

---

## 4. 파일 저장 포맷 (DB 없음)

### calendar.txt
```
# Wella Calendar Data
2026-05-31|팀 회의|주간 스탠드업|09:00|10:00
2026-06-05|생일|홍길동 생일||
```
`날짜|제목|설명|시작시간|종료시간`

### todos.txt
```
# Wella Todo Data
1|WinForms 프로젝트 생성|2026-06-01|false|HIGH
2|UI 레이아웃 구성||true|MEDIUM
```
`ID|제목|마감일|완료여부|우선순위`

### memos.txt
```
# Wella Memo Data
[MEMO:1]
TITLE=설계 초안
CREATED=2026-05-31 09:00
MODIFIED=2026-05-31 10:00
CONTENT=Wella 앱 설계 내용...
[/MEMO]
```

---

## 5. 도구별 GDI 핵심 구현 포인트

### 달력 (CalendarGrid.cs)
```
OnPaint → Graphics.DrawRectangle() 으로 날짜 셀 그리기
         → 오늘 날짜 강조 (FillRectangle 배경색)
         → 이벤트 있는 날 하단에 점(dot) 표시
         → MouseClick → 날짜 선택 처리
```

### 할일 (TodoPanel.cs)
```
CheckBox + Label 커스텀 → 완료시 취소선 텍스트
우선순위별 좌측 색상 바 (HIGH=빨강, MED=노랑, LOW=초록)
```

### 메모 (MemoPanel.cs)
```
좌측 메모 목록 + 우측 RichTextBox 에디터 분할 레이아웃
자동 저장 타이머 (3초 딜레이)
```

### 계산기 (CalculatorPanel.cs)
```
GDI로 버튼 그리드 직접 렌더링
수식 파싱은 DataTable.Compute() 또는 직접 파서 구현
```

---

## 6. 화면 전환 방식 (AppController)

```csharp
// 패널 교체 방식 - 탭 없이 깔끔하게 전환
public void SwitchTool(ToolType tool)
{
    _currentPanel?.Hide();
    _currentPanel = _panels[tool];
    _currentPanel.Dock = DockStyle.Fill;
    _currentPanel.Show();
    _mainForm.UpdateActiveMenu(tool);  // 사이드바/상단 선택 상태 갱신
}
```

---

## 7. 개발 순서 (모듈별 단계)

| Phase | 내용 | 상태 |
|---|---|---|
| Phase 1 | MainForm 셸 + TopMenuBar + SidebarControl (GDI 레이아웃) | ✅ 완료 |
| Phase 2 | CalendarGrid 커스텀 컨트롤 + CalendarController + FileService | ✅ 완료 |
| Phase 3 | TodoPanel + TodoController | 예정 |
| Phase 4 | MemoPanel + MemoController (자동저장 포함) | 예정 |
| Phase 5 | CalculatorPanel + CalculatorController | 예정 |

---

## 8. 권장 색상 팔레트

| 요소 | 색상 코드 |
|---|---|
| TopMenuBar 배경 | `#2C3E50` |
| 사이드바 배경 | `#34495E` |
| 사이드바 선택 항목 | `#1ABC9C` |
| 콘텐츠 배경 | `#F5F6FA` |
| 오늘 날짜 강조 | `#3498DB` |
| 완료된 할일 텍스트 | `#95A5A6` |

---

## 9. 향후 모듈 추가 시 체크리스트

새 도구를 추가할 때 아래 순서로 진행한다.

1. `Models/` 에 데이터 모델 클래스 추가
2. `Data/` 에 해당 도구의 `.txt` 파일 포맷 정의
3. `Services/FileService.cs` 에 Load/Save 메서드 추가
4. `Views/Panels/` 에 패널 클래스 추가 (GDI 렌더 포함)
5. `Controllers/` 에 컨트롤러 클래스 추가
6. `Controllers/AppController.cs` 의 `ToolType` enum 에 항목 추가
7. `Views/Controls/TopMenuBar.cs` 에 버튼 항목 추가
8. `Views/Controls/SidebarControl.cs` 에 메뉴 항목 추가
9. `Views/MainForm.cs` 에 패널 등록

---

## 변경 이력

| 날짜 | 내용 |
|---|---|
| 2026-05-31 | 최초 설계 방안 작성 |
| 2026-05-31 | Phase 1 완료 — MainForm / TopMenuBar / SidebarControl / 플레이스홀더 패널 4개 / FileService. net8.0-windows 타겟 |
