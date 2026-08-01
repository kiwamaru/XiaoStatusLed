// XIAO RP2040 の NeoPixelを使ったCOMコマンドでのインジケーター
// このコードは Arduino 用の プロジェクトです

//COMコマンド形式
//SET 255 0 0 SINE 2000 0 255

// SET 
// 点滅時の色=RGB  0～255
// 波形=CONSTANT(即時切替),SINE(サインカーブ),TRIANGLE(三角波)
// 点滅周期=2000ms
// 点滅時の最小輝度=0
// 点滅時の最大輝度=255

// 使用例
//赤色を常時点灯
//SET 255 0 0 CONSTANT 1000 255 255
//赤色を2秒周期で滑らかに明滅
//SET 255 0 0 SINE 2000 0 255
//青色を1秒周期で三角波状に明滅
//SET 0 0 255 TRIANGLE 1000 0 255
//黄色を完全には消さずに明滅
//SET 255 255 0 SINE 1500 30 255
//消灯
//OFF

#include <Adafruit_NeoPixel.h>
#include <math.h>

// XIAO RP2040
constexpr uint8_t LED_POWER_PIN = 11;
constexpr uint8_t LED_DATA_PIN  = 12;
constexpr uint8_t LED_COUNT     = 1;

Adafruit_NeoPixel pixel(
    LED_COUNT,
    LED_DATA_PIN,
    NEO_GRB + NEO_KHZ800
);

// --------------------------------------------------
// LEDパターン
// --------------------------------------------------

enum class Waveform
{
    Constant,
    Sine,
    Triangle,
    Square
};

struct LedPattern
{
    uint8_t r = 0;
    uint8_t g = 0;
    uint8_t b = 0;

    Waveform waveform = Waveform::Constant;

    uint32_t periodMs = 1000;

    uint8_t minBrightness = 0;
    uint8_t maxBrightness = 255;

    uint32_t startTime = 0;
};

LedPattern pattern;

// --------------------------------------------------
// LED制御
// --------------------------------------------------

void setLed(uint8_t r, uint8_t g, uint8_t b)
{
    pixel.setPixelColor(
        0,
        pixel.Color(r, g, b)
    );

    pixel.show();
}
/**
 * @param waveform 波形
 * @param phase 0.0 ～ 1.0 の位相
 * @return 波形の値 0.0 ～ 1.0
 */
float getWaveValue(Waveform waveform, float phase)
{
    // phase: 0.0 ～ 1.0

    switch (waveform)
    {
    case Waveform::Constant:
        return 1.0f;

    case Waveform::Sine:
        // 0.0 ～ 1.0 ～ 0.0
        return (sinf(phase * 2.0f * PI) + 1.0f) * 0.5f;

    case Waveform::Triangle:
        if (phase < 0.5f)
        {
            return phase * 2.0f;
        }
        else
        {
            return 2.0f - phase * 2.0f;
        }

    case Waveform::Square:
        return phase < 0.5f ? 1.0f : 0.0f;
    }

    return 0.0f;
}
/**
 * @brief LEDの状態を更新する
 */
void updateLed()
{
    const uint32_t now = millis();

    float phase = 0.0f;

    if (pattern.periodMs > 0)
    {
        const uint32_t elapsed =
            now - pattern.startTime;

        phase =
            (elapsed % pattern.periodMs) /
            static_cast<float>(pattern.periodMs);
    }

    const float waveValue =
        getWaveValue(pattern.waveform, phase);

    const float brightness =
        pattern.minBrightness +
        (pattern.maxBrightness -
         pattern.minBrightness) * waveValue;

    const uint8_t r =
        static_cast<uint8_t>(
            pattern.r * brightness / 255.0f
        );

    const uint8_t g =
        static_cast<uint8_t>(
            pattern.g * brightness / 255.0f
        );

    const uint8_t b =
        static_cast<uint8_t>(
            pattern.b * brightness / 255.0f
        );

    setLed(r, g, b);
}

