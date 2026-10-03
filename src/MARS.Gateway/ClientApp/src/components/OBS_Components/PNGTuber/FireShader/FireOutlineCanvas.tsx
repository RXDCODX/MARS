import { type FC, useEffect, useRef } from "react";

type FireOutlineCanvasProperties = {
  width: number;
  height: number;
  maskUrl: string;
  intensity?: number; // 0..2
  radiusPx?: number; // thickness of flames around the contour
  speed?: number; // flame animation speed multiplier
  className?: string;
  style?: React.CSSProperties;
};

const vertSource = `#version 100
precision mediump float;
attribute vec2 a_pos;
varying vec2 v_uv;
void main(){
  v_uv = (a_pos + 1.0) * 0.5;
  gl_Position = vec4(a_pos, 0.0, 1.0);
}`;

// Edge-based flame with UV alignment to object-fit: contain
const fragSource = `#version 100
precision mediump float;
varying vec2 v_uv;
uniform sampler2D u_mask;
uniform vec2 u_texel; // 1/textureSize
uniform vec2 u_canvasSize;
uniform vec2 u_texSize;
uniform float u_time;
uniform float u_radiusPx;
uniform float u_intensity;

float luminance(vec3 c){ return dot(c, vec3(0.299, 0.587, 0.114)); }

// Map canvas UV (v_uv) to texture UV (uvTex) using contain fit
vec2 mapContain(vec2 uv){
  float canvasAspect = u_canvasSize.x / u_canvasSize.y;
  float texAspect = u_texSize.x / u_texSize.y;
  if(canvasAspect > texAspect){
    // Letterbox left-right
    float scaleX = texAspect / canvasAspect; // displayed width fraction
    float offsetX = (1.0 - scaleX) * 0.5;
    return vec2((uv.x - offsetX) / scaleX, uv.y);
  } else {
    // Letterbox top-bottom
    float scaleY = canvasAspect / texAspect; // displayed height fraction
    float offsetY = (1.0 - scaleY) * 0.5;
    return vec2(uv.x, (uv.y - offsetY) / scaleY);
  }
}

float edge_strength(vec2 uvTex){
  vec2 t = u_texel;
  float tl = luminance(texture2D(u_mask, uvTex + vec2(-t.x, -t.y)).rgb);
  float tc = luminance(texture2D(u_mask, uvTex + vec2( 0.0, -t.y)).rgb);
  float tr = luminance(texture2D(u_mask, uvTex + vec2( t.x, -t.y)).rgb);
  float ml = luminance(texture2D(u_mask, uvTex + vec2(-t.x,  0.0)).rgb);
  float mc = luminance(texture2D(u_mask, uvTex).rgb);
  float mr = luminance(texture2D(u_mask, uvTex + vec2( t.x,  0.0)).rgb);
  float bl = luminance(texture2D(u_mask, uvTex + vec2(-t.x,  t.y)).rgb);
  float bc = luminance(texture2D(u_mask, uvTex + vec2( 0.0,  t.y)).rgb);
  float br = luminance(texture2D(u_mask, uvTex + vec2( t.x,  t.y)).rgb);
  float gx = -tl - 2.0*ml - bl + tr + 2.0*mr + br;
  float gy = -tl - 2.0*tc - tr + bl + 2.0*bc + br;
  float g = sqrt(gx*gx + gy*gy);
  return g; // ~0..1
}

float distance_to_edge(vec2 uvTex, float maxSteps, float stepPx){
  float minD = maxSteps * stepPx + 1.0;
  for(int d=0; d<16; d++){
    float angle = 6.2831853 * float(d) / 16.0;
    vec2 dir = vec2(cos(angle), sin(angle));
    for(float s=1.0; s<=maxSteps; s+=1.0){
      vec2 offs = dir * (s * stepPx) * u_texel;
      float e = edge_strength(uvTex + offs);
      if(e > 0.10){ // threshold
        float distPx = s * stepPx;
        if(distPx < minD){ minD = distPx; }
        break;
      }
    }
  }
  return minD;
}

float fbm(vec2 p){
  float v=0.0; float a=0.5; vec2 q=p;
  for(int i=0;i<4;i++){ v+=a*fract(sin(dot(q,vec2(127.1,311.7)))*43758.5453); q*=2.0; a*=0.5; }
  return v;
}

void main(){
  vec2 uvTex = mapContain(v_uv);
  // Discard outside the displayed texture region
  if(any(bvec2(uvTex.x < 0.0 || uvTex.x > 1.0, uvTex.y < 0.0 || uvTex.y > 1.0))){ discard; }

  float stepPx = 1.5;
  float maxSteps = ceil(u_radiusPx / stepPx) + 8.0;
  float distPx = distance_to_edge(uvTex, maxSteps, stepPx);
  if(distPx > u_radiusPx) discard;

  float n = fbm(uvTex * 6.0 + vec2(0.0, u_time * 0.6)) +
            0.5 * fbm(uvTex * 12.0 - vec2(u_time * 0.3, 0.0));
  float edge = 1.0 - smoothstep(u_radiusPx * 0.15, u_radiusPx, distPx + n * 2.0);
  float alpha = clamp(edge * (0.6 + 0.6 * u_intensity), 0.0, 1.0);

  vec3 c1 = vec3(1.0, 0.9, 0.2);
  vec3 c2 = vec3(1.0, 0.5, 0.1);
  vec3 c3 = vec3(1.0, 0.1, 0.0);
  vec3 color = mix(c2, c3, smoothstep(0.3, 1.0, 1.0 - distPx / max(u_radiusPx, 0.001)));
  color = mix(c1, color, 0.5 + 0.5 * n);
  gl_FragColor = vec4(color, alpha);
}`;

