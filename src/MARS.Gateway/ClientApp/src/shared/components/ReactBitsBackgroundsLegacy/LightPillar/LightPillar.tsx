import "./LightPillar.css";

import React, { useEffect, useRef, useState } from "react";
import * as THREE from "three";

interface LightPillarProperties {
  topColor?: string;
  bottomColor?: string;
  intensity?: number;
  rotationSpeed?: number;
  interactive?: boolean;
  className?: string;
  glowAmount?: number;
  pillarWidth?: number;
  pillarHeight?: number;
  noiseIntensity?: number;
  mixBlendMode?: React.CSSProperties["mixBlendMode"];
  pillarRotation?: number;
  quality?: "low" | "medium" | "high";
}

const LightPillar: React.FC<LightPillarProperties> = ({
  topColor = "#5227FF",
  bottomColor = "#FF9FFC",
  intensity = 1,
  rotationSpeed = 0.3,
  interactive = false,
  className = "",
  glowAmount = 0.005,
  pillarWidth = 3,
  pillarHeight = 0.4,
  noiseIntensity = 0.5,
  mixBlendMode = "screen",
  pillarRotation = 0,
  quality = "high",
}) => {
  const containerReference = useRef<HTMLDivElement>(null);
  const rafReference = useRef<number | null>(null);
  const rendererReference = useRef<THREE.WebGLRenderer | null>(null);
  const materialReference = useRef<THREE.ShaderMaterial | null>(null);
  const sceneReference = useRef<THREE.Scene | null>(null);
  const cameraReference = useRef<THREE.OrthographicCamera | null>(null);
  const geometryReference = useRef<THREE.PlaneGeometry | null>(null);
  const mouseReference = useRef<THREE.Vector2>(new THREE.Vector2(0, 0));
  const timeReference = useRef(0);
  const rotationSpeedReference = useRef(rotationSpeed);
  const [webGLSupported, setWebGLSupported] = useState<boolean>(true);

  // Check WebGL support
  useEffect(() => {
    const canvas = document.createElement("canvas");
    const gl =
      canvas.getContext("webgl") || canvas.getContext("experimental-webgl");
    if (!gl) {
      setWebGLSupported(false);
    }
  }, []);

  useEffect(() => {
    if (!containerReference.current || !webGLSupported) return;

    const container = containerReference.current;
    const width = container.clientWidth;
    const height = container.clientHeight;

    const isMobile =
      /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(
        navigator.userAgent
      );
    const isLowEndDevice =
      isMobile ||
      (navigator.hardwareConcurrency && navigator.hardwareConcurrency <= 4);

    let effectiveQuality = quality;
    if (isLowEndDevice && quality === "high") effectiveQuality = "medium";
    if (isMobile && quality !== "low") effectiveQuality = "low";

    const qualitySettings = {
      low: {
        iterations: 24,
        waveIterations: 1,
        pixelRatio: 0.5,
        precision: "mediump",
        stepMultiplier: 1.5,
      },
      medium: {
        iterations: 40,
        waveIterations: 2,
        pixelRatio: 0.65,
        precision: "mediump",
        stepMultiplier: 1.2,
      },
      high: {
        iterations: 80,
        waveIterations: 4,
        pixelRatio: Math.min(window.devicePixelRatio, 2),
        precision: "highp",
        stepMultiplier: 1,
      },
    };

    const settings =
      qualitySettings[effectiveQuality] || qualitySettings.medium;

    const scene = new THREE.Scene();
    sceneReference.current = scene;
    const camera = new THREE.OrthographicCamera(-1, 1, 1, -1, 0, 1);
    cameraReference.current = camera;

    let renderer: THREE.WebGLRenderer;
    try {
      renderer = new THREE.WebGLRenderer({
        antialias: false,
        alpha: true,
        powerPreference:
          effectiveQuality === "high" ? "high-performance" : "low-power",
        precision: settings.precision as "highp" | "mediump" | "lowp",
        stencil: false,
        depth: false,
      });
    } catch {
      setWebGLSupported(false);
      return;
    }

    renderer.setSize(width, height);
    renderer.setPixelRatio(settings.pixelRatio);
    container.append(renderer.domElement);
    rendererReference.current = renderer;

    const parseColor = (hex: string): THREE.Vector3 => {
      const color = new THREE.Color(hex);
      return new THREE.Vector3(color.r, color.g, color.b);
    };

    const vertexShader = `
      varying vec2 vUv;
      void main() {
        vUv = uv;
        gl_Position = vec4(position, 1.0);
      }
    `;

    const fragmentShader = `
      precision ${settings.precision} float;

      uniform float uTime;
      uniform vec2 uResolution;
      uniform vec2 uMouse;
      uniform vec3 uTopColor;
      uniform vec3 uBottomColor;
      uniform float uIntensity;
      uniform bool uInteractive;
      uniform float uGlowAmount;
      uniform float uPillarWidth;
      uniform float uPillarHeight;
      uniform float uNoiseIntensity;
      uniform float uRotCos;
      uniform float uRotSin;
      uniform float uPillarRotCos;
      uniform float uPillarRotSin;
      uniform float uWaveSin;
      uniform float uWaveCos;
      varying vec2 vUv;

      const float STEP_MULT = ${settings.stepMultiplier.toFixed(1)};
      const int MAX_ITER = ${settings.iterations};
      const int WAVE_ITER = ${settings.waveIterations};

      void main() {
        vec2 uv = (vUv * 2.0 - 1.0) * vec2(uResolution.x / uResolution.y, 1.0);
        uv = vec2(uPillarRotCos * uv.x - uPillarRotSin * uv.y, uPillarRotSin * uv.x + uPillarRotCos * uv.y);

        vec3 ro = vec3(0.0, 0.0, -10.0);
        vec3 rd = normalize(vec3(uv, 1.0));

        float rotC = uRotCos;
        float rotS = uRotSin;
        if(uInteractive && (uMouse.x != 0.0 || uMouse.y != 0.0)) {
          float a = uMouse.x * 6.283185;
          rotC = cos(a);
          rotS = sin(a);
        }

        vec3 col = vec3(0.0);
        float t = 0.1;
        
        for(int i = 0; i < MAX_ITER; i++) {
          vec3 p = ro + rd * t;
          p.xz = vec2(rotC * p.x - rotS * p.z, rotS * p.x + rotC * p.z);

          vec3 q = p;
          q.y = p.y * uPillarHeight + uTime;
          
          float freq = 1.0;
          float amp = 1.0;
          for(int j = 0; j < WAVE_ITER; j++) {
            q.xz = vec2(uWaveCos * q.x - uWaveSin * q.z, uWaveSin * q.x + uWaveCos * q.z);
            q += cos(q.zxy * freq - uTime * float(j) * 2.0) * amp;
            freq *= 2.0;
            amp *= 0.5;
          }
          
          float d = length(cos(q.xz)) - 0.2;
          float bound = length(p.xz) - uPillarWidth;
          float k = 4.0;
          float h = max(k - abs(d - bound), 0.0);
          d = max(d, bound) + h * h * 0.0625 / k;
          d = abs(d) * 0.15 + 0.01;

          float grad = clamp((15.0 - p.y) / 30.0, 0.0, 1.0);
          col += mix(uBottomColor, uTopColor, grad) / d;

          t += d * STEP_MULT;
          if(t > 50.0) break;
        }

        float widthNorm = uPillarWidth / 3.0;
        col = tanh(col * uGlowAmount / widthNorm);
        
        col -= fract(sin(dot(gl_FragCoord.xy, vec2(12.9898, 78.233))) * 43758.5453) / 15.0 * uNoiseIntensity;
        
        gl_FragColor = vec4(col * uIntensity, 1.0);
      }
    `;

    const pillarRotRad = (pillarRotation * Math.PI) / 180;
    const waveSin = Math.sin(0.4);
    const waveCos = Math.cos(0.4);

    const material = new THREE.ShaderMaterial({
      vertexShader,
      fragmentShader,
      uniforms: {
        uTime: { value: 0 },
        uResolution: { value: new THREE.Vector2(width, height) },
        uMouse: { value: mouseReference.current },
        uTopColor: { value: parseColor(topColor) },
        uBottomColor: { value: parseColor(bottomColor) },
        uIntensity: { value: intensity },
        uInteractive: { value: interactive },
        uGlowAmount: { value: glowAmount },
        uPillarWidth: { value: pillarWidth },
        uPillarHeight: { value: pillarHeight },
        uNoiseIntensity: { value: noiseIntensity },
        uRotCos: { value: 1 },
        uRotSin: { value: 0 },
        uPillarRotCos: { value: Math.cos(pillarRotRad) },
        uPillarRotSin: { value: Math.sin(pillarRotRad) },
        uWaveSin: { value: waveSin },
        uWaveCos: { value: waveCos },
      },
      transparent: true,
      depthWrite: false,
      depthTest: false,
    });
    materialReference.current = material;

    const geometry = new THREE.PlaneGeometry(2, 2);
    geometryReference.current = geometry;
    const mesh = new THREE.Mesh(geometry, material);
    scene.add(mesh);

    let mouseMoveTimeout: number | null = null;
    const handleMouseMove = (event: MouseEvent) => {
      if (!interactive) return;

      if (mouseMoveTimeout) return;

      mouseMoveTimeout = globalThis.setTimeout(() => {
        mouseMoveTimeout = null;
      }, 16);

      const rect = container.getBoundingClientRect();
      const x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
      const y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
      mouseReference.current.set(x, y);
    };

    if (interactive) {
      container.addEventListener("mousemove", handleMouseMove, {
        passive: true,
      });
    }

    let lastTime = performance.now();
    const targetFPS = effectiveQuality === "low" ? 30 : 60;
    const frameTime = 1000 / targetFPS;

    const animate = (currentTime: number) => {
      if (
        !materialReference.current ||
        !rendererReference.current ||
        !sceneReference.current ||
        !cameraReference.current
      )
        return;

      const deltaTime = currentTime - lastTime;

      if (deltaTime >= frameTime) {
        timeReference.current += 0.016 * rotationSpeedReference.current;
        const t = timeReference.current;
        materialReference.current.uniforms.uTime.value = t;
        materialReference.current.uniforms.uRotCos.value = Math.cos(t * 0.3);
        materialReference.current.uniforms.uRotSin.value = Math.sin(t * 0.3);
        rendererReference.current.render(
          sceneReference.current,
          cameraReference.current
        );
        lastTime = currentTime - (deltaTime % frameTime);
      }

      rafReference.current = requestAnimationFrame(animate);
    };
    rafReference.current = requestAnimationFrame(animate);

    let resizeTimeout: number | null = null;
    const handleResize = () => {
      if (resizeTimeout) {
        clearTimeout(resizeTimeout);
      }

      resizeTimeout = globalThis.setTimeout(() => {
        if (
          !rendererReference.current ||
          !materialReference.current ||
          !containerReference.current
        )
          return;
        const newWidth = containerReference.current.clientWidth;
        const newHeight = containerReference.current.clientHeight;
        rendererReference.current.setSize(newWidth, newHeight);
        materialReference.current.uniforms.uResolution.value.set(
          newWidth,
          newHeight
        );
      }, 150);
    };

    window.addEventListener("resize", handleResize, { passive: true });

    return () => {
      window.removeEventListener("resize", handleResize);
      if (interactive) {
        container.removeEventListener("mousemove", handleMouseMove);
      }
      if (rafReference.current) {
        cancelAnimationFrame(rafReference.current);
      }
      if (rendererReference.current) {
        rendererReference.current.dispose();
        rendererReference.current.forceContextLoss();
        if (container.contains(rendererReference.current.domElement)) {
          rendererReference.current.domElement.remove();
        }
      }
      if (materialReference.current) {
        materialReference.current.dispose();
      }
      if (geometryReference.current) {
        geometryReference.current.dispose();
      }

      rendererReference.current = null;
      materialReference.current = null;
      sceneReference.current = null;
      cameraReference.current = null;
      geometryReference.current = null;
      rafReference.current = null;
    };
  }, [webGLSupported, quality]);

  useEffect(() => {
    rotationSpeedReference.current = rotationSpeed;
  }, [rotationSpeed]);

  useEffect(() => {
    if (!materialReference.current) return;
    const parseColor = (hex: string) => {
      const color = new THREE.Color(hex);
      return new THREE.Vector3(color.r, color.g, color.b);
    };
    materialReference.current.uniforms.uTopColor.value = parseColor(topColor);
  }, [topColor]);

  useEffect(() => {
    if (!materialReference.current) return;
    const parseColor = (hex: string) => {
      const color = new THREE.Color(hex);
      return new THREE.Vector3(color.r, color.g, color.b);
    };
    materialReference.current.uniforms.uBottomColor.value =
      parseColor(bottomColor);
  }, [bottomColor]);

  useEffect(() => {
    if (!materialReference.current) return;
    materialReference.current.uniforms.uIntensity.value = intensity;
  }, [intensity]);

  useEffect(() => {
    if (!materialReference.current) return;
    materialReference.current.uniforms.uInteractive.value = interactive;
  }, [interactive]);

  useEffect(() => {
    if (!materialReference.current) return;
    materialReference.current.uniforms.uGlowAmount.value = glowAmount;
  }, [glowAmount]);

  useEffect(() => {
    if (!materialReference.current) return;
    materialReference.current.uniforms.uPillarWidth.value = pillarWidth;
  }, [pillarWidth]);

  useEffect(() => {
    if (!materialReference.current) return;
    materialReference.current.uniforms.uPillarHeight.value = pillarHeight;
  }, [pillarHeight]);

  useEffect(() => {
    if (!materialReference.current) return;
    materialReference.current.uniforms.uNoiseIntensity.value = noiseIntensity;
  }, [noiseIntensity]);

  useEffect(() => {
    if (!materialReference.current) return;
    const pillarRotRad = (pillarRotation * Math.PI) / 180;
    materialReference.current.uniforms.uPillarRotCos.value =
      Math.cos(pillarRotRad);
    materialReference.current.uniforms.uPillarRotSin.value =
      Math.sin(pillarRotRad);
  }, [pillarRotation]);

  if (!webGLSupported) {
    return (
      <div
        className={`light-pillar-fallback ${className}`}
        style={{ mixBlendMode }}
      >
        WebGL not supported
      </div>
    );
  }

  return (
    <div
      ref={containerReference}
      className={`light-pillar-container ${className}`}
      style={{ mixBlendMode }}
    />
  );
};

export default LightPillar;
