## Summary
- Добавлена возможность отключения WaifuChat LLM через appsettings.json (использует существующий `WaifuChatOptions.Enabled`)
- Добавлена возможность отключения TTS через appsettings.json с тремя уровнями контроля:
  - `Tts:Enabled` — мастер-выключатель для всего TTS
  - `Tts:WindowsTts:Enabled` — отключение только Windows TTS (SystemSpeech)
  - `Tts:OnnxTts:Enabled` — отключение только ONNX TTS (Synthezia)
- SyntheziaQueueManager получает nullable параметры для обоих движков
- `NoOpSyntheziaQueueManager` как заглушка когда TTS полностью отключён

## Changes
- `Program.cs`: conditional DI registration для TTS и WaifuChat
- `TtsOptions.cs`: новый класс настроек с вложенными `WindowsTtsSettings` и `OnnxTtsSettings`
- `SyntheziaQueueManager.cs`: nullable параметры + null-check guards
- `appsettings.json`: обновлена секция Tts с новыми настройками
