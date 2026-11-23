import { useEffect, useRef, useState } from "react";

interface Properties {
  compressor?: number;
  minFontSize?: number;
  maxFontSize?: number;
  children: React.ReactNode;
}

export const FullText = ({
  compressor = 1,
  minFontSize = Number.NEGATIVE_INFINITY,
  maxFontSize = Infinity,
  children,
}: Properties) => {
  const elementReference = useRef<HTMLDivElement>(null);
  const [fontSize, setFontSize] = useState<number | undefined>();

  useEffect(() => {
    const resizer = () => {
      if (!elementReference.current) {
        return;
      }

      const width = elementReference.current.offsetWidth;
      const newFontSize = Math.max(
        Math.min(width / (compressor * 10), maxFontSize),
        minFontSize
      );
      setFontSize(newFontSize);
    };

    // Initial call to set the font size
    resizer();

    // Add event listeners for resize and orientation change
    window.addEventListener("resize", resizer);
    globalThis.addEventListener("orientationchange", resizer);

    // Cleanup event listeners on component unmount
    return () => {
      window.removeEventListener("resize", resizer);
      globalThis.removeEventListener("orientationchange", resizer);
    };
  }, [compressor, minFontSize, maxFontSize]);

  return (
    <div
      ref={elementReference}
      style={{ fontSize: fontSize ? `${fontSize}px` : undefined }}
    >
      {children}
    </div>
  );
};
