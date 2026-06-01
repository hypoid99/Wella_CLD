# Wella 개발 중 발생한 문제 및 해결 기록

> 목적: 동일한 실수 반복 방지 / 다음 모듈 개발 시 참고

---

## 1. FontStyle.Light 없음

WinForms `System.Drawing.FontStyle`에는 `Light`가 없다. 유효한 값은 `Regular`, `Bold`, `Italic`, `Underline`, `Strikeout`뿐이다.  
`new Font("Segoe UI", 22f, FontStyle.Light)` → `FontStyle.Regular`로 교체.

---

## 2. .NET 타겟 버전 불일치

`.csproj`에 `net6.0-windows`로 작성했으나 설치된 SDK는 .NET 10이어서 경고 발생.  
`<TargetFramework>net10.0-windows</TargetFramework>`로 수정.  
**교훈**: 프로젝트 시작 전 `dotnet --version`으로 설치 버전 먼저 확인.

---

## 3. GDI OnPaint 안에서 Font/Brush 반복 생성 → 시스템 다운

`OnMouseMove` → `Invalidate()` → `OnPaint` 루프가 빠르게 반복되는 상황에서 `OnPaint` 내부에서 `new Font()`를 매번 생성하면 GDI 핸들이 고갈되어 시스템이 다운된다.  
이모지 폰트(`Segoe UI Emoji`)는 렌더링 비용이 극히 높아 특히 심각함.  
**해결**: Font/Brush/Pen을 클래스 필드로 캐싱, `Dispose()`에서 해제. 이모지 폰트 금지 → 일반 유니코드 기호(`▦ ✔ ▤ #`) 사용.

---

## 4. 콘텐츠 패널에 달력이 안 보임 (초기화 순서 문제)

Form 생성자에서 `AppController.Initialize()`를 호출하면 레이아웃 미완료 상태(패널 크기=0)에서 실행되어 패널이 비어 보인다.  
**해결**: `Load += (_, _) => _appController.Initialize()`로 Form 레이아웃 완료 후 실행.

---

## 5. NullReferenceException — 초기화 순서 역전

`BuildNav()` 내부에서 `_grid`를 참조하는데 `_grid`가 그 이후에 생성되어 null 예외 발생.  
**해결**: 참조되는 객체(`_grid`)를 호출 메서드보다 먼저 생성.  
**교훈**: 메서드 호출 간 필드 참조 관계가 있을 때 초기화 순서를 반드시 확인.

---

## 6. Dock 중첩 레이아웃 버그 — 요일 헤더 없음 / 열 잘림

크기=0인 body 패널에 자식을 추가해 레이아웃이 잘못 계산된 후, body 크기가 올바르게 변경되어도 자식 레이아웃이 재실행되지 않아 CalendarGrid가 잘못된 크기를 유지했다.  
**해결**: Dock 중첩 방식 폐기 → `OnResize` + `SetBounds` 수동 배치로 전환.  
**교훈**: GDI 커스텀 컨트롤이 포함된 복잡한 레이아웃은 수동 배치가 훨씬 안정적.

---

## 7. GDI 커스텀 컨트롤 리사이즈 후 재드로우 안 됨

창 크기 변경 시 CalendarGrid 내용이 갱신되지 않는 문제.  
**해결**: 생성자에 `ResizeRedraw = true` 추가 또는 `OnResize`에서 `Invalidate()` 호출.

---

## 8. git push 시 bin / obj 폴더 올라감

`git add .` 후 빌드 결과물(`.exe`, `.dll` 등)이 GitHub에 함께 업로드됨.  
**해결**: 프로젝트 루트에 `.gitignore` 생성하여 `bin/`, `obj/`, `Data/` 제외.  
**교훈**: git init 직후 가장 먼저 `.gitignore`를 만들 것.

---

## 9. 창 최대화 시 TopMenuBar 버튼 잔상(유령 버튼)

창을 키울 때 버튼이 `Width` 기준 우측 정렬로 이동하는데, WinForms가 새로 노출된 영역만 재드로우해서 기존 위치의 버튼이 지워지지 않고 잔상으로 남는다.  
**해결**: `TopMenuBar` 생성자에 `ResizeRedraw = true` 추가 → 리사이즈 시 전체 영역 재드로우.

---

## 10. CalendarGrid 최대화 후 렌더링 느림

`DrawCell`에서 42개 셀마다 `new SolidBrush()`를 반복 생성해 대형 화면에서 GDI 객체 할당 비용이 누적됐다.  
**해결**: SunBrush, SatBrush, NormalBrush 등 9개 브러시/펜을 클래스 필드로 캐싱.

---

## 11. 달력 날짜 클릭 → 다이얼로그 방식으로 변경

기존: 날짜 클릭 → 사이드 패널 선택 → "+ 추가" 버튼 클릭의 2단계 구조.  
**변경**: 날짜 클릭 즉시 `CalendarDayDialog` 열림 — 기존 일정 목록 + 추가/삭제를 하나의 창에서 처리.  
우측 사이드 패널과 좌측 사이드바도 불필요하여 제거, 달력이 전체 영역 사용.

---

## 12. 대한민국 공휴일 표시

양력 고정 공휴일(8종)과 음력 기반 공휴일(설날·추석 연휴, 부처님오신날, 2020~2030)을 `KoreanHolidays.cs`에 정적 딕셔너리로 관리.  
공휴일 날짜는 빨간색으로 표시하고, 날짜 숫자 아래에 7.5pt 소자로 공휴일 이름 표시.  
양력 고정 우선 적용(예: 2025-05-05 → "어린이날").

---

## 요약 — 다음 모듈 개발 시 체크리스트

| # | 항목 |
|---|---|
| 1 | `FontStyle.Light` 사용 금지 → `Regular` 사용 |
| 2 | `.csproj` TargetFramework를 설치된 SDK 버전과 맞출 것 |
| 3 | GDI `OnPaint` 안에서 `new Font/Brush/Pen` 금지 → 필드로 캐싱 |
| 4 | 이모지 폰트(`Segoe UI Emoji`) 사용 금지 |
| 5 | Form 초기화는 생성자가 아닌 `Load` 이벤트에서 실행 |
| 6 | 메서드 간 참조 필드의 초기화 순서 확인 |
| 7 | 복잡한 레이아웃은 Dock 중첩 대신 `SetBounds + OnResize` 수동 배치 |
| 8 | GDI 커스텀 컨트롤에 `ResizeRedraw = true` 설정 |
| 9 | git init 후 즉시 `.gitignore` 생성 (`bin/`, `obj/` 제외) |
| 10 | TopMenuBar 등 너비 기반 위치 계산 컨트롤에 `ResizeRedraw = true` 필수 |
| 11 | 새 모듈 추가 시 기존 사이드바/메뉴바 수정 불필요 (AppController에 이미 등록됨) |

---

## 변경 이력

| 날짜 | 내용 |
|---|---|
| 2026-05-31 | Phase 1~2 개발 중 발생한 문제 최초 기록 |
| 2026-06-01 | Phase 2 수정 및 UI 개편 내용 추가 (9~12번) |
