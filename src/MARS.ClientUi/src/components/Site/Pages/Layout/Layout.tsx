import { useSiteColors } from "@/shared/Utils/useSiteColors";

import Footer from "./Footer/Footer";
import Header from "./Header/Header";
import styles from "./Layout.module.scss";

interface LayoutProperties {
  children: React.ReactNode;
}

const Layout: React.FC<LayoutProperties> = ({ children }) => {
  const colors = useSiteColors();

  return (
    <div
      className={`${styles.layout} site-component`}
      style={{
        backgroundColor: colors.background.primary,
        color: colors.text.primary,
      }}
    >
      <Header />
      <main className={styles.main}>{children}</main>
      <Footer />
    </div>
  );
};

export default Layout;
