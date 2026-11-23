import { FireOutlineCanvas } from "./FireShader";

// Демо: в качестве маски используется SVG (data URL), вокруг него рисуем огонь
export const AvatarWithFireSvg = () => {
  const size = 512;
  const radius = Math.floor(size * 0.5);
  const intensity = 3;
  const speed = 1.35;

  // Простой SVG-силуэт (блоб) на прозрачном фоне
  const svg = `
<svg xmlns='http://www.w3.org/2000/svg' width='${size}' height='${size}' viewBox='0 0 ${size} ${size}'>
  <defs>
    <radialGradient id='g' cx='50%' cy='45%' r='50%'>
      <stop offset='0%' stop-color='#ffffff' />
      <stop offset='100%' stop-color='#dddddd' />
    </radialGradient>
  </defs>
  <rect width='100%' height='100%' fill='transparent'/>
  <path fill='url(#g)' d='
    M ${size * 0.2} ${size * 0.45}
    C ${size * 0.2} ${size * 0.25}, ${size * 0.35} ${size * 0.1}, ${size * 0.5} ${size * 0.1}
    C ${size * 0.72} ${size * 0.1}, ${size * 0.8} ${size * 0.28}, ${size * 0.8} ${size * 0.45}
    C ${size * 0.8} ${size * 0.72}, ${size * 0.64} ${size * 0.88}, ${size * 0.5} ${size * 0.9}
    C ${size * 0.35} ${size * 0.88}, ${size * 0.2} ${size * 0.7}, ${size * 0.2} ${size * 0.45}
    Z' />
</svg>`;
  const svgUrl = `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`;

  return (
    <div
      style={{
        width: size,
        height: size,
        position: "relative",
        background: "#000",
      }}
    >
      <img
        alt="svg-mask"
        src={svgUrl}
        style={{
          width: "100%",
          height: "100%",
          objectFit: "contain",
          display: "block",
          userSelect: "none",
          pointerEvents: "none",
          position: "absolute",
          inset: 0,
          zIndex: 0,
        }}
      />
      <FireOutlineCanvas
        width={size}
        height={size}
        maskUrl={svgUrl}
        radiusPx={radius}
        intensity={intensity}
        speed={speed}
        style={{ position: "absolute", inset: 0 }}
      />
    </div>
  );
};
