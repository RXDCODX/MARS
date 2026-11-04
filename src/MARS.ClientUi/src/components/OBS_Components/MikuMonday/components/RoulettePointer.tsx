import styles from "../MikuMonday.module.scss";

interface RoulettePointerProperties {
  visible: boolean;
  pointerHeight: number;
}

export default function RoulettePointer({
  visible,
  pointerHeight,
}: RoulettePointerProperties) {
  if (!visible) {
    return null;
  }

  return (
    <div
      className={styles["roulette-pointer"]}
      style={{
        position: "absolute",
        top: "50%",
        left: "50%",
        width: "4px",
        height: `${pointerHeight}px`,
        background: "var(--bs-primary)",
        zIndex: 10,
        transform: "translate(-50%, -50%)",
        visibility: visible ? "visible" : "hidden",
        boxShadow: "0 0 20px var(--bs-primary), 0 0 40px var(--bs-primary)",
      }}
    />
  );
}
