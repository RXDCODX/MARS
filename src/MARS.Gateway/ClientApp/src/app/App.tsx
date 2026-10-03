import "./global.scss";
import "./tailwind.css";

import { ThemeProvider } from "@/contexts/ThemeContext";
import { usePrefetchRoutes } from "@/routes/hooks/usePrefetchRoutes";
import Routes from "@/routes/Routes";
import {
  AntdStyleProvider,
  ThemeProvider as AntThemeProvider,
} from "@/shared/components/ui";

export default function App() {
  usePrefetchRoutes();

  return (
    <ThemeProvider>
      <AntdStyleProvider>
        <AntThemeProvider>
          <Routes />
        </AntThemeProvider>
      </AntdStyleProvider>
    </ThemeProvider>
  );
}
