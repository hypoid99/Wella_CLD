# Wella 개발 중 발생한 문제 및 해결 기록

> 작성일: 2026-05-31  
> 목적: 동일한 실수 반복 방지 / 다음 모듈 개발 시 참고

---

## 1. FontStyle.Light 없음

### 증상
```
error CS0117: 'FontStyle'에는 'Light'에 대한 정의가 포함되어 있지 않습니다.
error CS1503: 1 인수: 'string'에서 'System.Drawing.FontFamily'(으)로 변환할 수 없습니다.
```

### 원인
WinForms `System.Drawing.FontStyle` 에는 `Light` 가 없다.  
유효한 값: `Regular`, `Bold`, `Italic`, `Underline`, `Strikeout`

### 해결
```csharp
// ❌ 잘못된 코드
new Font("Segoe UI", 22f, FontStyle.Light)

// ✅ 수정 코드
new Font("Segoe UI", 22f, FontStyle.Regular)
```

---

## 2. .NET 타겟 버전 불일치

### 증상
```
warning NETSDK1138: 대상 프레임워크 'net6.0-windows'은(는) 지원되지 않습니다.
```

### 원인
프로젝트 `.csproj` 에 `net6.0-windows` 로 작성했으나  
설치된 SDK는 .NET 10.

### 해결
```xml
<!-- ❌ -->
<TargetFramework>net6.0-windows</TargetFramework>

<!-- ✅ 설치된 SDK 버전과 맞춤 -->
<TargetFramework>net10.0-windows</TargetFramework>
```

> **교훈**: 개발 시작 전 `dotnet --version` 으로 설치된 버전 먼저 확인

---

## 3. GDI OnPaint 안에서 Font/Brush 객체 반복 생성 → 시스템 다운

### 증상
- 마우스를 상단 메뉴바 / 사이드바 위로 움직이면 PC 가 멈추거나 다운됨
- `"Segoe UI Emoji"` 폰트 사용 시 특히 심각

### 원인
```csharp
// ❌ OnPaint(마우스 이동마다 호출) 안에서 매번 Font 객체 생성
protected override void OnPaint(PaintEventArgs e)
{
    using var iconFont = new Font("Segoe UI Emoji", 13f);  // 매번 생성/해제
    using var labelFont = new Font("Segoe UI", 8.5f);      // 매번 생성/해제
    // ...
}
```
- `OnMouseMove` → `Invalidate()` → `OnPaint` 루프가 매우 빠르게 반복
- 이모지 폰트(`Segoe UI Emoji`)는 GDI+ 렌더링 비용이 극히 높음
- GDI 핸들 고갈 → 시스템 불안정

### 해결
```csharp
// ✅ Font/Brush/Pen 을 클래스 필드로 캐싱 (OnPaint 에서 new 금지)
private readonly Font       _iconFont  = new("Segoe UI", 13f);
private readonly SolidBrush _accBrush  = new(AccentColor);

protected override void OnPaint(PaintEventArgs e)
{
    // 캐시된 객체 사용 — new 없음
    g.DrawString(icon, _iconFont, _accBrush, ...);
}

// ✅ Dispose 에서 해제
protected override void Dispose(bool disposing)
{
    if (disposing) { _iconFont.Dispose(); _accBrush.Dispose(); }
    base.Dispose(disposing);
}
```

> **교훈**: GDI 커스텀 컨트롤에서 **Font/Brush/Pen 은 절대 OnPaint 안에서 new 금지**  
> **교훈**: 이모지 폰트(`Segoe UI Emoji`) 사용 금지 → 일반 유니코드 기호(`▦ ✔ ▤ #`) 사용

---

## 4. 콘텐츠 패널에 달력이 안 보임 (초기화 순서 문제)

### 증상
앱 실행 시 달력 패널 영역이 비어 있음

### 원인
`AppController.Initialize()` 를 Form 생성자에서 호출 →  
Form 이 화면에 그려지기 전(레이아웃 미완료) 상태에서 패널 크기 = 0

### 해결
```csharp
// ❌ 생성자에서 즉시 초기화
public MainForm()
{
    BuildLayout();
    _appController = new AppController(this);
    _appController.Initialize();  // 레이아웃 미완료 상태
}

// ✅ Load 이벤트로 지연
public MainForm()
{
    BuildLayout();
    _appController = new AppController(this);
    Load += (_, _) => _appController.Initialize();  // 레이아웃 완료 후 실행
}
```

---

## 5. NullReferenceException — 초기화 순서 역전

### 증상
```
Unhandled exception. System.NullReferenceException
   at CalendarPanel.UpdateMonthLabel()
   at CalendarPanel.BuildNav()
   at CalendarPanel.BuildUI()
```