// --------------------------------------------------
// コマンド解析
// --------------------------------------------------
/**
 * @brief 波形名をWaveform列挙型に変換する
 * @param name 波形名
 * @return Waveform列挙型
 */
Waveform parseWaveform(const char* name)
{
    if (strcmp(name, "SINE") == 0)
        return Waveform::Sine;

    if (strcmp(name, "TRIANGLE") == 0)
        return Waveform::Triangle;

    if (strcmp(name, "SQUARE") == 0)
        return Waveform::Square;

    return Waveform::Constant;
}
/**
 * @brief コマンドを解析してLEDパターンを設定する
 * @param command コマンド文字列
 */
void processCommand(char* command)
{
    uint16_t r;
    uint16_t g;
    uint16_t b;

    char waveformName[16];

    uint32_t period;
    uint16_t minBrightness;
    uint16_t maxBrightness;

    // ----------------------------------------------
    // SET R G B WAVEFORM PERIOD MIN MAX
    //
    // 例:
    // SET 255 0 0 SINE 2000 0 255
    // ----------------------------------------------

    if (sscanf(
            command,
            "SET %hu %hu %hu %15s %lu %hu %hu",
            &r,
            &g,
            &b,
            waveformName,
            &period,
            &minBrightness,
            &maxBrightness
        ) == 7)
    {
        pattern.r =
            static_cast<uint8_t>(constrain(r, 0, 255));

        pattern.g =
            static_cast<uint8_t>(constrain(g, 0, 255));

        pattern.b =
            static_cast<uint8_t>(constrain(b, 0, 255));

        pattern.waveform =
            parseWaveform(waveformName);

        pattern.periodMs =
            max<uint32_t>(period, 1);

        pattern.minBrightness =
            static_cast<uint8_t>(
                constrain(minBrightness, 0, 255)
            );

        pattern.maxBrightness =
            static_cast<uint8_t>(
                constrain(maxBrightness, 0, 255)
            );

        pattern.startTime = millis();

        Serial.println("OK");

        return;
    }

    // ----------------------------------------------
    // OFF
    // ----------------------------------------------

    if (strcmp(command, "OFF") == 0)
    {
        pattern.r = 0;
        pattern.g = 0;
        pattern.b = 0;
        pattern.waveform = Waveform::Constant;

        setLed(0, 0, 0);

        Serial.println("OK");

        return;
    }

    // ----------------------------------------------
    // PING
    // ----------------------------------------------

    if (strcmp(command, "PING") == 0)
    {
        Serial.println("XIAO_STATUS_LED");

        return;
    }

    Serial.println("ERROR");
}

// --------------------------------------------------
// シリアル受信
// --------------------------------------------------

constexpr size_t COMMAND_BUFFER_SIZE = 128;

char commandBuffer[COMMAND_BUFFER_SIZE];
size_t commandLength = 0;
/**
 * @brief シリアルからコマンドを受信して処理する
 */
void receiveCommand()
{
    while (Serial.available() > 0)
    {
        const char c =
            static_cast<char>(Serial.read());

        if (c == '\n' || c == '\r')
        {
            if (commandLength > 0)
            {
                commandBuffer[commandLength] = '\0';

                processCommand(commandBuffer);

                commandLength = 0;
            }
        }
        else
        {
            if (commandLength <
                COMMAND_BUFFER_SIZE - 1)
            {
                commandBuffer[commandLength++] = c;
            }
            else
            {
                // バッファオーバーフロー
                commandLength = 0;

                Serial.println("ERROR BUFFER");
            }
        }
    }
}

// --------------------------------------------------
// Arduino
// --------------------------------------------------

void setup()
{
    // XIAO RP2040のNeoPixel電源
    pinMode(LED_POWER_PIN, OUTPUT);
    digitalWrite(LED_POWER_PIN, HIGH);

    pixel.begin();

    setLed(0, 0, 0);

    Serial.begin(115200);

    pattern.startTime = millis();
}

void loop()
{
    receiveCommand();

    updateLed();
}