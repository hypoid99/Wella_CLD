# Wella 프로젝트 인수인계 문서 (Handoff)

> 최종 수정: 2026-06-01  
> 이 문서는 새 채팅 세션에서 Wella 개발을 이어받을 때 **가장 먼저 읽어야 할 문서**입니다.

---

## 1. 프로젝트 개요

| 항목 | 내용 |
|---|---|
| 앱 이름 | **Wella** — 일정관리 데스크탑 앱 |
| 기술 스택 | C# .NET 10 / WinForms / GDI+ |
| 아키텍처 | MVC 패턴 + 모듈별 독립 추가 방식 |
| 저장소 | https://github.com/hypoid99/Wella_CLD |
| 로컬 경로 | `C:\Users\BM20260102\Documents\Claude Cowork\Wella_CLD\` |
| 프로젝트 파일 | `...\Wella\Wella.csproj` |
| 빌드 명령 | `cd ...\Wella_CLD\Wella && dotnet build` |
| 실행 명령 | `dotnet run` 또는 `bin\Debug\net10.0-windows\Wella.exe` |

---

## 2. 현재 개발 상태

| Phase | 내용 | 상태 |
|---|---|---|
| Phase 1 | MainForm 셸 + TopMenuBar | ✅ 완료 |
| Phase 2 | 달력 (CalendarGrid + CalendarController + 공휴일) | ✅ 완료 |
| Phase 3 | 할일 (TodoPanel + TodoController) | ⬜ 미구현 |
| Phase 4 | 메모 (MemoPanel + MemoController) | ⬜ 미구현 |
| Phase 5 | 계산기 (CalculatorPanel + CalculatorController) | ⬜ 미구현 |

**다음 작업: Phase 3 — 할일(Todo) 모듈 구현**

---

## 3. 파일 구조 (현재 상태)

```
Wella_CLD\
├── DESIGN.md
├── HANDOFF.md                      ← 이 파일
├── TROUBLESHOOTING.md
├── Wella.sln
└── Wella\
    ├── Wella.csproj                (net10.0-windows)
    ├── Program.cs
    ├── Models\
    │   └── CalendarEvent.cs        ✅ 완료
    ├── Controllers\
    │   ├── AppController.cs        ✅ 완료  (ToolType enum 포함)
    │   └── CalendarController.cs   ✅ 완료
    ├── Services\
    │   ├── FileService.cs          ✅ 완료  (달력 Load/Save 포함)
    │   └── KoreanHolidays.cs       ✅ 완료  (공휴일 정적 딕셔너리)
    └── Views\
        ├── MainForm.cs             ✅ 완료
        ├── MainForm.Designer.cs    ✅ 완료
        ├── Controls\
        │   ├── TopMenuBar.cs       ✅ 완료  (GDI 커스텀, ResizeRedraw=true)
        │   └── CalendarGrid.cs     ✅ 완료  (GDI 월간 달력 + 공휴일 표시)
        └── Panels\
            ├── CalendarPanel.cs    ✅ 완료  (날짜 클릭 → CalendarDayDialog)
            ├── TodoPanel.cs        ⬜ 플레이스홀더만 있음
            ├── MemoPanel.cs        ⬜ 플레이스홀더만 있음
            └── CalculatorPanel.cs  ⬜ 플레이스홀더만 있음
```

> **참고**: `SidebarControl.cs`는 2026-06-01에 제거됨. 필요 시 git log로 복원 가능.

---

## 4. 달력 모듈 현재 구조 (Phase 2 완료 상태)

### UI 구조
- **TopMenuBar** (56px): 도구 전환 버튼 4개 (달력/할일/메모/계산기), 우측 정렬
- **CalendarPanel**: TopMenuBar 아래 전체 영역을 달력이 꽉 채움 (사이드바/사이드패널 없음)
  - 내부 네비바(48px): `< >` 월 이동 버튼 + 현재 월 표시
  - CalendarGrid: 나머지 전체 영역

### 날짜 클릭 동작
날짜 셀 클릭 → `CalendarDayDialog` 모달 창 열림  
창 안에서 해당 날짜의 일정 목록 조회 + 새 일정 추가(제목/메모) + 선택 항목 삭제 가능.  
창 닫힌 후 CalendarGrid 자동 갱신.

### 공휴일 표시
- 양력 고정 공휴일 8종 + 음력 기반(설날·추석 연휴, 부처님오신날) 2020~2030 수록
- 공휴일 날짜: 빨간색 + 날짜 숫자 아래 공휴일 이름 (7.5pt)
- 양력 고정 우선 적용 (2025-05-05 → "어린이날")

---

## 5. 아키텍처 핵심 규칙

### MVC 흐름
```
[View] 이벤트 발생
    → [Controller] 처리 결정
        → [Model] 데이터 변경
        → [FileService] 파일 저장
    → [View] Refresh() 호출