### 원인
`BuildUI()` 에서 `BuildNav()` 를 먼저 호출했는데,  
`BuildNav()` 내부의 `UpdateMonthLabel()` 이 `_grid` 를 참조함.  
`_grid` 는 그 다음에 생성되어 null 상태.

```csharp
// ❌ 잘못된 순서
private void BuildUI()
{
    var nav = BuildNav();   // ← 내부에서 _grid 참조
    // ...
    _grid = new CalendarGrid();   // ← _grid 가 여기서 생성됨 (너무 늦음)
}
```

### 해결
```csharp
// ✅ 참조되는 객체를 먼저 생성
private void BuildUI()
{
    _grid = new CalendarGrid();   // ← 먼저 생성
    var nav = BuildNav();          // ← 이제 _grid 참조 가능
}
```

> **교훈**: 메서드 내에서 다른 필드를 참조하는 메서드를 호출할 때는  
> **피참조 필드의 초기화 순서를 반드시 확인**

---

## 6. Dock 중첩 레이아웃 버그 — 요일 헤더 없음 / 열 잘림

### 증상
- 달력 요일 헤더(일월화수목금토)가 안 보임
- 토요일 열이 잘려 6개 열만 표시됨

### 원인
중첩 `Panel` 에서 `Dock` 레이아웃이 `body` 패널의 크기가 0일 때 실행됨.  
이후 `body` 가 올바른 크기를 받아도 자식 컨트롤 레이아웃이 재계산되지 않아  
CalendarGrid 가 잘못된 크기(전체 너비)를 사용.

```
body(크기=0) 에 자식 추가 → 레이아웃 실행(크기=0 기준)
→ CalendarPanel 에 body 추가 → body 크기 올바르게 변경
→ but 자식들 레이아웃 재실행 안 됨 → 잘못된 크기 유지
```

### 해결
**Dock 방식 폐기 → `OnResize` + `SetBounds` 수동 배치**

```csharp
// ✅ 컨트롤 크기/위치를 직접 지정
private void ManualLayout()
{
    int w = ClientSize.Width;
    int h = ClientSize.Height;
    _navBar.SetBounds(0,        0,    w,        NavH);
    _side  .SetBounds(w - SideW, NavH, SideW,   h - NavH);
    _grid  .SetBounds(0,        NavH, w - SideW, h - NavH);
}

protected override void OnResize(EventArgs e)
{
    base.OnResize(e);
    ManualLayout();
}
```

> **교훈**: GDI 커스텀 컨트롤이 포함된 복잡한 레이아웃은  
> **Dock 중첩보다 수동 배치(SetBounds + OnResize)가 훨씬 안정적**

---

## 7. GDI 커스텀 컨트롤 리사이즈 후 재드로우 안 됨

### 증상
창 크기 변경 시 CalendarGrid 내용이 갱신되지 않음

### 해결
```csharp
// ✅ 생성자에 추가
ResizeRedraw = true;

// ✅ 또는 OnResize 오버라이드
protected override void OnResize(EventArgs e)
{
    base.OnResize(e);
    Invalidate();
}
```

---

## 8. git push 시 bin / obj 폴더 올라감

### 증상
`git add .` 후 빌드 결과물(`.exe`, `.dll`, `.pdb` 등)이 GitHub 에 함께 업로드됨

### 해결
프로젝트 루트에 `.gitignore` 생성:

```gitignore
bin/
obj/
Data/
*.txt
!README.md
.vs/
*.user
err.txt
```

> **교훈**: **git init 직후 가장 먼저 `.gitignore` 를 만들 것**

---

## 요약 — 다음 모듈 개발 시 체크리스트

| # | 항목 | 확인 |
|---|---|---|
| 1 | `FontStyle.Light` 사용 금지 → `Regular` 사용 | |
| 2 | `.csproj` TargetFramework 를 설치된 SDK 버전과 맞출 것 | |
| 3 | GDI `OnPaint` 안에서 `new Font/Brush/Pen` 금지 → 필드로 캐싱 | |
| 4 | 이모지 폰트(`Segoe UI Emoji`) 사용 금지 | |
| 5 | Form 초기화는 생성자가 아닌 `Load` 이벤트에서 실행 | |
| 6 | 메서드 간 참조 필드의 초기화 순서 확인 | |
| 7 | 복잡한 레이아웃은 Dock 중첩 대신 `SetBounds + OnResize` 수동 배치 | |
| 8 | GDI 커스텀 컨트롤에 `ResizeRedraw = true` 설정 | |
| 9 | git init 후 즉시 `.gitignore` 생성 (`bin/`, `obj/` 제외) | |

---

## 변경 이력

| 날짜 | 내용 |
|---|---|
| 2026-05-31 | Phase 1~2 개발 중 발생한 문제 최초 기록 |