const FireOutlineCanvas: FC<FireOutlineCanvasProperties> = ({
  width,
  height,
  maskUrl,
  intensity = 1,
  radiusPx = 10,
  speed = 1,
  className,
  style,
}) => {
  const canvasReference = useRef<HTMLCanvasElement | null>(null);
  const glReference = useRef<WebGLRenderingContext | null>(null);
  const progReference = useRef<WebGLProgram | null>(null);
  const texReference = useRef<WebGLTexture | null>(null);
  const vaoReference = useRef<{ buf: WebGLBuffer | null; loc: number } | null>(
    null
  );
  const rafReference = useRef<number | null>(null);
  const startTimeReference = useRef<number>(0);
  const texSizeReference = useRef<{ w: number; h: number }>({
    w: width,
    h: height,
  });

  useEffect(() => {
    const canvas = canvasReference.current;
    if (!canvas) return;
    canvas.width = width;
    canvas.height = height;

    let gl = canvas.getContext("webgl", {
      alpha: true,
      premultipliedAlpha: true,
      antialias: false,
    }) as WebGLRenderingContext | null;
    if (!gl) {
      gl = canvas.getContext(
        "experimental-webgl"
      ) as WebGLRenderingContext | null;
    }
    if (!gl) return;
    glReference.current = gl;

    gl.enable(gl.BLEND);
    gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);

    // Compile shader helper
    const compile = (type: number, source: string) => {
      const s = gl.createShader(type)!;
      gl.shaderSource(s, source);
      gl.compileShader(s);
      if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) {
        console.error(
          "FireOutline shader compile error:",
          gl.getShaderInfoLog(s)
        );
      }
      return s;
    };

    const vs = compile(gl.VERTEX_SHADER, vertSource);
    const fs = compile(gl.FRAGMENT_SHADER, fragSource);
    const prog = gl.createProgram()!;
    gl.attachShader(prog, vs);
    gl.attachShader(prog, fs);
    gl.linkProgram(prog);
    if (!gl.getProgramParameter(prog, gl.LINK_STATUS)) {
      console.error(
        "FireOutline program link error:",
        gl.getProgramInfoLog(prog)
      );
    }
    gl.deleteShader(vs);
    gl.deleteShader(fs);
    progReference.current = prog;

    // Fullscreen quad
    const buffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
    const quad = new Float32Array([-1, -1, 1, -1, -1, 1, -1, 1, 1, -1, 1, 1]);
    gl.bufferData(gl.ARRAY_BUFFER, quad, gl.STATIC_DRAW);

    const loc = gl.getAttribLocation(prog, "a_pos");
    vaoReference.current = { buf: buffer, loc };

    // Load mask texture
    const tex = gl.createTexture();
    texReference.current = tex;
    const img = new Image();
    img.crossOrigin = "anonymous";
    img.addEventListener("load", () => {
      texSizeReference.current = { w: img.width, h: img.height };
      gl.bindTexture(gl.TEXTURE_2D, tex);
      gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL, true);
      gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
      gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
      gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
      gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
      gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
      startTimeReference.current = performance.now();
      draw();
    });
    img.src = maskUrl;

    const draw = () => {
      if (
        !glReference.current ||
        !progReference.current ||
        !vaoReference.current ||
        !texReference.current
      )
        return;
      const glc = glReference.current;
      const progc = progReference.current;
      const vao = vaoReference.current;

      glc.viewport(0, 0, canvas.width, canvas.height);
      glc.clearColor(0, 0, 0, 0);
      glc.clear(glc.COLOR_BUFFER_BIT);

      glc.useProgram(progc);

      glc.bindBuffer(glc.ARRAY_BUFFER, vao.buf);
      glc.enableVertexAttribArray(vao.loc);
      glc.vertexAttribPointer(vao.loc, 2, glc.FLOAT, false, 0, 0);

      const t =
        (performance.now() - startTimeReference.current) * 0.001 * speed;

      const uMask = glc.getUniformLocation(progc, "u_mask");
      const uTexel = glc.getUniformLocation(progc, "u_texel");
      const uCanvasSize = glc.getUniformLocation(progc, "u_canvasSize");
      const uTexSize = glc.getUniformLocation(progc, "u_texSize");
      const uTime = glc.getUniformLocation(progc, "u_time");
      const uRadius = glc.getUniformLocation(progc, "u_radiusPx");
      const uIntensity = glc.getUniformLocation(progc, "u_intensity");

      glc.activeTexture(glc.TEXTURE0);
      glc.bindTexture(glc.TEXTURE_2D, texReference.current);
      glc.uniform1i(uMask, 0);

      const texW = Math.max(1, texSizeReference.current.w);
      const texH = Math.max(1, texSizeReference.current.h);
      glc.uniform2f(uTexel, 1 / texW, 1 / texH);
      glc.uniform2f(uCanvasSize, canvas.width, canvas.height);
      glc.uniform2f(uTexSize, texW, texH);
      glc.uniform1f(uTime, t);
      glc.uniform1f(uRadius, radiusPx);
      glc.uniform1f(uIntensity, intensity);

      glc.drawArrays(glc.TRIANGLES, 0, 6);

      rafReference.current = requestAnimationFrame(draw);
    };

    return () => {
      if (rafReference.current) cancelAnimationFrame(rafReference.current);
      if (glReference.current) {
        // Cleanup GL resources
        if (progReference.current)
          glReference.current.deleteProgram(progReference.current);
        if (vaoReference.current?.buf)
          glReference.current.deleteBuffer(vaoReference.current.buf);
        if (texReference.current)
          glReference.current.deleteTexture(texReference.current);
      }
      glReference.current = null;
      progReference.current = null;
      vaoReference.current = null;
      texReference.current = null;
    };
  }, [width, height, maskUrl, intensity, radiusPx, speed]);

  return (
    <canvas
      ref={canvasReference}
      className={className}
      style={{
        width,
        height,
        display: "block",
        position: "absolute",
        left: 0,
        top: 0,
        pointerEvents: "none",
        zIndex: 1,
        ...style,
      }}
    />
  );
};

export default FireOutlineCanvas;