```

### 레이어별 금지사항
| 레이어 | 금지 |
|---|---|
| Model | UI 참조 금지 |
| View | 비즈니스 로직 금지 |
| Controller | 직접 그리기 금지 |

### 화면 전환 방식
`AppController.SwitchTool(ToolType)` 호출 → 현재 패널 Hide → 다음 패널 BringToFront → Show  
AppController / TopMenuBar는 **수정 불필요** (이미 4개 도구 등록됨).

---

## 6. 색상 팔레트 (전체 공통)

```csharp
TopMenuBar 배경   : Color.FromArgb(44,  62,  80)   // #2C3E50
콘텐츠 배경       : Color.FromArgb(245, 246, 250)  // #F5F6FA
액센트 (선택)     : Color.FromArgb(26, 188, 156)   // #1ABC9C
일요일/공휴일     : Color.FromArgb(210,  70,  70)
토요일            : Color.FromArgb( 60, 110, 220)
완료 텍스트        : Color.FromArgb(149, 165, 166)  // #95A5A6
```

---

## 7. 파일 저장 포맷

### calendar.txt (완료)
```
# Wella Calendar Data
ID|YYYY-MM-DD|제목|설명|시작시간|종료시간
1|2026-05-31|팀 회의|주간 스탠드업|09:00|10:00
```

### todos.txt (Phase 3에서 구현)
```
# Wella Todo Data
ID|제목|마감일(YYYY-MM-DD)|완료여부(true/false)|우선순위(HIGH/MEDIUM/LOW)
1|보고서 작성|2026-06-01|false|HIGH
```

### memos.txt (Phase 4에서 구현)
```
# Wella Memo Data
[MEMO:1]
TITLE=메모 제목
CREATED=2026-05-31 09:00
MODIFIED=2026-05-31 10:00
CONTENT=내용
[/MEMO]
```

---

## 8. 반드시 지켜야 할 GDI 코딩 규칙

```csharp
// ✅ Font/Brush/Pen 은 반드시 필드로 캐싱
private readonly Font       _font  = new("Segoe UI", 10f);
private readonly SolidBrush _brush = new(Color.FromArgb(44, 62, 80));

// ❌ OnPaint 안에서 new 절대 금지 (시스템 다운 유발)

// ✅ 이모지 폰트 금지, 안전한 유니코드 기호 사용
// ❌ "Segoe UI Emoji"  ✅ "▦", "✔", "▤", "#"

// ✅ GDI 커스텀 컨트롤 생성자 필수 설정
SetStyle(ControlStyles.OptimizedDoubleBuffer |
         ControlStyles.AllPaintingInWmPaint  |
         ControlStyles.UserPaint, true);
ResizeRedraw = true;   // ← 리사이즈 시 전체 재드로우 (잔상 방지)

// ✅ Dispose 에서 반드시 해제
protected override void Dispose(bool disposing)
{
    if (disposing) { _font.Dispose(); _brush.Dispose(); }
    base.Dispose(disposing);
}
```

---

## 9. 레이아웃 규칙

```csharp
// ✅ 복잡한 레이아웃은 수동 배치 (Dock 중첩 금지)
private void ManualLayout()
{
    _navBar.SetBounds(0,   0,   Width, NavH);
    _grid  .SetBounds(0,   NavH, Width, Height - NavH);
}
protected override void OnResize(EventArgs e) { base.OnResize(e); ManualLayout(); }
protected override void OnLoad  (EventArgs e) { base.OnLoad(e);   ManualLayout(); }

// ✅ Form 초기화는 Load 이벤트에서
Load += (_, _) => _appController.Initialize();
// ❌ 생성자에서 Initialize() 직접 호출 금지
```

---

## 10. Phase 3 할일 모듈 구현 가이드

### 구현할 기능
- 할일 목록 표시 (제목 / 마감일 / 우선순위 / 완료여부)
- 추가 / 완료 체크 / 삭제
- 우선순위별 좌측 색상 바 (HIGH=빨강 / MEDIUM=노랑 / LOW=초록)
- 완료 항목 취소선 처리
- `Data/todos.txt`에 저장

### UI 레이아웃 (참고)
```
┌──────────────────────────────────────────────┐
│  [+ 할일 추가]   [전체] [미완료] [완료]  필터  │  ← 상단 툴바 (48px)
├──────────────────────────────────────────────┤
│  □  보고서 작성          2026-06-01   ■ HIGH  │
│  □  회의 준비            2026-06-02   ■ MED   │
│  ☑  자료 정리 (취소선)                ■ LOW   │
└──────────────────────────────────────────────┘
```

### 추가할 파일
```
Models/TodoItem.cs
Controllers/TodoController.cs
Views/Panels/TodoPanel.cs       ← 플레이스홀더 교체
Services/FileService.cs         ← LoadTodos / SaveTodos 추가
```

---

## 11. 새 모듈 추가 절차 (Phase 3~5 공통)

```
① Models/         → 데이터 모델 클래스 (ToLine / FromLine 포함)
② Services/       → FileService.cs 에 Load/Save 메서드 추가
③ Controllers/    → CRUD 컨트롤러 클래스
④ Views/Panels/   → 패널 클래스 (플레이스홀더 교체)
   └ 레이아웃은 반드시 OnResize + SetBounds 수동 배치 사용
⑤ 필요 시 Views/Controls/ → 커스텀 GDI 컨트롤
```

AppController / TopMenuBar는 **수정 불필요** (이미 4개 도구 등록됨).

---

## 12. 참고 문서

| 파일 | 내용 |
|---|---|
| `DESIGN.md` | 전체 설계 방안 (색상 팔레트, 파일 포맷, 개발 순서) |
| `TROUBLESHOOTING.md` | 개발 중 발생한 문제 12가지 및 해결법 |
| `HANDOFF.md` | 이 문서 |

---

## 13. 시작 전 확인 명령어

```powershell
cd "C:\Users\BM20260102\Documents\Claude Cowork\Wella_CLD"
git pull
cd Wella
dotnet build
dotnet run
```

---

## 변경 이력

| 날짜 | 내용 |
|---|---|
| 2026-05-31 | 최초 작성 (Phase 1~2 완료 기준) |
| 2026-06-01 | Phase 2 UI 개편 반영 — 사이드바 제거, 날짜 클릭 다이얼로그, 공휴일 표시, TopMenuBar ResizeRedraw 추가 |
