# UsageWidget

![Windows](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4?style=flat-square&logo=windows)
![WPF](https://img.shields.io/badge/UI-WPF-5C2D91?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-2ea44f?style=flat-square)
[![Release](https://img.shields.io/github/v/release/sloth197/UsageWidget?style=flat-square)](https://github.com/sloth197/UsageWidget/releases)

## About

Codex 사용량을 화면 한쪽에 조용히 띄워두는 Windows 위젯입니다.

사용량 한도를 확인하면서도 작업 흐름을 방해하지 않도록, 투명도 조절·항상 위·드래그 이동·Jelly Card 인터랙션을 작은 카드 안에 담았습니다.

![UsageWidget 미리보기](assets/preview.svg)

### Tags

`windows` `wpf` `dotnet` `codex` `usage-monitor` `desktop-widget` `jelly-ui` `spring-animation` `always-on-top` `korean`

## 기능

- 5시간·주간 등 서버가 반환하는 사용량 한도만 동적으로 표시
- Pro·Enterprise·Edu·크레딧·무제한 계정 대응
- 1분마다 자동 갱신
- 사용량 조회 자체는 AI 모델 요청을 보내지 않음
- 투명도 슬라이더
- 항상 위에 표시 및 자유로운 창 이동
- 가장자리를 당기면 게이지까지 함께 휘는 Jelly Card 효과
- `%` 전용 멀티 해상도 아이콘

## 다운로드

최신 Windows x64 실행 파일은 [Releases](https://github.com/sloth197/UsageWidget/releases)에서 받을 수 있습니다.

현재 릴리즈: [UsageWidget v0.1.0](https://github.com/sloth197/UsageWidget/releases/tag/v0.1.0)

## 빌드

요구 사항:

- Windows 10/11 x64
- .NET 10 SDK
- Codex 데스크톱 앱 설치 및 로그인 (실행 시 사용량 조회에 필요)

```powershell
dotnet publish .\CodexUsageWidget\CodexUsageWidget.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\publish
```

실행 파일은 `publish\CodexUsageWidget.exe`에 생성됩니다.

## 개인정보와 제한사항

이 프로젝트는 공식 OpenAI 제품이 아닌 커뮤니티 프로젝트입니다. 설치된 Codex의 로컬 app-server 인터페이스를 통해 현재 사용량 스냅샷만 읽습니다. Codex 업데이트로 내부 인터페이스가 변경되면 동작하지 않을 수 있습니다.

위젯은 비밀번호, API 키, 대화 내용 또는 계정 인증 정보를 읽거나 저장하지 않습니다. Codex가 설치되지 않았거나 로그인되지 않은 경우 사용량 대신 조회 실패를 표시합니다.

## 라이선스

MIT License. 자세한 내용은 [LICENSE](LICENSE)를 참고하세요.
