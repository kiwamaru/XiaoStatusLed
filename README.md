# XiaoStatusLed

USB接続した [Seeed Studio XIAO RP2040](https://wiki.seeedstudio.com/XIAO-RP2040-with-Arduino/) の NeoPixel を、Windows PCから制御するためのプロジェクトです。

Claude Codeなどの開発ツールの状態を、LEDの色や明滅パターンで視覚的に通知することを目的としています。

## Features

* XIAO RP2040のNeoPixelをWindows PCから制御
* COMポート番号を固定せず、USBデバイスのVID/PIDから自動検出
* PINGによるデバイス確認
* 色、明るさ、点滅パターンをコマンドで指定
* 徐々に明るくなったり暗くなったりするフェード表示に対応
* Claude Code Hooksとの連携を想定

## System Overview

```text
Claude Code
     │
     │ Hook
     ▼
XiaoStatusLed.exe
     │
     │ COM Port
     ▼
XIAO RP2040
     │
     ▼
NeoPixel
```

## Hardware

* Seeed Studio XIAO RP2040
* NeoPixel LED

XIAO RP2040に搭載されているNeoPixelを使用します。

## Software

* Windows
* .NET 10
* Arduino IDE
* Arduino-Pico / RP2040 Arduino environment

## COM Port Detection

COMポート番号は固定せず、Windowsのデバイス情報からXIAO RP2040のVID/PIDに一致するCOMポートを検索します。

```text
USB Device
    │
    ├── VID/PIDを確認
    │
    ├── 対象デバイスのみ選択
    │
    └── PING
            │
            └── XIAO_STATUS_LED
```

そのため、XIAO RP2040が `COM3` から `COM7` に変わっても、COMポート番号を設定し直す必要はありません。

## Serial Protocol

### PING

XIAO RP2040の存在を確認します。

```text
PING
```

XIAO RP2040から以下の応答を受信すると、対象デバイスとして認識します。

```text
XIAO_STATUS_LED
```

### SET

LEDの表示パターンを設定します。

```text
SET <R> <G> <B> <WAVEFORM> <PERIOD_MS> <MIN_BRIGHTNESS> <MAX_BRIGHTNESS>
```

Example:

```text
SET 0 0 255 SINE 2000 30 255
```

これは、青色で明るさを徐々に変化させるパターンです。

### Example

```text
SET 255 0 0 CONSTANT 1000 255 255
```

赤色で点灯。

```text
SET 0 255 0 CONSTANT 1000 255 255
```

緑色で点灯。

```text
SET 0 0 255 SINE 2000 30 255
```

青色でゆっくり明滅。

消灯：

```text
SET 0 0 0 CONSTANT 1000 0 0
```

## Example Status Patterns

| State   | Color  | Pattern       |
| ------- | ------ | ------------- |
| Working | Blue   | Slowly fading |
| Waiting | Orange | Fast fading   |
| Success | Green  | Constant      |
| Error   | Red    | Fading        |
| Off     | Off    | -             |

## Claude Code Integration

Claude Code Hooksから、状態に応じて`XiaoStatusLed.exe`を呼び出すことを想定しています。

Example:

```text
UserPromptSubmit
        │
        ▼
XiaoStatusLed.exe working
        │
        ▼
Blue LED
```

```text
Stop
        │
        ▼
XiaoStatusLed.exe success
        │
        ▼
Green LED
```

Claude Code Hookの設定例：

```json
{
  "hooks": {
    "UserPromptSubmit": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "C:\\Tools\\XiaoStatusLed.exe working"
          }
        ]
      }
    ],
    "Stop": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "C:\\Tools\\XiaoStatusLed.exe success"
          }
        ]
      }
    ]
  }
}
```

## Project Structure

```text
XiaoStatusLed/
├── XiaoStatusLed/
│   └── C# application
│
└── XiaoStatusLed/
    └── Arduino sketch
```

## Status

Work in progress.

The primary goal is to use this device as a physical status indicator for Claude Code and other development tools.

## License

To be determined.
