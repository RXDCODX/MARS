import AFKScreen from "./AFKScreen";

/**
 * Расширенный пример использования компонента AFKScreen
 *
 * Этот компонент можно использовать в OBS как браузерный источник
 * или в любом React приложении для воспроизведения видео
 */
const AFKScreenExample: React.FC = () => (
  <div style={{ width: "100vw", height: "100vh" }}>
    <AFKScreen />
  </div>
);

export default AFKScreenExample;

// Пример использования с кастомными настройками:
/*
import React from 'react';
import AFKScreen from './components/OBS_Components/AFKScreen/AFKScreen';

function App() {
  return (
    <div className="App">
      <AFKScreen 
        playlistUrl="https://www.youtube.com/playlist?list=YOUR_PLAYLIST_ID"
        autoplay={true}
        autoplayDelay={2000}
        startMuted={true}
        enableSoundOnInteraction={true}
        autoRecovery={true}
        recoveryDelay={5000}
        showStatusIndicator={true}
        showInstructions={true}
        theme="dark"
      />
    </div>
  );
}

export default App;
*/

// Пример использования только компонента:
/*
import React from 'react';
import AFKScreen from './components/OBS_Components/AFKScreen';

function App() {
  return (
    <div className="App">
      <AFKScreen />
    </div>
  );
}

export default App;
*/

// Пример использования в OBS:
/*
1. Откройте OBS Studio
2. Добавьте новый источник "Браузер"
3. Введите URL вашей страницы с AFKScreen
4. Установите размер 1920x1080 (или другой)
5. Включите источник
6. Компонент автоматически начнет воспроизведение
*/
